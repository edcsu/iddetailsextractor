"""Thin wrapper around PaddleOCR.

main.py never touches the PaddleOCR API directly — everything OCR-engine-specific
lives here, so swapping engines later only touches this file. PaddleOCR itself is
imported lazily (inside ``_ensure_loaded``) so the module can be imported, and the
FastAPI app exercised in tests, without the (large) paddleocr/paddlepaddle packages
installed.
"""

from __future__ import annotations

import os
import threading
from functools import lru_cache
from typing import Any

import numpy as np
from PIL import Image


class OcrEngine:
    def __init__(self, lang: str = "en") -> None:
        self._lang = lang
        self._lock = threading.Lock()
        self._paddle_ocr: Any = None

    def _ensure_loaded(self) -> Any:
        if self._paddle_ocr is None:
            with self._lock:
                if self._paddle_ocr is None:
                    from paddleocr import PaddleOCR

                    self._paddle_ocr = PaddleOCR(lang=self._lang, use_angle_cls=True, show_log=False)
        return self._paddle_ocr

    def recognize(self, image: Image.Image) -> list[dict]:
        ocr = self._ensure_loaded()
        array = np.array(image.convert("RGB"))
        result = ocr.ocr(array, cls=True)

        lines: list[dict] = []
        for page in result or []:
            for box, (text, confidence) in page or []:
                lines.append(
                    {
                        "text": text,
                        "confidence": float(confidence),
                        "bounding_box": [[float(x), float(y)] for x, y in box],
                    }
                )
        return lines


@lru_cache(maxsize=1)
def get_ocr_engine() -> OcrEngine:
    return OcrEngine(lang=os.environ.get("OCR_LANG", "en"))

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

                    # PaddleOCR 3.x rewrote its constructor around PaddleX pipelines:
                    # `use_angle_cls` became `use_textline_orientation`, and `show_log`
                    # was removed entirely (it now uses standard Python logging).
                    self._paddle_ocr = PaddleOCR(lang=self._lang, use_textline_orientation=True)
        return self._paddle_ocr

    def recognize(self, image: Image.Image) -> list[dict]:
        ocr = self._ensure_loaded()
        array = np.array(image.convert("RGB"))
        results = ocr.predict(array)

        lines: list[dict] = []
        for page in results:
            texts = page["rec_texts"]
            scores = page["rec_scores"]
            polys = page["rec_polys"]
            for text, score, poly in zip(texts, scores, polys):
                lines.append(
                    {
                        "text": text,
                        "confidence": float(score),
                        "bounding_box": [[float(x), float(y)] for x, y in poly],
                    }
                )
        return lines


@lru_cache(maxsize=1)
def get_ocr_engine() -> OcrEngine:
    return OcrEngine(lang=os.environ.get("OCR_LANG", "en"))

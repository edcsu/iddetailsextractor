"""OCR engine backends.

main.py never touches an OCR library directly — everything engine-specific lives here,
so swapping engines only touches this file. Both backends run the same PP-OCR models
and return the same shape; they differ only in the inference runtime:

* ``rapidocr`` (default) — onnxruntime. Has working native ARM64 wheels.
* ``paddle``             — paddlepaddle/paddleocr. Requires requirements-paddle.txt,
  and segfaults during model construction on ARM64 hosts (upstream bug, see README).

Pick one with the ``OCR_ENGINE`` environment variable. Each backend imports its library
lazily (inside ``_ensure_loaded``) so this module can be imported — and the FastAPI app
exercised in tests — with neither of the (large) OCR packages installed.
"""

from __future__ import annotations

import os
import threading
from functools import lru_cache
from typing import Any

import numpy as np
from PIL import Image


class PaddleOcrEngine:
    """PP-OCR under paddlepaddle. Opt-in — see the module docstring."""

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


class RapidOcrEngine:
    """PP-OCR under onnxruntime, via RapidOCR. The default engine."""

    def __init__(self, lang: str = "en") -> None:
        self._lang = lang
        self._lock = threading.Lock()
        self._rapid_ocr: Any = None

    def _ensure_loaded(self) -> Any:
        if self._rapid_ocr is None:
            with self._lock:
                if self._rapid_ocr is None:
                    from rapidocr import LangDet, LangRec, RapidOCR

                    # Overrides use "<Section>.<key>" keys and must be the library's own
                    # enums, not strings. Only detection and recognition are language-
                    # specific; the orientation classifier ships a single shared model.
                    self._rapid_ocr = RapidOCR(
                        params={
                            "Det.lang_type": LangDet(self._lang),
                            "Rec.lang_type": LangRec(self._lang),
                        }
                    )
        return self._rapid_ocr

    def recognize(self, image: Image.Image) -> list[dict]:
        ocr = self._ensure_loaded()
        array = np.array(image.convert("RGB"))
        result = ocr(array)

        # RapidOCROutput leaves txts/scores/boxes as None when nothing was recognised.
        if result.txts is None or result.scores is None:
            return []

        boxes = result.boxes if result.boxes is not None else [None] * len(result.txts)
        return [
            {
                "text": text,
                "confidence": float(score),
                "bounding_box": [] if box is None else [[float(x), float(y)] for x, y in box],
            }
            for text, score, box in zip(result.txts, result.scores, boxes)
        ]


ENGINES = {"rapidocr": RapidOcrEngine, "paddle": PaddleOcrEngine}


@lru_cache(maxsize=1)
def get_ocr_engine():
    name = os.environ.get("OCR_ENGINE", "rapidocr").lower()
    try:
        engine_cls = ENGINES[name]
    except KeyError:
        raise ValueError(
            f"Unknown OCR_ENGINE '{name}'. Expected one of: {', '.join(sorted(ENGINES))}."
        ) from None
    return engine_cls(lang=os.environ.get("OCR_LANG", "en"))

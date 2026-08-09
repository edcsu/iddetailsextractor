"""Internal-only OCR sidecar.

Wraps PaddleOCR behind a small HTTP contract so the .NET Extraction module never
needs to know how OCR is actually performed. Not exposed outside the docker-compose
network — the API host is the only intended caller.
"""

from __future__ import annotations

import io

from fastapi import FastAPI, File, HTTPException, UploadFile
from PIL import Image, UnidentifiedImageError
from pydantic import BaseModel

from ocr_engine import get_ocr_engine

MAX_UPLOAD_BYTES = 10 * 1024 * 1024
ALLOWED_CONTENT_TYPES = {"image/jpeg", "image/png", "image/webp"}

app = FastAPI(title="IdExtractor OCR Sidecar")


class OcrLine(BaseModel):
    text: str
    confidence: float
    bounding_box: list[list[float]]


class OcrResponse(BaseModel):
    lines: list[OcrLine]


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/ocr", response_model=OcrResponse)
async def run_ocr(file: UploadFile = File(...)) -> OcrResponse:
    if file.content_type not in ALLOWED_CONTENT_TYPES:
        raise HTTPException(status_code=415, detail=f"Unsupported content type '{file.content_type}'.")

    raw = await file.read()
    if not raw:
        raise HTTPException(status_code=400, detail="Empty upload.")
    if len(raw) > MAX_UPLOAD_BYTES:
        raise HTTPException(status_code=413, detail="Image exceeds the 10 MB limit.")

    try:
        image = Image.open(io.BytesIO(raw))
        image.load()
    except UnidentifiedImageError as exc:
        raise HTTPException(status_code=400, detail="Could not decode image.") from exc

    lines = get_ocr_engine().recognize(image)
    return OcrResponse(lines=[OcrLine(**line) for line in lines])

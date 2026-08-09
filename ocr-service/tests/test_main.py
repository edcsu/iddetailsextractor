import io
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from fastapi.testclient import TestClient
from PIL import Image

import main


class StubEngine:
    def recognize(self, image):
        return [
            {"text": "PASSPORT", "confidence": 0.99, "bounding_box": [[0, 0], [10, 0], [10, 5], [0, 5]]},
        ]


def _make_png_bytes() -> bytes:
    buffer = io.BytesIO()
    Image.new("RGB", (20, 20), color="white").save(buffer, format="PNG")
    return buffer.getvalue()


def test_health_check():
    client = TestClient(main.app)

    response = client.get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_ocr_returns_recognized_lines(monkeypatch):
    monkeypatch.setattr(main, "get_ocr_engine", lambda: StubEngine())
    client = TestClient(main.app)

    response = client.post("/ocr", files={"file": ("id.png", _make_png_bytes(), "image/png")})

    assert response.status_code == 200
    body = response.json()
    assert body["lines"][0]["text"] == "PASSPORT"
    assert body["lines"][0]["confidence"] == 0.99
    assert body["lines"][0]["bounding_box"] == [[0, 0], [10, 0], [10, 5], [0, 5]]


def test_ocr_rejects_unsupported_content_type():
    client = TestClient(main.app)

    response = client.post("/ocr", files={"file": ("id.txt", b"not an image", "text/plain")})

    assert response.status_code == 415


def test_ocr_rejects_empty_upload():
    client = TestClient(main.app)

    response = client.post("/ocr", files={"file": ("id.png", b"", "image/png")})

    assert response.status_code == 400


def test_ocr_rejects_oversized_upload(monkeypatch):
    monkeypatch.setattr(main, "MAX_UPLOAD_BYTES", 10)
    client = TestClient(main.app)

    response = client.post("/ocr", files={"file": ("id.png", _make_png_bytes(), "image/png")})

    assert response.status_code == 413


def test_ocr_rejects_corrupt_image():
    client = TestClient(main.app)

    response = client.post("/ocr", files={"file": ("id.png", b"\x89PNGnotreallyapng", "image/png")})

    assert response.status_code == 400

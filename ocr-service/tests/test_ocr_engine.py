import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import numpy as np
import pytest
from PIL import Image

import ocr_engine
from ocr_engine import PaddleOcrEngine, RapidOcrEngine, get_ocr_engine


@pytest.fixture(autouse=True)
def _clear_engine_cache():
    get_ocr_engine.cache_clear()
    yield
    get_ocr_engine.cache_clear()


def test_defaults_to_rapidocr(monkeypatch):
    monkeypatch.delenv("OCR_ENGINE", raising=False)

    assert isinstance(get_ocr_engine(), RapidOcrEngine)


@pytest.mark.parametrize(
    ("value", "expected"),
    [("rapidocr", RapidOcrEngine), ("paddle", PaddleOcrEngine), ("PADDLE", PaddleOcrEngine)],
)
def test_selects_engine_from_env(monkeypatch, value, expected):
    monkeypatch.setenv("OCR_ENGINE", value)

    assert isinstance(get_ocr_engine(), expected)


def test_unknown_engine_raises(monkeypatch):
    monkeypatch.setenv("OCR_ENGINE", "tesseract")

    with pytest.raises(ValueError, match="Unknown OCR_ENGINE 'tesseract'"):
        get_ocr_engine()


def test_engine_uses_ocr_lang(monkeypatch):
    monkeypatch.setenv("OCR_ENGINE", "rapidocr")
    monkeypatch.setenv("OCR_LANG", "japan")

    assert get_ocr_engine()._lang == "japan"


class FakeRapidOcrOutput:
    """Stands in for rapidocr's RapidOCROutput dataclass."""

    def __init__(self, txts, scores, boxes):
        self.txts = txts
        self.scores = scores
        self.boxes = boxes


def _stub_rapid_engine(engine, output):
    engine._rapid_ocr = lambda array: output


def test_rapidocr_maps_results_to_line_dicts():
    engine = RapidOcrEngine()
    _stub_rapid_engine(
        engine,
        FakeRapidOcrOutput(
            txts=("PASSPORT", "P<GBRSMITH<<JOHN"),
            scores=(np.float32(0.98), np.float32(0.91)),
            boxes=np.array(
                [
                    [[0, 0], [10, 0], [10, 5], [0, 5]],
                    [[0, 6], [40, 6], [40, 11], [0, 11]],
                ],
                dtype=np.float32,
            ),
        ),
    )

    lines = engine.recognize(Image.new("RGB", (50, 20), color="white"))

    assert lines == [
        {
            "text": "PASSPORT",
            "confidence": pytest.approx(0.98),
            "bounding_box": [[0.0, 0.0], [10.0, 0.0], [10.0, 5.0], [0.0, 5.0]],
        },
        {
            "text": "P<GBRSMITH<<JOHN",
            "confidence": pytest.approx(0.91),
            "bounding_box": [[0.0, 6.0], [40.0, 6.0], [40.0, 11.0], [0.0, 11.0]],
        },
    ]
    # Confidences must be plain floats, not numpy scalars, so FastAPI can serialize them.
    assert all(type(line["confidence"]) is float for line in lines)


def test_rapidocr_returns_empty_list_when_nothing_recognized():
    engine = RapidOcrEngine()
    _stub_rapid_engine(engine, FakeRapidOcrOutput(txts=None, scores=None, boxes=None))

    assert engine.recognize(Image.new("RGB", (10, 10), color="white")) == []


def test_rapidocr_tolerates_missing_boxes():
    engine = RapidOcrEngine()
    _stub_rapid_engine(engine, FakeRapidOcrOutput(txts=("ID",), scores=(0.5,), boxes=None))

    assert engine.recognize(Image.new("RGB", (10, 10), color="white")) == [
        {"text": "ID", "confidence": 0.5, "bounding_box": []}
    ]


def test_paddle_maps_results_to_line_dicts():
    engine = PaddleOcrEngine()
    engine._paddle_ocr = type(
        "FakePaddle",
        (),
        {
            "predict": staticmethod(
                lambda array: [
                    {
                        "rec_texts": ["PASSPORT"],
                        "rec_scores": [np.float32(0.97)],
                        "rec_polys": [np.array([[0, 0], [10, 0], [10, 5], [0, 5]])],
                    }
                ]
            )
        },
    )()

    lines = engine.recognize(Image.new("RGB", (20, 20), color="white"))

    assert lines == [
        {
            "text": "PASSPORT",
            "confidence": pytest.approx(0.97),
            "bounding_box": [[0.0, 0.0], [10.0, 0.0], [10.0, 5.0], [0.0, 5.0]],
        }
    ]


def test_engine_registry_covers_documented_values():
    assert sorted(ocr_engine.ENGINES) == ["paddle", "rapidocr"]

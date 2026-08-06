# Music Remixer

A web app that splits a song into four stems (vocals, drums, bass, other) using the Demucs model, lets you adjust each stem's volume, and exports a remixed WAV.

## How it works

Upload an audio file → Demucs separates it into four tracks → adjust the volume of each track → download the mixed result.

Built with FastAPI, Vue 3, and Meta's HTDemucs model.

## Requirements

- Python 3.10+
- Node.js 18+
- About 2 GB of free disk space (model weights + temp audio)
- 4 GB+ RAM recommended

## Setup

### Backend

```bash
cd ai-service

python -m venv venv
source venv/Scripts/activate    # Windows (Git Bash)
# venv\Scripts\activate         # Windows (CMD / PowerShell)
# source venv/bin/activate      # macOS / Linux

pip install -r requirements.txt
uvicorn main:app --host 127.0.0.1 --port 8000
```

The first run downloads the HTDemucs model weights (~80 MB) from HuggingFace. Set the `HF_ENDPOINT` environment variable to a mirror if you are behind a firewall or in China.

### Frontend

```bash
cd frontend

npm install
npm run dev
```

Open http://localhost:5173 and upload a file.

## Formats

Input: WAV, MP3, FLAC, OGG. Output is always 16-bit 44.1kHz stereo WAV.

## API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/separate` | POST | Upload a file. Returns `job_id`. |
| `/progress/{job_id}` | GET | SSE stream with separation progress (0–100%). |
| `/mix` | POST | Mix stems with per-track volume (0.0–1.0). Returns WAV. |

See `ai-service/main.py` for details.

## Notes

- Demucs runs on GPU if CUDA is available. It falls back to CPU otherwise, which is slower.
- Separation speed depends on audio length and hardware. A 4-minute track takes about 1–2 minutes on a modern GPU.
- The mixer uses only numpy and the Python `wave` module — no ffmpeg required.
- CORS is configured for localhost:5173 and 5174 by default.

## License

MIT

---

[AsianCopper](https://github.com/AsianCopper)

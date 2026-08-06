from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import FileResponse, StreamingResponse
from pydantic import BaseModel
from mixer import mix_stems
import shutil
import subprocess
import os
import uuid
import json
import asyncio
import threading
import time
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5173",
        "http://127.0.0.1:5174",
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ---- job state ----
jobs = {}  # job_id → {progress, status}


class MixRequest(BaseModel):
    job_id: str
    vocals: float
    drums: float
    bass: float
    other: float


@app.get("/")
def home():
    return {"message": "Music Remixer is running..."}


# ---- 第一步：上传 → 后台分离 ----
@app.post("/separate")
async def separate(file: UploadFile = File(...)):
    job_id = uuid.uuid4().hex[:10]
    input_dir = "uploads"
    os.makedirs(input_dir, exist_ok=True)
    # 保留原始文件扩展名
    ext = os.path.splitext(file.filename)[1] or ".mp3"
    input_path = f"{input_dir}/{job_id}{ext}"

    with open(input_path, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    jobs[job_id] = {"progress": 0, "status": "uploaded"}

    def run_demucs():
        import re
        jobs[job_id]["status"] = "正在分离…"

        proc = subprocess.Popen(
            ["demucs", "-o", "separated", input_path],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.PIPE,
            text=True,
            bufsize=1,
        )

        # 逐行解析 tqdm 进度条: "8%|████...| 23.4/298.35 [00:07<01:28, 3.11s/s]"
        pct_re = re.compile(r"^\s*(\d+)%")
        for line in proc.stderr:
            m = pct_re.match(line)
            if m:
                jobs[job_id]["progress"] = int(m.group(1))

        proc.wait()
        jobs[job_id]["progress"] = 100
        jobs[job_id]["status"] = "done"

    threading.Thread(target=run_demucs, daemon=True).start()
    return {"job_id": job_id}


# ---- SSE 进度推送 ----
@app.get("/progress/{job_id}")
async def progress(job_id: str):
    async def event_stream():
        while True:
            job = jobs.get(job_id)
            if not job:
                yield f"data: {json.dumps({'error': 'job not found'})}\n\n"
                return

            yield f"data: {json.dumps({'progress': job['progress'], 'status': job['status']})}\n\n"

            if job["status"] == "done":
                return

            await asyncio.sleep(0.3)

    return StreamingResponse(
        event_stream(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "Connection": "keep-alive",
        },
    )


# ---- 第二步：混音 ----
@app.post("/mix")
def mix(request: MixRequest):
    stem_dir = f"separated/htdemucs/{request.job_id}"

    if not os.path.isdir(stem_dir):
        raise HTTPException(status_code=404, detail="分离结果不存在，请重新上传")

    try:
        output_path = mix_stems(
            stem_dir=stem_dir,
            vocals_volume=request.vocals,
            drums_volume=request.drums,
            bass_volume=request.bass,
            other_volume=request.other,
        )

        return FileResponse(
            path=output_path,
            media_type="audio/wav",
            filename="remixed.wav",
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"混音失败: {str(e)}")

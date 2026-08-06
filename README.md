# AI Music Remixer

AI 驱动的音乐人声/伴奏分离与重新混音 Web 应用。

上传一首歌 → AI 模型分离为 4 个独立音轨（人声、鼓、贝斯、其他）→ 分别调节音量 → 导出混音。

## 技术栈

| 层 | 技术 |
|------|------|
| AI 分离 | [Demucs](https://github.com/facebookresearch/demucs) (htdemucs) + PyTorch |
| 后端 | FastAPI + Python |
| 前端 | Vue 3 + Vite |
| 音频处理 | numpy + wave |

## 快速开始

### 1. 启动后端

```bash
cd ai-service
python -m venv venv
source venv/Scripts/activate   # Windows
# source venv/bin/activate     # macOS/Linux
pip install demucs fastapi uvicorn numpy python-multipart
uvicorn main:app --host 127.0.0.1 --port 8000
```

首次运行时会自动从 HuggingFace 下载 HTDemucs 模型权重（约 80MB）。

### 2. 启动前端

```bash
cd frontend
npm install
npm run dev
```

### 3. 打开浏览器

访问 `http://localhost:5173`，上传音频文件即可使用。

## 工作流程

1. **上传音频** → 前端发送文件到后端 `/separate`
2. **AI 分离** → 后端调用 Demucs，SSE 实时推送进度
3. **调节音轨** → 四个独立滑块控制人声/鼓/贝斯/其他的音量
4. **导出混音** → 后端 `/mix` 混音，浏览器自动下载 wav

## 项目结构

```
├── ai-service/
│   ├── main.py          # FastAPI 后端
│   ├── mixer.py         # 音轨混音逻辑
│   └── requirements.txt
├── frontend/
│   └── src/
│       └── App.vue      # Vue 单文件组件
└── README.md
```

## License

MIT

---

by [亚洲铜](https://github.com/AsianCopper)

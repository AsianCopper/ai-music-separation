# 🎵 AI Music Remixer

AI 驱动的音乐人声/伴奏分离与重新混音 Web 应用。

上传一首歌 → AI 模型分离为 4 个独立音轨（人声、鼓、贝斯、其他）→ 分别调节音量 → 导出混音成品。

---

## ✨ 功能

- **AI 音源分离** — 基于 Meta Demucs (htdemucs) 模型，将任意歌曲拆分为人声、鼓、贝斯、其他四个独立音轨
- **实时进度** — SSE 推送分离进度，tqdm 级别的实时百分比反馈
- **独立混音** — 每个音轨 0-100% 无级调节，所见即所得
- **一键导出** — 调节完成后点击导出，浏览器自动下载混音后的 WAV 文件

## 🖼️ 界面

三步式操作流程：

```
① 上传页面          ② 分离进度            ③ 混音调节
┌────────────┐    ┌────────────┐    ┌────────────┐
│  拖拽或点击  │ → │ ██████ 45% │ → │ 🎤 人声 ──●│
│  上传音频    │    │ AI 分离中…  │    │ 🥁 鼓   ───●│
│  MP3/WAV    │    │            │    │ 🎸 贝斯 ───●│
│             │    │            │    │ 🎼 其他 ───●│
│             │    │            │    │ [▶ 导出音频] │
└────────────┘    └────────────┘    └────────────┘
```

---

## 📋 环境要求

| 依赖 | 最低版本 | 说明 |
|------|---------|------|
| Python | 3.10+ | 推荐 3.11 |
| Node.js | 18+ | 推荐 20 LTS |
| pip | 23.0+ | Python 包管理 |
| 磁盘空间 | ~2 GB | 模型权重 ~80MB + 临时音频文件 |
| 内存 | 4 GB+ | Demucs 模型推理需要 |

> ⚠️ **Windows 用户注意**：建议在 Git Bash 或 PowerShell 中运行以下命令，CMD 可能出现编码问题。

---

## 🚀 安装与运行

### 1. 克隆仓库

```bash
git clone https://github.com/AsianCopper/ai-music-separation.git
cd ai-music-separation
```

### 2. 配置后端 (Python)

```bash
cd ai-service

# 创建虚拟环境
python -m venv venv

# 激活虚拟环境
source venv/Scripts/activate   # Windows (Git Bash)
# 或
venv\Scripts\activate          # Windows (CMD / PowerShell)
# 或
source venv/bin/activate       # macOS / Linux

# 安装依赖
pip install -r requirements.txt
```

> 📦 `requirements.txt` 包含：`demucs`, `fastapi`, `uvicorn`, `numpy`, `python-multipart`

**首次运行**：Demucs 会自动从 HuggingFace 下载 HTDemucs 模型权重（约 80MB），缓存到 `~/.cache/huggingface/`。请确保网络通畅。如果下载较慢，可设置 HuggingFace 镜像：

```bash
export HF_ENDPOINT=https://hf-mirror.com   # Linux/macOS
set HF_ENDPOINT=https://hf-mirror.com      # Windows CMD
$env:HF_ENDPOINT="https://hf-mirror.com"   # Windows PowerShell
```

**启动后端**：

```bash
uvicorn main:app --host 127.0.0.1 --port 8000
```

启动成功后会显示：

```
INFO:     Uvicorn running on http://127.0.0.1:8000
```

### 3. 配置前端 (Vue 3)

打开**新的终端窗口**：

```bash
cd frontend

# 安装依赖
npm install

# 启动开发服务器
npm run dev
```

启动成功后会显示：

```
VITE v8.x.x  ready in xxx ms
➜  Local:   http://localhost:5173/
```

### 4. 开始使用

1. 浏览器打开 `http://localhost:5173`
2. 点击上传区域或拖拽音频文件
3. 等待 AI 分离完成（进度条实时显示）
4. 调节四个音轨的音量滑块
5. 点击「导出音频」下载混音成品

---

## 🏗️ 项目结构

```
ai-music-separation/
├── ai-service/                # Python 后端
│   ├── main.py                # FastAPI 应用 (API 端点 + SSE)
│   ├── mixer.py               # 音轨混音引擎 (numpy + wave)
│   └── requirements.txt       # Python 依赖
├── frontend/                  # Vue 3 前端
│   ├── src/
│   │   ├── App.vue            # 主组件 (三步骤流程 UI)
│   │   └── main.js            # Vue 入口
│   ├── public/
│   │   └── bg.png             # 背景图
│   ├── index.html
│   ├── package.json
│   └── vite.config.js
├── .gitignore
└── README.md
```

## 🔧 API 文档

### `POST /separate`
上传音频文件，启动 AI 分离。

- **请求**：`multipart/form-data`，字段 `file`（音频文件）
- **响应**：`{ "job_id": "abc123..." }`

### `GET /progress/{job_id}`
SSE 端点，实时推送分离进度。

- **事件格式**：`data: {"progress": 45, "status": "正在分离…"}`
- **完成标记**：`status` 为 `"done"` 且 `progress` 为 100

### `POST /mix`
对已分离的音轨进行混音。

- **请求**：`application/json`
```json
{
  "job_id": "abc123...",
  "vocals": 0.5,
  "drums": 1.0,
  "bass": 1.0,
  "other": 1.0
}
```
- **响应**：`audio/wav` 文件下载

---

## 🛠️ 技术栈

| 层 | 技术 | 说明 |
|------|------|------|
| AI 模型 | [Demucs](https://github.com/facebookresearch/demucs) 4.x (htdemucs) | Meta 的音源分离模型，基于 PyTorch |
| Web 框架 | FastAPI | 异步 Python Web 框架 |
| 实时推送 | Server-Sent Events (SSE) | 分离进度实时推送到前端 |
| 音频处理 | numpy + Python wave | 纯 Python 实现，无需 ffmpeg |
| 前端框架 | Vue 3 (Composition API) | 单文件组件，v-model 绑定 |
| 构建工具 | Vite | 极速开发服务器 |
| HTTP 客户端 | Axios | 文件上传 + 进度回调 + Blob 下载 |

---

## ❓ 常见问题

### Q: 分离速度有多快？
取决于你的硬件和音频长度。在 NVIDIA GPU 上通常比实时快 2-5 倍，CPU 上接近实时或稍慢。一首 4 分钟的歌曲在 GPU 上大约需要 1-2 分钟。

### Q: 支持 GPU 加速吗？
支持。Demucs 会自动检测可用的 CUDA GPU。安装带 CUDA 的 PyTorch 即可：

```bash
pip install torch --index-url https://download.pytorch.org/whl/cu121
```

### Q: 支持哪些音频格式？
WAV / MP3 / FLAC / OGG 均可上传，后端会根据原始扩展名保留格式。导出格式固定为 WAV（16-bit, 44.1kHz 立体声）。

### Q: 为什么分离后的文件那么大？
WAV 是无损格式，一首 4 分钟的歌约 40MB。混音输出也是 WAV，如需压缩请用其他工具转码。

### Q: 前端报 CORS 错误？
后端已配置 CORS 允许 `localhost:5173` 和 `localhost:5174`。如果你用其他端口，需在 `ai-service/main.py` 的 `allow_origins` 中添加。

---

## 📄 License

MIT

---

by [亚洲铜](https://github.com/AsianCopper)

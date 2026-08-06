<template>
  <div class="app">
    <div class="card">
      <!-- ====== header ====== -->
      <div class="hero">
        <h1>AI Music Remixer</h1>
        <p class="author">by 亚洲铜</p>
      </div>

      <!-- ====== step 1: upload ====== -->
      <template v-if="step === 'upload'">
        <label
          class="upload-zone"
          :class="{ dragover: isDragover }"
          for="file-input"
          @dragover.prevent="isDragover = true"
          @dragleave.prevent="isDragover = false"
          @drop.prevent="onDrop"
        >
          <svg class="upload-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
            <polyline points="17 8 12 3 7 8" />
            <line x1="12" y1="3" x2="12" y2="15" />
          </svg>
          <span>点击或拖拽上传音频文件</span>
          <span class="hint">支持 MP3 / WAV / FLAC</span>
        </label>
        <input id="file-input" type="file" accept="audio/*" hidden @change="onFilePicked" ref="fileInput" />
      </template>

      <!-- ====== step 2: processing ====== -->
      <template v-if="step === 'processing'">
        <div class="processing">
          <div class="file-badge">
            <svg class="file-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 19V6h6l4 4v9a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h7l6 6v12" />
            </svg>
            <span class="file-name">{{ fileName }}</span>
          </div>

          <div class="progress-section">
            <div class="progress-bar">
              <div class="progress-fill" :style="{ width: progress + '%' }" />
            </div>
            <p class="progress-text">{{ progressText }}</p>
          </div>

          <div class="spinner-row">
            <span class="spinner" />
            <span class="status-label">{{ statusLabel }}</span>
          </div>
        </div>
      </template>

      <!-- ====== step 3: ready ====== -->
      <template v-if="step === 'ready'">
        <div class="file-badge done">
          <svg class="file-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M9 19V6h6l4 4v9a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h7l6 6v12" />
          </svg>
          <span class="file-name">{{ fileName }}</span>
          <svg class="check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <polyline points="20 6 9 17 4 12" />
          </svg>
        </div>

        <div class="stems">
          <div class="stem">
            <div class="stem-header">
              <span class="stem-icon">🎤</span>
              <span class="stem-label">人声</span>
              <span class="stem-val">{{ Math.round(vocals * 100) }}%</span>
            </div>
            <div class="track-wrap">
              <div class="track-fill" :style="{ width: vocals * 100 + '%' }" />
              <input type="range" min="0" max="1" step="0.01" v-model="vocals" />
            </div>
          </div>

          <div class="stem">
            <div class="stem-header">
              <span class="stem-icon">🥁</span>
              <span class="stem-label">鼓</span>
              <span class="stem-val">{{ Math.round(drums * 100) }}%</span>
            </div>
            <div class="track-wrap">
              <div class="track-fill" :style="{ width: drums * 100 + '%' }" />
              <input type="range" min="0" max="1" step="0.01" v-model="drums" />
            </div>
          </div>

          <div class="stem">
            <div class="stem-header">
              <span class="stem-icon">🎸</span>
              <span class="stem-label">贝斯</span>
              <span class="stem-val">{{ Math.round(bass * 100) }}%</span>
            </div>
            <div class="track-wrap">
              <div class="track-fill" :style="{ width: bass * 100 + '%' }" />
              <input type="range" min="0" max="1" step="0.01" v-model="bass" />
            </div>
          </div>

          <div class="stem">
            <div class="stem-header">
              <span class="stem-icon">🎼</span>
              <span class="stem-label">其他</span>
              <span class="stem-val">{{ Math.round(other * 100) }}%</span>
            </div>
            <div class="track-wrap">
              <div class="track-fill" :style="{ width: other * 100 + '%' }" />
              <input type="range" min="0" max="1" step="0.01" v-model="other" />
            </div>
          </div>
        </div>

        <button class="generate-btn" @click="downloadRemix">
          <svg class="btn-icon" viewBox="0 0 24 24" fill="currentColor">
            <polygon points="5 3 19 12 5 21 5 3" />
          </svg>
          导出音频
        </button>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue"
import axios from "axios"

const API = "http://127.0.0.1:8000"

const step = ref("upload")
const jobId = ref("")
const fileName = ref("")
const fileInput = ref(null)
const isDragover = ref(false)

const progress = ref(0)
const progressText = ref("")
const statusLabel = ref("")

const vocals = ref(0.5)
const drums = ref(1)
const bass = ref(1)
const other = ref(1)

// ====== 拖拽上传 ======
function onDrop(e) {
  isDragover.value = false
  const file = e.dataTransfer?.files?.[0]
  if (!file) return
  processFile(file)
}

// ====== 1. 选文件 → 上传 → SSE 监听分离进度 ======
function onFilePicked(e) {
  const file = e.target.files?.[0]
  if (!file) return
  processFile(file)
}

async function processFile(file) {
  fileName.value = file.name
  step.value = "processing"

  progress.value = 0
  progressText.value = "0%"
  statusLabel.value = "正在上传…"

  const formData = new FormData()
  formData.append("file", file)

  try {
    // 上传文件（后端只存文件，立即返回）
    const res = await axios.post(`${API}/separate`, formData, {
      onUploadProgress(e) {
        if (e.total) {
          const pct = Math.round((e.progress ?? 0) * 100)
          progress.value = pct
          progressText.value = pct + "%"
        }
      },
    })

    jobId.value = res.data.job_id
    progress.value = 0
    progressText.value = "0%"
    statusLabel.value = "AI 分离中…"

    // SSE 监听真实分离进度
    const es = new EventSource(`${API}/progress/${jobId.value}`)

    es.onmessage = (event) => {
      const data = JSON.parse(event.data)
      if (data.error) {
        es.close()
        statusLabel.value = "出错了，请重试"
        return
      }

      progress.value = data.progress
      progressText.value = Math.round(data.progress) + "%"
      statusLabel.value = data.status

      if (data.status === "done") {
        es.close()
        step.value = "ready"
      }
    }

    es.onerror = () => {
      es.close()
      statusLabel.value = "连接中断，请重试"
    }

  } catch (err) {
    console.error("分离失败:", err)
    statusLabel.value = "分离失败，请重试"
  }
}

// ====== 2. 导出混音 ======
async function downloadRemix() {
  try {
    const res = await axios.post(
      `${API}/mix`,
      {
        job_id: jobId.value,
        vocals: vocals.value,
        drums: drums.value,
        bass: bass.value,
        other: other.value,
      },
      { responseType: "blob" }
    )

    const url = window.URL.createObjectURL(res.data)
    const a = document.createElement("a")
    a.href = url
    a.download = fileName.value.replace(/\.[^/.]+$/, "") + "_remixed.wav"
    a.click()
    window.URL.revokeObjectURL(url)

  } catch (err) {
    console.error("导出失败:", err)
    alert("生成失败，请检查后台服务是否开启。")
  }
}

function sleep(ms) {
  return new Promise(
    r => setTimeout(r, ms)
)
}
</script>

<style>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  background: #f3f0ec;
  color: #3c3c43;
  min-height: 100vh;
}

.app {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background:
    linear-gradient(rgba(255,255,255,0.55), rgba(255,255,255,0.55)),
    url('/bg.png') center/cover no-repeat fixed;
}

.card {
  width: 100%;
  max-width: 520px;
  background: rgba(255, 255, 255, 0.85);
  border: 1px solid rgba(0, 0, 0, 0.06);
  border-radius: 24px;
  padding: 40px 36px 36px;
  backdrop-filter: blur(16px);
  box-shadow: 0 4px 40px rgba(0, 0, 0, 0.06);
}

.hero {
  text-align: center;
  margin-bottom: 32px;
}

h1 {
  font-size: 26px;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: #1a1a1a;
}

.author {
  margin-top: 6px;
  font-size: 14px;
  color: #9ca3af;
}

/* ---- upload ---- */

.upload-zone {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 28px 16px;
  border: 2px dashed #e5e5e5;
  border-radius: 16px;
  cursor: pointer;
  transition: border-color .2s, background .2s;
  font-size: 14px;
  color: #9ca3af;
}

.upload-zone:hover,
.upload-zone.dragover {
  border-color: rgba(244, 114, 182, 0.5);
  background: rgba(244, 114, 182, 0.06);
}

.upload-zone.dragover {
  border-color: #f472b6;
  background: rgba(244, 114, 182, 0.12);
}

.upload-icon {
  width: 28px;
  height: 28px;
  color: #9ca3af;
}

.hint {
  font-size: 12px;
  color: #c4c4c4;
}

/* ---- file badge ---- */

.file-badge {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  background: #f8f8f8;
  border-radius: 12px;
  margin-bottom: 24px;
}

.file-badge.done {
  border: 1px solid rgba(244, 114, 182, 0.3);
}

.file-icon {
  width: 20px;
  height: 20px;
  color: #9ca3af;
  flex-shrink: 0;
}

.file-name {
  font-size: 14px;
  color: #3c3c43;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.check-icon {
  width: 20px;
  height: 20px;
  color: #f472b6;
  flex-shrink: 0;
}

/* ---- processing ---- */

.processing {
  display: flex;
  flex-direction: column;
}

.progress-section {
  margin-bottom: 20px;
}

.progress-bar {
  height: 6px;
  background: #eee;
  border-radius: 3px;
  overflow: hidden;
  margin-bottom: 10px;
}

.progress-fill {
  height: 100%;
  border-radius: 3px;
  background: linear-gradient(90deg, #ef4444, #f472b6);
  transition: width .25s ease;
}

.progress-text {
  text-align: right;
  font-size: 14px;
  font-weight: 600;
  color: #f472b6;
  font-variant-numeric: tabular-nums;
}

.spinner-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2px solid #eee;
  border-top-color: #f472b6;
  border-radius: 50%;
  animation: spin .7s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.status-label {
  font-size: 14px;
  color: #9ca3af;
}

/* ---- stems ---- */

.stems {
  display: flex;
  flex-direction: column;
  gap: 18px;
  margin-bottom: 28px;
}

.stem {
  background: #f8f8f8;
  border-radius: 14px;
  padding: 14px 18px;
}

.stem-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
}

.stem-icon {
  font-size: 16px;
}

.stem-label {
  font-size: 14px;
  font-weight: 500;
  color: #3c3c43;
  flex: 1;
}

.stem-val {
  font-size: 13px;
  font-weight: 600;
  color: #f472b6;
  font-variant-numeric: tabular-nums;
  min-width: 36px;
  text-align: right;
}

/* ---- range ---- */

.track-wrap {
  position: relative;
  height: 18px;
  display: flex;
  align-items: center;
}

.track-fill {
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%);
  height: 6px;
  border-radius: 3px;
  background: linear-gradient(90deg, #f472b6, #f9a8d4);
  pointer-events: none;
}

input[type="range"] {
  position: absolute;
  left: 0;
  top: 50%;
  width: 100%;
  height: 18px;
  transform: translateY(-50%);
  -webkit-appearance: none;
  appearance: none;
  background: transparent;
  outline: none;
  margin: 0;
}

/* track background */
input[type="range"]::-webkit-slider-runnable-track {
  height: 6px;
  border-radius: 3px;
  background: #e5e5e5;
}

input[type="range"]::-moz-range-track {
  height: 6px;
  border-radius: 3px;
  background: #e5e5e5;
}

input[type="range"]::-webkit-slider-thumb {
  -webkit-appearance: none;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: #fff;
  border: 2px solid #f472b6;
  cursor: pointer;
  box-shadow: 0 0 10px rgba(244, 114, 182, 0.3);
  transition: box-shadow .15s;
}

input[type="range"]::-webkit-slider-thumb:hover {
  box-shadow: 0 0 18px rgba(244, 114, 182, 0.55);
}

input[type="range"]::-moz-range-thumb {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: #fff;
  border: 2px solid #f472b6;
  cursor: pointer;
  box-shadow: 0 0 10px rgba(244, 114, 182, 0.3);
}

/* ---- button ---- */

.generate-btn {
  width: 100%;
  padding: 14px 0;
  font-size: 16px;
  font-weight: 600;
  font-family: inherit;
  color: #fff;
  background: linear-gradient(135deg, #ef4444, #ec4899);
  border: none;
  border-radius: 14px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  transition: transform .15s, box-shadow .15s, opacity .15s;
}

.generate-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 8px 30px rgba(239, 68, 68, 0.3);
}

.generate-btn:active {
  transform: translateY(0);
}

.btn-icon {
  width: 16px;
  height: 16px;
}
</style>

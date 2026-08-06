import numpy as np
import wave


def mix_stems(
    stem_dir,
    vocals_volume=1.0,
    drums_volume=1.0,
    bass_volume=1.0,
    other_volume=1.0,
):
    def load(path):
        with wave.open(path, "rb") as w:
            params = w.getparams()
            data = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16)
            if params.nchannels == 2:
                data = data.reshape(-1, 2)
            return data, params

    vocals, params = load(f"{stem_dir}/vocals.wav")
    drums, _       = load(f"{stem_dir}/drums.wav")
    bass, _        = load(f"{stem_dir}/bass.wav")
    other, _       = load(f"{stem_dir}/other.wav")

    # 线性增益（数学上等价于 pydub 的 20*log10(v) dB 增益）
    vocals = (vocals * vocals_volume).astype(np.int16)
    drums  = (drums  * drums_volume).astype(np.int16)
    bass   = (bass   * bass_volume).astype(np.int16)
    other  = (other  * other_volume).astype(np.int16)

    # 叠加 + 防止溢出
    result = vocals.astype(np.int32) + drums.astype(np.int32) + bass.astype(np.int32) + other.astype(np.int32)
    result = np.clip(result, -32768, 32767).astype(np.int16)

    output_path = f"{stem_dir}/mixed.wav"
    with wave.open(output_path, "wb") as w:
        w.setparams(params)
        w.writeframes(result.tobytes())

    return output_path


def volume_to_db(volume):
    # 保留兼容，但 mixer 内部已不再使用
    import math
    if volume <= 0:
        return -120
    return 20 * math.log10(volume)


if __name__ == "__main__":
    mix_stems(
        stem_dir="separated/htdemucs/song",
        vocals_volume=0,
        drums_volume=1.0,
        bass_volume=1.0,
        other_volume=1.0,
    )
    print("混音完毕")

#!/usr/bin/env python3
"""音频入库用到的底层函数：解码、BS.1770 响度、真峰值、循环交叉、切段、编码。

只依赖 numpy、scipy 与 ffmpeg（SYS-AUDIO）。全部在开发机上运行，不进游戏。
用法：import audiolib as A；A.write_wav(path, x, bits=16)；A.write_ogg(path, x, quality)。
产出：调用方传入路径的 WAV 或 Ogg 文件（父目录由调用方传入）。
"""
import json, subprocess, shutil, os
import numpy as np
from scipy import signal

SR = 48000
FFMPEG = shutil.which("ffmpeg") or "ffmpeg"
FFPROBE = shutil.which("ffprobe") or "ffprobe"

# BS.1770-4 K 计权（48 kHz）
_K1 = (np.array([1.53512485958697, -2.69169618940638, 1.19839281085285]),
       np.array([1.0, -1.69065929318241, 0.73248077421585]))
_K2 = (np.array([1.0, -2.0, 1.0]), np.array([1.0, -1.99004745483398, 0.99007225036621]))


def probe(path):
    r = subprocess.run([FFPROBE, "-v", "error", "-select_streams", "a:0", "-show_entries",
                        "stream=sample_rate,channels,duration", "-of", "json", path],
                       capture_output=True, text=True)
    s = json.loads(r.stdout)["streams"][0]
    return int(s["sample_rate"]), int(s["channels"]), float(s.get("duration") or 0)


def load(path, mono=True):
    """解码成 48 kHz float32，形状 (N, ch)。下混用声道平均（ffmpeg 默认下混会多 +3 dB）。"""
    _, ch, _ = probe(path)
    r = subprocess.run([FFMPEG, "-v", "error", "-i", path, "-f", "f32le", "-ar", str(SR),
                        "-ac", str(ch), "-"], capture_output=True)
    x = np.frombuffer(r.stdout, dtype=np.float32).reshape(-1, ch).astype(np.float64)
    if mono and ch > 1:
        x = x.mean(axis=1, keepdims=True)
    return x


def kweight(x):
    y = signal.lfilter(*_K1, x, axis=0)
    return signal.lfilter(*_K2, y, axis=0)


def _block_energy(k, win, hop):
    """k: K 计权后 (N, ch)；返回每个窗口的平均能量（各声道求和）。"""
    e = (k ** 2).sum(axis=1)
    c = np.concatenate([[0.0], np.cumsum(e)])
    starts = np.arange(0, max(len(e) - win, 0) + 1, hop)
    return (c[starts + win] - c[starts]) / win


def integrated_lufs(x):
    k = kweight(x)
    win, hop = int(0.4 * SR), int(0.1 * SR)
    if len(k) < win:
        k = np.vstack([k, np.zeros((win - len(k), k.shape[1]))])
    z = _block_energy(k, win, hop)
    l = -0.691 + 10 * np.log10(np.maximum(z, 1e-12))
    z1 = z[l > -70]
    if len(z1) == 0:
        return -70.0
    g = -0.691 + 10 * np.log10(z1.mean()) - 10
    z2 = z[l > g]
    if len(z2) == 0:
        return -70.0
    return -0.691 + 10 * np.log10(z2.mean())


def momentary_max_lufs(x):
    """LUFS-M 最大值（400 ms 窗，前后补零，10 ms 步进）。一次性声用。"""
    pad = int(0.4 * SR)
    k = kweight(np.vstack([np.zeros((pad, x.shape[1])), x, np.zeros((pad, x.shape[1]))]))
    z = _block_energy(k, pad, int(0.01 * SR))
    return -0.691 + 10 * np.log10(max(z.max(), 1e-12))


def true_peak_db(x):
    up = signal.resample_poly(x, 4, 1, axis=0)
    return 20 * np.log10(max(np.abs(up).max(), 1e-9))


def sample_peak_db(x):
    return 20 * np.log10(max(np.abs(x).max(), 1e-9))


def highpass(x, fc, order=4):
    if fc <= 0:
        return x
    sos = signal.butter(order, fc, "highpass", fs=SR, output="sos")
    return signal.sosfilt(sos, x, axis=0)


def lowpass(x, fc, order=4):
    sos = signal.butter(order, fc, "lowpass", fs=SR, output="sos")
    return signal.sosfilt(sos, x, axis=0)


def peaking(x, f0, gain_db, q=0.9):
    """RBJ 峰值滤波；gain_db 为负即切。"""
    a = 10 ** (gain_db / 40)
    w0 = 2 * np.pi * f0 / SR
    al = np.sin(w0) / (2 * q)
    b = [1 + al * a, -2 * np.cos(w0), 1 - al * a]
    aa = [1 + al / a, -2 * np.cos(w0), 1 - al / a]
    return signal.lfilter(np.array(b) / aa[0], np.array(aa) / aa[0], x, axis=0)


def fade(x, fin_ms=0, fout_ms=0):
    y = x.copy()
    n = len(y)
    fi, fo = int(fin_ms * SR / 1000), int(fout_ms * SR / 1000)
    if fi:
        y[:fi] *= np.linspace(0, 1, fi)[:, None]
    if fo:
        y[n - fo:] *= (np.linspace(1, 0, fo) ** 2.0)[:, None]
    return y


def pitch_shift(x, semitones):
    """重采样法变调（同时改速度；±1–3 半音内可接受，SYS-AUDIO 创作性处理）。"""
    r = 2 ** (semitones / 12)
    n = int(round(len(x) / r))
    return signal.resample(x, n, axis=0) if abs(semitones) > 1e-6 else x


def loop_crossfade(seg, xf):
    """seg 长 L+xf；返回长 L 的首尾等功率交叉后的循环体（见 SYS-AUDIO 第 3 条）。"""
    L = len(seg) - xf
    out = seg[:L].copy()
    th = (np.arange(xf) / xf) * np.pi / 2
    out[:xf] = seg[:xf] * np.sin(th)[:, None] + seg[L:L + xf] * np.cos(th)[:, None]
    return out


def seam_ratio(x):
    """循环接缝：首尾样本差 ÷ 全文件 |一阶差分| 的 99.99 分位。≤ 2 合格（§14.2 第 5 条）。"""
    m = x.mean(axis=1)
    d = np.abs(np.diff(m))
    ref = np.percentile(d, 99.99)
    return float(abs(m[0] - m[-1]) / max(ref, 1e-12))


def seam_rms_diff_db(x, ms=50):
    n = int(ms * SR / 1000)
    a = np.sqrt((x[:n] ** 2).mean() + 1e-20)
    b = np.sqrt((x[-n:] ** 2).mean() + 1e-20)
    return float(abs(20 * np.log10(a / b)))


def frame_db(x, frame):
    m = x.mean(axis=1) if x.ndim > 1 else x
    n = len(m) // frame
    r = np.sqrt((m[: n * frame].reshape(n, frame) ** 2).mean(axis=1) + 1e-14)
    return 20 * np.log10(r)


def find_clean_window(x, win_s, step_s=1.0, hp=60):
    """在 x 里找长 win_s 的最干净段：无突出事件（高于中位电平多少 dB）、电平平稳。返回 (起点样本, 得分, 事件dB)。"""
    m = highpass(x, hp).mean(axis=1)
    fr = int(0.1 * SR)
    db = frame_db(m[:, None], fr)
    nfr = int(win_s * 10)
    if len(db) < nfr:
        return 0, 99.0, 99.0
    best = (None, 1e9, 0)
    for s in range(0, len(db) - nfr + 1, int(step_s * 10)):
        w = db[s:s + nfr]
        med = np.median(w)
        ev = np.percentile(w, 99.8) - med
        lv = w.reshape(-1, 10).mean(axis=1) if nfr % 10 == 0 else w
        sd = np.std(lv)
        score = ev * 1.0 + sd * 2.0
        if score < best[1]:
            best = (s * fr, score, ev)
    return best


def write_wav(path, x, bits=16):
    from scipy.io import wavfile
    os.makedirs(os.path.dirname(path), exist_ok=True)
    y = np.clip(x, -1.0, 1.0)
    if bits == 16:
        # TPDF 抖动
        rng = np.random.default_rng(1234)
        d = (rng.random(y.shape) - rng.random(y.shape)) / 32768.0
        y = np.round((y + d) * 32767).astype(np.int16)
    wavfile.write(path, SR, y if y.shape[1] > 1 else y[:, 0])


def write_ogg(path, x, quality):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ch = x.shape[1]
    data = np.clip(x, -1, 1).astype("<f4").tobytes()
    r = subprocess.run([FFMPEG, "-v", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", str(ch),
                        "-i", "-", "-c:a", "libvorbis", "-q:a", str(quality), "-ar", str(SR), path],
                       input=data, capture_output=True)
    if r.returncode != 0:
        raise RuntimeError(r.stderr.decode("utf8", "replace"))


def normalize_to(x, target_lufs, tp_limit=-1.0):
    """按 BS.1770 积分响度调到 target；真峰值超限则只压峰（软限幅），不再降响度之外改动。"""
    lu = integrated_lufs(x)
    g = 10 ** ((target_lufs - lu) / 20)
    y = x * g
    tp = true_peak_db(y)
    if tp > tp_limit:
        y = y * 10 ** ((tp_limit - tp) / 20)  # 床：宁可降一点，不削波
    return y, lu, true_peak_db(y)


def dc_offset(x):
    return float(np.abs(x.mean(axis=0)).max())


def limit(x, thr_db, release_ms=120.0, look_ms=3.0):
    """前瞻限幅：把瞬态压到 thr_db（dBFS，样本峰值）以内，平滑增益；用来驯服床里偶发的单个水滴/碰响。"""
    from scipy.ndimage import minimum_filter1d, uniform_filter1d
    thr = 10 ** (thr_db / 20)
    env = np.abs(x).max(axis=1)
    g = np.minimum(1.0, thr / np.maximum(env, 1e-9))
    look = max(1, int(look_ms * SR / 1000))
    g = minimum_filter1d(g, size=2 * look + 1)
    g = uniform_filter1d(g, size=2 * look + 1)
    # 释放：向后单边平滑（增益只允许缓慢回升）
    rel = np.exp(-1.0 / (release_ms * SR / 1000))
    out = g.copy()
    for_blocks = 4096
    # 向量化的单极点释放：g_s[n] = min(g[n], rel*g_s[n-1] + (1-rel))
    gs = np.empty_like(out)
    prev = 1.0
    for i in range(0, len(out), for_blocks):
        blk = out[i:i + for_blocks]
        for j in range(len(blk)):
            v = rel * prev + (1 - rel)
            prev = blk[j] if blk[j] < v else v
            gs[i + j] = prev
    return x * gs[:, None]


def rotate_best_seam(x, tries=600, seed=7):
    """循环体是环形连续的，旋转不引入新接缝；挑接缝两侧 50 ms RMS 差最小、样本差最小的旋转点。"""
    rng = np.random.default_rng(seed)
    n = len(x)
    m = x.mean(axis=1)
    d = np.abs(np.diff(m))
    ref = np.percentile(d, 99.99)
    w = int(0.05 * SR)
    best, bk = 1e9, 0
    c = np.concatenate([[0.0], np.cumsum(m ** 2)])
    def rms(a, b):
        a %= n; b = a + (b)
        idx = (np.arange(a, a + w)) % n
        return np.sqrt((m[idx] ** 2).mean() + 1e-20)
    for k in rng.integers(w, n - w, size=tries):
        a = rms(k - w, w)
        b = rms(k, w)
        rd = abs(20 * np.log10(a / b))
        sd = abs(m[k - 1] - m[k]) / max(ref, 1e-12)
        sc = rd + 2.0 * sd
        if sc < best:
            best, bk = sc, int(k)
    return np.roll(x, -bk, axis=0)

#!/usr/bin/env python3
"""程序合成的占位音：傍晚广播的旋律（八音盒音色，原创曲调）与单音铜钟。

自有版权（SYS-AUDIO §14.1 “程序合成”）。旋律为占位，正式的原创曲及录音由委托完成（§6.3、WP-53e 后续）。
产物：
  music/broadcast_melody_dry.ogg   干声，单声道
  music/broadcast_melody_mid.ogg   过喇叭频带（300 Hz–4 kHz）+ 很轻的房间感，“中”档
  music/broadcast_melody_far.ogg   更窄的频带 + 距离混响尾，“远”档
  bells/bell_strike_01.ogg         单音铜钟（低沉、余音 7 s），干声
"""
import argparse, os
import numpy as np
from scipy import signal
import audiolib as A

SR = A.SR
rng = np.random.default_rng(20261016)  # 固定种子：合成结果可复现

# 五声音阶（C 大调五声：C D E G A），音名到半音
SEMI = {"C": 0, "D": 2, "E": 4, "G": 7, "A": 9}


def hz(note, octave):
    return 440.0 * 2 ** ((SEMI[note] + 12 * (octave - 4) - 9) / 12)


def music_box(f, dur, vel=1.0):
    n = int((dur + 2.5) * SR)
    t = np.arange(n) / SR
    y = np.zeros(n)
    for k, (mul, amp, dec) in enumerate([(1, 1.0, 2.2), (2.0, 0.42, 1.4), (3.01, 0.22, 0.9), (4.02, 0.12, 0.6), (5.4, 0.06, 0.35)]):
        ff = f * mul
        if ff > 9000:
            continue
        y += amp * np.sin(2 * np.pi * ff * t + rng.random() * 0.3) * np.exp(-t / dec)
    # 梳齿的敲击瞬态：极短的高频噪声
    click = rng.standard_normal(n) * np.exp(-t / 0.004) * 0.05
    click = A.highpass(click[:, None], 3000, 2)[:, 0]
    y = (y + click) * np.minimum(t / 0.002, 1.0)
    return y * vel


def melody():
    bpm = 76
    beat = 60.0 / bpm
    # (音名, 八度, 拍数)；None 为休止。四个乐句，第四句收在主音上
    A1 = [("E", 5, 1), ("G", 5, 1), ("A", 5, 1), ("G", 5, 1), ("E", 5, 1), ("D", 5, 1), ("C", 5, 2),
          ("D", 5, 1), ("E", 5, 1), ("G", 5, 1), ("E", 5, 1), ("D", 5, 1), ("C", 5, 1), ("D", 5, 2)]
    B1 = [("G", 5, 1), ("A", 5, 1), ("C", 6, 1.5), ("A", 5, 0.5), ("G", 5, 1), ("E", 5, 1), ("D", 5, 2),
          ("E", 5, 1), ("G", 5, 1), ("A", 5, 1), ("G", 5, 1), ("E", 5, 1), ("D", 5, 1), ("C", 5, 3)]
    notes = A1 + B1 + A1[:-1] + [("D", 5, 1), ("C", 5, 3)] + B1[:-1] + [("C", 5, 4)]
    total = sum(d for _, _, d in notes) * beat + 4.0
    out = np.zeros(int(total * SR))
    t = 0.0
    for i, (nm, oc, d) in enumerate(notes):
        vel = 0.85 + 0.15 * (i % 2 == 0)
        y = music_box(hz(nm, oc), d * beat, vel)
        a = int(t * SR)
        out[a:a + len(y)] += y[: len(out) - a]
        t += d * beat
    return out[:, None]


def reverb_tail(x, rt60, wet, seed=5):
    """极简混响：衰减噪声脉冲响应卷积。仅用于“远”档，距离感的尾巴。"""
    r = np.random.default_rng(seed)
    n = int(rt60 * SR)
    ir = r.standard_normal(n) * np.exp(-6.9 * np.arange(n) / n)
    ir = A.lowpass(ir[:, None], 5000, 2)[:, 0]
    ir[: int(0.012 * SR)] *= 0.0  # 12 ms 预延迟
    ir /= np.sqrt((ir ** 2).sum())
    wet_sig = signal.fftconvolve(x[:, 0], ir)[: len(x) + n]
    out = np.zeros(len(wet_sig))
    out[: len(x)] += x[:, 0] * (1 - wet)
    out += wet_sig * wet
    return out[:, None]


def band(x, lo, hi):
    return A.lowpass(A.highpass(x, lo, 2), hi, 2)


def bell(f0=146.8, dur=8.0):
    """低沉的铜钟：非谐部分音（hum、prime、tierce、quint、nominal…），余音 6–8 s。"""
    n = int(dur * SR)
    t = np.arange(n) / SR
    parts = [(0.5, 0.55, 7.5), (1.0, 1.0, 6.5), (1.183, 0.7, 4.8), (1.506, 0.45, 3.6), (2.0, 0.6, 3.2), (2.514, 0.28, 2.2),
             (3.011, 0.22, 1.7), (4.166, 0.12, 1.1)]
    y = np.zeros(n)
    for mul, amp, dec in parts:
        y += amp * np.sin(2 * np.pi * f0 * mul * t + rng.random() * 6.28) * np.exp(-t / (dec * 0.5))
        y += 0.1 * amp * np.sin(2 * np.pi * (f0 * mul + 1.1) * t) * np.exp(-t / (dec * 0.5))  # 轻微拍频
    strike = A.lowpass(A.highpass(rng.standard_normal(n)[:, None], 400, 2), 3500, 2)[:, 0] * np.exp(-t / 0.03) * 0.25
    y = (y + strike) * np.minimum(t / 0.004, 1.0)
    return y[:, None]


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--out-dir", required=True)
    a = ap.parse_args()
    m = melody()
    dry = A.highpass(m, 120, 2)
    mid = reverb_tail(band(m, 300, 4000), 0.9, 0.15)
    far = reverb_tail(band(m, 400, 3200), 2.2, 0.45)
    for name, y, lufs in [("dry", dry, -26.0), ("mid", mid, -28.0), ("far", far, -31.0)]:
        y = y - y.mean()
        y, _, tp = A.normalize_to(y, lufs, tp_limit=-1.0)
        A.write_ogg(os.path.join(a.out_dir, "music", f"broadcast_melody_{name}.ogg"), y, 6)
    b = bell()
    b = A.highpass(b, 50, 2)
    b = A.fade(b, 2, 600)
    lm = A.momentary_max_lufs(b)
    b = b * 10 ** ((-24.0 - lm) / 20)
    if A.true_peak_db(b) > -3:
        b = b * 10 ** ((-3 - A.true_peak_db(b)) / 20)
    A.write_ogg(os.path.join(a.out_dir, "bells", "bell_strike_01.ogg"), b, 5)
    print("ok", round(len(m) / SR, 1), "s melody")

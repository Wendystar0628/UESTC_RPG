#!/usr/bin/env python3
"""一次性声入库：从长录音里切出单个事件（脚步、鸟叫、乌鸦…）→ 清理 → LUFS-M 对齐 → 48 kHz 单声道 WAV。

两种切法：
- "step"：按瞬态峰值切（脚步、碰撞）；
- "phrase"：按“安静间隙”切（鸟叫、乌鸦、蛙），一段乐句为一条。
用法: python build_oneshots.py --raw-dir <原料> --out-dir <game/assets/audio> --recipes oneshots.json --report <json> [--only set,set]
"""
import argparse, hashlib, json, os
import numpy as np
from scipy import signal
import audiolib as A

HOP = 96  # 2 ms


def env_db(x, band_hp, band_lp):
    y = x
    if band_hp:
        y = A.highpass(y[:, None], band_hp, 2)[:, 0]
    if band_lp:
        y = A.lowpass(y[:, None], band_lp, 2)[:, 0]
    n = len(y) // HOP
    fr = (y[: n * HOP] ** 2).reshape(n, HOP).mean(axis=1)
    # 4 ms 窗：相邻两帧平均
    fr = np.convolve(fr, [0.5, 0.5], mode="same")
    return 10 * np.log10(fr + 1e-14)


def segments_step(x, p):
    e = env_db(x, p.get("det_hp", 150), p.get("det_lp", 8000))
    floor = max(np.percentile(e, p.get("floor_pct", 15)), -75.0)
    mindist = int(p.get("min_gap_ms", 220) / 2)
    pk, props = signal.find_peaks(e, prominence=p.get("prom", 14), distance=mindist, height=floor + p.get("snr_min", 18))
    segs = []
    maxf = int(p.get("max_ms", 480) / 2)
    for i, k in enumerate(pk):
        top = e[k]
        s = k
        while s > 0 and e[s] > top - 28 and k - s < int(p.get("pre_max_ms", 90) / 2):
            s -= 1
        end = k
        nxt = pk[i + 1] if i + 1 < len(pk) else len(e)
        trunc = False
        while end < len(e) - 1 and end - k < maxf and e[end] > floor + 7:
            end += 1
            if end >= nxt - 3:
                trunc = True
                break
        # 停在下一次事件起点之前
        a = max(0, s * HOP - int(0.003 * A.SR))
        b = min(len(x), end * HOP)
        dur = (b - a) / A.SR
        if dur < p.get("min_ms", 120) / 1000:
            continue
        snr = top - floor
        tail_ok = e[min(end, len(e) - 1)] < floor + 12
        prev_ok = (i == 0) or (pk[i - 1] * 2 < s * 2 - 40) or True
        score = snr - (10 if trunc else 0) - (0 if tail_ok else 6)
        segs.append(dict(a=a, b=b, score=float(score), snr=float(snr), trunc=trunc))
    return segs, floor


def segments_phrase(x, p):
    e = env_db(x, p.get("det_hp", 1200), p.get("det_lp", 9000))
    floor = max(np.percentile(e, p.get("floor_pct", 20)), -75.0)
    thr = floor + p.get("snr_min", 10)
    act = e > thr
    # 合并小间隙
    gap = int(p.get("merge_ms", 140) / 2)
    idx = np.flatnonzero(act)
    if len(idx) == 0:
        return [], floor
    runs, s, prev = [], idx[0], idx[0]
    for i in idx[1:]:
        if i - prev > gap:
            runs.append((s, prev))
            s = i
        prev = i
    runs.append((s, prev))
    segs = []
    guard = int(p.get("guard_ms", 100) / 2)
    for (s, t) in runs:
        dur = (t - s) * 2 / 1000
        if dur < p.get("min_ms", 150) / 1000 or dur > p.get("max_ms", 3500) / 1000:
            continue
        pre = e[max(0, s - guard - 5):max(0, s - 5)]
        post = e[t + 5:t + 5 + guard]
        gdb = p.get("guard_snr", 8)
        quiet = (len(pre) == 0 or pre.mean() < floor + gdb) and (len(post) == 0 or post.mean() < floor + gdb)
        if not quiet:
            continue
        snr = float(e[s:t + 1].max() - floor)
        a = max(0, s * HOP - int(0.02 * A.SR))
        b = min(len(x), t * HOP + int(0.06 * A.SR))
        segs.append(dict(a=a, b=b, score=snr, snr=snr, trunc=False))
    return segs, floor


def clean(seg, p):
    y = seg.copy()
    if p.get("hp", 80):
        y = A.highpass(y[:, None], p.get("hp", 80), 2)[:, 0]
    if p.get("lp"):
        y = A.lowpass(y[:, None], p["lp"], 2)[:, 0]
    y = y[:, None]
    for f0, g, q in p.get("eq", []):
        y = A.peaking(y, f0, g, q)
    if p.get("pitch"):
        y = A.pitch_shift(y, p["pitch"])
    y = y - y.mean()
    y = A.fade(y, fin_ms=p.get("fin_ms", 2), fout_ms=p.get("fout_ms", 45))
    return y


def process_set(s, raw_dir, out_dir, ref_lufs_m):
    cands = []
    for src in s["srcs"]:
        path = os.path.join(raw_dir, f"{src}.ogg")
        if not os.path.exists(path):
            continue
        x = A.load(path, mono=True)[:, 0]
        if s.get("start") or s.get("end"):
            x = x[int(s.get("start", 0) * A.SR): int(s.get("end", len(x) / A.SR) * A.SR)]
        segs, floor = (segments_phrase if s["mode"] == "phrase" else segments_step)(x, s)
        for g in segs:
            g["src"] = src
            g["audio"] = x[g["a"]:g["b"]]
        cands += segs
    # 轮流从各来源取（保证来源多样），来源内按得分排
    bysrc = {}
    for g in sorted(cands, key=lambda g: -g["score"]):
        bysrc.setdefault(g["src"], []).append(g)
    for k in bysrc:
        bysrc[k] = bysrc[k][s.get("skip", 0):]
    chosen, i = [], 0
    while len(chosen) < s["n"] and any(bysrc.values()):
        for src in list(bysrc):
            if bysrc[src] and len(chosen) < s["n"]:
                chosen.append(bysrc[src].pop(0))
    # drop：人工复核后剔除的序号（1 起，按选中顺序），例如语音识别在某条里听出了人声
    chosen = [g for i, g in enumerate(chosen, start=1) if i not in s.get("drop", [])]
    outs = []
    for g in chosen:
        y = clean(g["audio"], s)
        y = np.vstack([np.zeros((int(0.002 * A.SR), 1)), y, np.zeros((int(0.03 * A.SR), 1))])
        outs.append((g, y))
    if not outs:
        return []
    lm = [A.momentary_max_lufs(y) for _, y in outs]
    med = float(np.median(lm))
    target = ref_lufs_m + s.get("rel_db", 0.0)
    reps = []
    for n, ((g, y), l) in enumerate(zip(outs, lm), start=1):
        y = y * 10 ** ((target - l) / 20)
        # 同组响度以组内中位数为准：每条只做对齐，不另压缩
        tp = A.true_peak_db(y)
        if tp > s.get("peak_db", -3.0):
            y = y * 10 ** ((s.get("peak_db", -3.0) - tp) / 20)
        name = f'{s["id"]}_{n:02d}'
        ext = ".ogg" if s.get("fmt") == "ogg" else ".wav"
        path = os.path.join(out_dir, s.get("dir") or ("foley" if s["id"].startswith("foot") else "animals"), name + ext)
        if ext == ".ogg":
            A.write_ogg(path, y, s.get("q", 4))
        else:
            A.write_wav(path, y)
        z = A.load(path, mono=False)
        reps.append(dict(file=name + ext, set=s["id"], src=g["src"], dur_ms=int(len(y) / A.SR * 1000),
                         lufs_m=round(A.momentary_max_lufs(z), 2), tp_db=round(A.true_peak_db(z), 2),
                         snr_db=round(g["snr"], 1), trunc=bool(g["trunc"]),
                         bytes=os.path.getsize(path), sha256=hashlib.sha256(open(path, "rb").read()).hexdigest()))
    return reps


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--raw-dir", required=True)
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--recipes", required=True)
    ap.add_argument("--report", required=True)
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    cfg = json.load(open(a.recipes, encoding="utf8"))
    only = set(filter(None, a.only.split(",")))
    rep = {}
    if os.path.exists(a.report):
        rep = {r["set"]: r for r in json.load(open(a.report, encoding="utf8"))}
    for s in cfg["sets"]:
        if only and s["id"] not in only:
            continue
        files = process_set({**cfg.get("defaults", {}).get(s["mode"], {}), **s}, a.raw_dir, a.out_dir,
                            s.get("ref_lufs_m", cfg.get("ref_lufs_m", {}).get(s["mode"], -24.0)))
        rep[s["id"]] = dict(set=s["id"], files=files)
        lm = [f["lufs_m"] for f in files]
        print(f'{s["id"]}: {len(files)}/{s["n"]}  LUFS-M {np.round(lm, 1).tolist()}  trunc={sum(f["trunc"] for f in files)}', flush=True)
    json.dump(list(rep.values()), open(a.report, "w", encoding="utf8"), ensure_ascii=False, indent=1)

#!/usr/bin/env python3
"""床（长循环）入库：选最干净的一段 → 滤波 → 等功率首尾交叉 → 标到 -30 LUFS → Ogg。

用法: python build_beds.py --raw-dir <原料> --out-dir <game/assets/audio> --recipes <beds.json> --report <json> [--only id,id]
配方字段见 beds.json；每条产出 <out>/beds/<id>.ogg 与一行体检记录（写入 --report）。
产出：--out-dir 指定目录内的 beds/；--report 指定的 JSON 文件（父目录由调用方传入）。
"""
import argparse, hashlib, json, os, sys
import numpy as np
import audiolib as A


def process(r, raw_dir, out_dir):
    """床里突出事件（比中位电平高 12 dB 以上的瞬态）每分钟不得超过 1 个（G21a）；超了就缩短窗口再找更干净的一段。"""
    import check_assets as C
    r = dict(r)
    for attempt in range(4):
        rep = _process(r, raw_dir, out_dir)
        ev = C.events_per_min(A.load(os.path.join(out_dir, "beds", f'{r["id"]}.ogg'), mono=False))
        rep["events_per_min"] = round(float(ev), 2)
        if ev <= 1.0 or "start" in r or r.get("len", 90.0) <= 24:
            break
        r["len"] = max(24.0, r.get("len", 90.0) * 0.7)
        print(f'  {r["id"]}: 事件 {ev:.1f}/min，缩短到 {r["len"]:.0f} s 重找', flush=True)
    if rep["events_per_min"] > 1.0:
        os.remove(os.path.join(out_dir, "beds", f'{r["id"]}.ogg'))
        rep["rejected"] = True
    return rep


def _process(r, raw_dir, out_dir):
    src = os.path.join(raw_dir, f'{r["src"]}.ogg')
    x = A.load(src, mono=r.get("mono", False))
    if r.get("mono_out"):
        x = x.mean(axis=1, keepdims=True)
    total = len(x) / A.SR
    xf = r.get("xf", 4.0)
    want = min(r.get("len", 90.0), total - xf - 0.5)
    # 先用高通后的信号找干净窗，再在原信号上取同一段
    if "start" in r:
        s0 = int(r["start"] * A.SR)
        score, ev = 0.0, 0.0
    else:
        s0, score, ev = A.find_clean_window(x, want + xf, step_s=1.0, hp=r.get("hp", 40))
    seg = x[s0: s0 + int((want + xf) * A.SR)]
    if r.get("hp", 40):
        seg = A.highpass(seg, r.get("hp", 40), order=2)
    if r.get("lp"):
        seg = A.lowpass(seg, r["lp"], order=2)
    for f0, g, q in r.get("eq", []):
        seg = A.peaking(seg, f0, g, q)
    if r.get("pitch"):
        seg = A.pitch_shift(seg, r["pitch"])
    loop = A.loop_crossfade(seg, int(xf * A.SR))
    loop = loop - loop.mean(axis=0, keepdims=True)  # 去直流
    tgt = r.get("lufs", -30.0)
    loop, lu_in, tp = A.normalize_to(loop, tgt, tp_limit=-1.0)
    crest = r.get("crest", 17.0)  # 样本峰值最多高于积分响度多少 dB；超出的单个瞬态用限幅压下去（G21a 的“床里不放突出事件”）
    for _ in range(2):
        loop = A.limit(loop, tgt + crest)
        loop, _, tp = A.normalize_to(loop, tgt, tp_limit=-1.0)
    loop = A.rotate_best_seam(loop, tries=r.get("seam_tries", 600))
    path = os.path.join(out_dir, "beds", f'{r["id"]}.ogg')
    q = r.get("q", 5 if loop.shape[1] == 2 else 4)
    A.write_ogg(path, loop, q)
    # 回读检查（编码后的实际文件）；有损编码对窄带内容会改变响度，偏差超过 0.4 dB 就补偿一次重编
    y = A.load(path, mono=False)
    off = tgt - A.integrated_lufs(y)
    if abs(off) > 0.4:
        loop = loop * 10 ** (off / 20)
        A.write_ogg(path, loop, q)
        y = A.load(path, mono=False)
    rep = dict(id=r["id"], src=r["src"], start_s=round(s0 / A.SR, 1), len_s=round(len(loop) / A.SR, 1),
               ch=loop.shape[1], lufs=round(A.integrated_lufs(y), 2), tp_db=round(A.true_peak_db(y), 2),
               dc=round(A.dc_offset(loop), 6), seam=round(A.seam_ratio(y), 3),
               seam_rms_db=round(A.seam_rms_diff_db(y), 2), event_db=round(float(ev), 1),
               bytes=os.path.getsize(path), sha256=hashlib.sha256(open(path, "rb").read()).hexdigest())
    return rep


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--raw-dir", required=True)
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--recipes", required=True)
    ap.add_argument("--report", required=True)
    ap.add_argument("--only", default="")
    ap.add_argument("--jobs", type=int, default=4)
    a = ap.parse_args()
    recipes = json.load(open(a.recipes, encoding="utf8"))["beds"]
    only = set(filter(None, a.only.split(",")))
    reps = []
    if os.path.exists(a.report):
        reps = json.load(open(a.report, encoding="utf8"))
    byid = {r["id"]: r for r in reps}
    todo = [r for r in recipes if (not only or r["id"] in only)]
    for r in list(todo):
        if not os.path.exists(os.path.join(a.raw_dir, f'{r["src"]}.ogg')):
            print("missing raw", r["src"], flush=True)
            todo.remove(r)
    from concurrent.futures import ProcessPoolExecutor
    with ProcessPoolExecutor(max_workers=a.jobs) as ex:
        futs = [ex.submit(process, r, a.raw_dir, a.out_dir) for r in todo]
        for f in futs:
            rep = f.result()
            byid[rep["id"]] = rep
            print(json.dumps(rep, ensure_ascii=False), flush=True)
    json.dump(list(byid.values()), open(a.report, "w", encoding="utf8"), ensure_ascii=False, indent=1)

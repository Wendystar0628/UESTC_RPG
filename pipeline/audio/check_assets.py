#!/usr/bin/env python3
"""G21a 的素材侧检查：每个文件都有登记、许可在白名单、格式与响度合格、床循环接缝合格、床里没有突出事件。

用法: python check_assets.py --assets <game/assets/audio> --licenses <licenses.json> [--out report.json]
退出码：有失败项则为 1。
"""
import argparse, hashlib, json, os, sys
import numpy as np
import audiolib as A

WHITELIST = {"CC0 1.0", "CC BY 3.0", "CC BY 4.0", "SELF-SYNTH"}


def events_per_min(x, over_db=12.0):
    """床里比中位电平高 over_db 以上的瞬态（100 ms 帧上的上升沿）每分钟个数。"""
    m = A.highpass(x, 60).mean(axis=1)
    db = A.frame_db(m[:, None], int(0.1 * A.SR))
    med = np.median(db)
    hot = db > med + over_db
    ups = np.count_nonzero(hot[1:] & ~hot[:-1]) + (1 if hot[0] else 0)
    return ups / (len(m) / A.SR / 60.0)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--assets", required=True)
    ap.add_argument("--licenses", required=True)
    ap.add_argument("--out", default="")
    a = ap.parse_args()
    lic = json.load(open(a.licenses, encoding="utf8"))
    srcs = {s["id"]: s for s in lic["sources"]}
    files = {f["path"]: f for f in lic["files"]}
    fails, warns, rows = [], [], []
    seen = set()
    for root, _, names in os.walk(a.assets):
        for n in sorted(names):
            if not n.lower().endswith((".wav", ".ogg")):
                continue
            full = os.path.join(root, n)
            rel = os.path.relpath(full, a.assets).replace("\\", "/")
            seen.add(rel)
            rec = files.get(rel)
            if rec is None:
                fails.append(f"{rel}: 没有登记")
                continue
            data = open(full, "rb").read()
            if hashlib.sha256(data).hexdigest() != rec["sha256"]:
                fails.append(f"{rel}: SHA-256 与登记不符")
            for sid in rec["source_ids"]:
                s = srcs.get(sid)
                if not s:
                    fails.append(f"{rel}: 来源 {sid} 未登记")
                elif s["license"] not in WHITELIST:
                    fails.append(f"{rel}: 许可 {s['license']} 不在白名单")
                elif s["license"].startswith("CC BY") and not s.get("attribution"):
                    fails.append(f"{rel}: CC BY 缺署名文本")
            sr, ch, dur = A.probe(full)
            x = A.load(full, mono=False)
            row = dict(path=rel, sr=sr, ch=ch, dur=round(len(x) / A.SR, 2))
            if sr != A.SR:
                fails.append(f"{rel}: 采样率 {sr}")
            tp = A.true_peak_db(x)
            row["tp"] = round(tp, 2)
            if tp > -1.0 + 0.05:
                fails.append(f"{rel}: 真峰值 {tp:.2f} dBTP > -1")
            dc = A.dc_offset(x)
            if dc > 0.002:
                fails.append(f"{rel}: 直流 {dc:.4f}")
            top = rel.split("/")[0]
            if top == "beds":
                lu = A.integrated_lufs(x)
                row["lufs"] = round(lu, 2)
                if not (-31.0 <= lu <= -29.0):
                    fails.append(f"{rel}: 积分响度 {lu:.2f}（应 -30 ±1）")
                sm = A.seam_ratio(x)
                sd = A.seam_rms_diff_db(x)
                row["seam"], row["seam_rms_db"] = round(sm, 3), round(sd, 2)
                if sm > 2.0:
                    fails.append(f"{rel}: 循环接缝 {sm:.2f} > 2")
                if sd > 1.0:
                    warns.append(f"{rel}: 接缝两侧 50 ms RMS 差 {sd:.2f} dB > 1")
                ev = events_per_min(x)
                row["events_per_min"] = round(ev, 2)
                if ev > 1.0:
                    fails.append(f"{rel}: 床里突出事件 {ev:.1f}/min > 1")
            elif top in ("foley", "animals", "weather", "props"):
                if ch != 1:
                    fails.append(f"{rel}: 3D 用的一律单声道，实际 {ch} 声道")
                row["lufs_m"] = round(A.momentary_max_lufs(x), 2)
                end = x[-int(0.02 * A.SR):]
                if 20 * np.log10(np.abs(end).max() + 1e-9) > -50:
                    warns.append(f"{rel}: 尾部 20 ms 未淡到 -50 dBFS 以下")
            rows.append(row)
    for p in files:
        if p not in seen:
            fails.append(f"{p}: 登记了但文件不存在")
    rep = dict(files=len(rows), fails=fails, warns=warns, rows=rows)
    if a.out:
        json.dump(rep, open(a.out, "w", encoding="utf8"), ensure_ascii=False, indent=1)
    print(f"检查 {len(rows)} 个文件：失败 {len(fails)}，警告 {len(warns)}")
    for f in fails:
        print("FAIL", f)
    for w in warns[:40]:
        print("warn", w)
    sys.exit(1 if fails else 0)


if __name__ == "__main__":
    main()

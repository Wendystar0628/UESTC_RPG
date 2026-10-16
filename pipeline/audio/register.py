#!/usr/bin/env python3
"""登记：把入库结果写成 game/data/audio/licenses.json 与 game/assets/audio/index.json，
并给床和音乐的 .import 打上循环参数（SYS-AUDIO §15）。

用法: python register.py --assets <game/assets/audio> --licenses-out <licenses.json> --snap-dir <快照目录>
      --beds-rep <json> --beds-recipes <json> --os-rep <json> --os-recipes <json> [--synth-rep <json>]
"""
import argparse, datetime, hashlib, json, os, re
import audiolib as A


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--assets", required=True)
    ap.add_argument("--licenses-out", required=True)
    ap.add_argument("--snap-dir", required=True)
    ap.add_argument("--beds-rep", required=True)
    ap.add_argument("--beds-recipes", required=True)
    ap.add_argument("--os-rep", required=True)
    ap.add_argument("--os-recipes", required=True)
    a = ap.parse_args()

    snaps = {}
    for fn in os.listdir(a.snap_dir):
        if fn.endswith(".json"):
            s = json.load(open(os.path.join(a.snap_dir, fn), encoding="utf8"))
            snaps[str(s["id"])] = s
    beds_rep = {r["id"]: r for r in json.load(open(a.beds_rep, encoding="utf8"))}
    beds_rec = {r["id"]: r for r in json.load(open(a.beds_recipes, encoding="utf8"))["beds"]}
    os_rep = {r["set"]: r for r in json.load(open(a.os_rep, encoding="utf8"))}
    os_cfg = json.load(open(a.os_recipes, encoding="utf8"))
    os_rec = {s["id"]: s for s in os_cfg["sets"]}

    files, used, index = [], {}, dict(schema=1, generated=datetime.date.today().isoformat(), beds=[], sets=[], music=[], bells=[])

    def add_file(rel, srcs, proc):
        p = os.path.join(a.assets, rel)
        files.append(dict(path=rel, sha256=sha(p), bytes=os.path.getsize(p), source_ids=[f"fs-{s}" if s.isdigit() else s for s in srcs], processing=proc))
        for s in srcs:
            used.setdefault(f"fs-{s}" if s.isdigit() else s, []).append(rel)

    # 床
    for bid, r in sorted(beds_rep.items()):
        rec = beds_rec[bid]
        rel = f"beds/{bid}.ogg"
        if not os.path.exists(os.path.join(a.assets, rel)):
            continue
        proc = (f"取原片 {r['start_s']}–{r['start_s'] + r['len_s'] + rec.get('xf', 4.0):.1f} s 的最干净一段；高通 {rec.get('hp', 40)} Hz；"
                f"首尾 {rec.get('xf', 4.0)} s 等功率交叉成循环；限幅（峰值不高于积分响度 + {rec.get('crest', 17.0)} dB）；"
                f"旋转到接缝最平处；标到 -30 LUFS；Ogg Vorbis q{rec.get('q', 5 if r['ch'] == 2 else 4)}")
        add_file(rel, [r["src"]], proc)
        index["beds"].append(dict(id=bid, file=rel, loop=True, channels=r["ch"], duration_s=r["len_s"],
                                  lufs=r["lufs"], note=rec.get("note", "")))
    # 一次性声
    for sid, r in sorted(os_rep.items()):
        rec = os_rec[sid]
        mode = rec["mode"]
        dflt = os_cfg.get("defaults", {}).get(mode, {})
        hp = rec.get("hp", dflt.get("hp"))
        folder = rec.get("dir") or ("foley" if sid.startswith("foot") else "animals")
        kind = rec.get("kind") or ("foot" if sid.startswith("foot") else "bird")
        entry = dict(id=sid, kind=kind, mono=True, files=[], rel_db=rec.get("rel_db", 0.0),
                     ref_lufs_m=rec.get("ref_lufs_m", os_cfg["ref_lufs_m"][mode]) + rec.get("rel_db", 0.0))
        for f in r["files"]:
            rel = f"{folder}/{f['file']}"
            if not os.path.exists(os.path.join(a.assets, rel)):
                continue
            proc = (f"{'按瞬态' if mode == 'step' else '按安静间隙'}从原片切出单个事件；高通 {hp} Hz" +
                    (f"；低通 {rec['lp']} Hz" if rec.get("lp") else "") +
                    (f"；变调 {rec['pitch']:+} 半音" if rec.get("pitch") else "") +
                    "；起 2 ms、尾 45 ms 淡变并补 30 ms 静音；LUFS-M（400 ms 窗补零）对齐到组目标；48 kHz 单声道 16 bit WAV")
            add_file(rel, [f["src"]], proc)
            entry["files"].append(dict(file=rel, duration_ms=f["dur_ms"], lufs_m=f["lufs_m"]))
        index["sets"].append(entry)
    # 合成的占位
    synth = "SELF-SYNTH:pipeline/audio/synth_broadcast.py"
    for rel, kind in [("music/broadcast_melody_dry.ogg", "melody"), ("music/broadcast_melody_mid.ogg", "melody"),
                      ("music/broadcast_melody_far.ogg", "melody"), ("bells/bell_strike_01.ogg", "bell")]:
        p = os.path.join(a.assets, rel)
        if not os.path.exists(p):
            continue
        sr, ch, dur = A.probe(p)
        add_file(rel, [synth], "程序合成（pipeline/audio/synth_broadcast.py，固定种子，可复现）")
        index["music" if kind == "melody" else "bells"].append(dict(file=rel, duration_s=round(dur, 1), channels=ch,
            placeholder=True, note="原创曲调占位；正式曲目与录音由委托完成（SYS-AUDIO §6.3）" if kind == "melody" else "单音铜钟，占位"))

    sources = []
    for sid, rels in sorted(used.items()):
        if sid.startswith("SELF-SYNTH"):
            sources.append(dict(id=sid, source="自制（程序合成）", url="pipeline/audio/synth_broadcast.py", title="广播旋律与铜钟（占位）",
                                author="UESTC_RPG", license="SELF-SYNTH", license_url="", downloaded="", snapshot="", sha256="",
                                attribution="", note="自有版权，保留所有权利（OPS-RELEASE §7）", used_in=sorted(rels)))
            continue
        n = sid[3:]
        s = snaps[n]
        sources.append(dict(id=sid, source="Freesound", url=s["page_url"], title=s["title"], author=s["author"],
                            license="CC0 1.0", license_url=s["license_url"], downloaded=s["downloaded"],
                            snapshot=f"pipeline/audio/snapshots/{n}.json", retrieved_as=s["source_kind"],
                            original_name=s["title"], sha256=s["sha256"], attribution="", used_in=sorted(rels)))
    lic = dict(schema=1, generated=datetime.date.today().isoformat(),
               note="来源与许可登记（SYS-AUDIO §14.2 第 1 步）。Freesound 素材取自网站公开的 HQ 预览（48 kHz Ogg Vorbis），"
                    "原始无损文件需登录下载，登录后可在不改文件名的前提下替换母带。sha256 是下载文件的摘要。",
               sources=sources, files=sorted(files, key=lambda f: f["path"]))
    os.makedirs(os.path.dirname(a.licenses_out), exist_ok=True)
    json.dump(lic, open(a.licenses_out, "w", encoding="utf8"), ensure_ascii=False, indent=1)
    json.dump(index, open(os.path.join(a.assets, "index.json"), "w", encoding="utf8"), ensure_ascii=False, indent=1)
    print(f"登记 {len(files)} 个文件，{len(sources)} 个来源")

    # .import 的循环参数：床与广播旋律以外的音乐
    pat = re.compile(r"^loop=false$", re.M)
    n = 0
    for e in index["beds"]:
        imp = os.path.join(a.assets, e["file"] + ".import")
        if os.path.exists(imp):
            t = open(imp, encoding="utf8").read()
            if pat.search(t):
                open(imp, "w", encoding="utf8", newline="\n").write(pat.sub("loop=true", t))
                n += 1
    print(f"改写 {n} 个 .import 为循环")


if __name__ == "__main__":
    main()

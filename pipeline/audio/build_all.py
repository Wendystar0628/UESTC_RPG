#!/usr/bin/env python3
"""第一批素材的整条入库流程（开发机上运行，产物可复现）。

前置：ffmpeg 在 PATH，Python 有 numpy、scipy。
步骤：下载（beds.json 与 oneshots.json 里出现过的 Freesound ID）→ 床 → 一次性声 → 合成占位 → 登记 → 体检。
用法: python build_all.py --raw-dir <原料目录，不进 git> --repo <仓库根> [--skip-fetch]
Godot 导入（生成 .import、给床写循环）由 register.py 之后手动跑：
  Godot --headless --path game --import   （两遍：第一遍生成，register 改循环参数，第二遍应用）
"""
import argparse, json, os, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))


def run(*args):
    print(">", " ".join(args), flush=True)
    subprocess.run([sys.executable, *args], check=True, cwd=HERE)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--raw-dir", required=True)
    ap.add_argument("--repo", required=True)
    ap.add_argument("--skip-fetch", action="store_true")
    a = ap.parse_args()
    assets = os.path.join(a.repo, "game", "assets", "audio")
    snaps = os.path.join(HERE, "snapshots")
    work = os.path.join(a.repo, "output", "audio-build")
    os.makedirs(work, exist_ok=True)
    beds = json.load(open(os.path.join(HERE, "beds.json"), encoding="utf8"))
    ones = json.load(open(os.path.join(HERE, "oneshots.json"), encoding="utf8"))
    ids = sorted({b["src"] for b in beds["beds"]} | {s for st in ones["sets"] for s in st["srcs"]})
    if not a.skip_fetch:
        run("fetch.py", "--raw-dir", a.raw_dir, "--snap-dir", snaps, *ids)
    run("build_beds.py", "--raw-dir", a.raw_dir, "--out-dir", assets, "--recipes", "beds.json", "--report", os.path.join(work, "beds_rep.json"))
    run("build_oneshots.py", "--raw-dir", a.raw_dir, "--out-dir", assets, "--recipes", "oneshots.json", "--report", os.path.join(work, "os_rep.json"))
    run("synth_broadcast.py", "--out-dir", assets)
    run("register.py", "--assets", assets, "--licenses-out", os.path.join(a.repo, "game", "data", "audio", "licenses.json"),
        "--snap-dir", snaps, "--beds-rep", os.path.join(work, "beds_rep.json"), "--beds-recipes", "beds.json",
        "--os-rep", os.path.join(work, "os_rep.json"), "--os-recipes", "oneshots.json")
    run("check_assets.py", "--assets", assets, "--licenses", os.path.join(a.repo, "game", "data", "audio", "licenses.json"),
        "--out", os.path.join(work, "check.json"))


if __name__ == "__main__":
    main()

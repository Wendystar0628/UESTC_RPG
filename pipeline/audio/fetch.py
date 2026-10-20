#!/usr/bin/env python3
"""下载 Freesound CC0 素材（公开 HQ 预览，无需登录）并存页面快照。仅开发机使用。

用法: python fetch.py --raw-dir <目录> --snap-dir <目录> ID [ID ...]
- 只接受页面里许可链接为 publicdomain/zero 的声音；其余一律拒绝。
- 预览是 Freesound 提供的 48 kHz Ogg Vorbis（约 192 kbps）；原始无损文件要登录才能下，
  登录由作者完成后可替换（见 licenses.json 的 `source_kind`）。
- 每条写 snap-dir/<id>.json：标题、作者、许可链接、URL、下载日期、SHA-256。
产出：--raw-dir 指定的原料目录；--snap-dir 指定的页面快照目录（父目录由调用方传入）。
"""
import argparse, datetime, hashlib, html, json, os, re, sys, time, urllib.request

UA = "Mozilla/5.0 (UESTC_RPG audio pipeline)"


def get(url, timeout=90):
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.geturl(), r.read()


def fetch_one(sid, raw_dir, snap_dir):
    snap_path = os.path.join(snap_dir, f"{sid}.json")
    if os.path.exists(snap_path):
        return json.load(open(snap_path, encoding="utf8"))
    final, body = get(f"https://freesound.org/s/{sid}/")
    page = body.decode("utf8", "replace")
    lic = re.search(r'https?://creativecommons\.org/(publicdomain/zero/1\.0|licenses/[a-z\-]+/[0-9.]+)/?', page)
    if not lic or "publicdomain/zero/1.0" not in lic.group(0):
        raise SystemExit(f"{sid}: 许可不是 CC0（{lic.group(0) if lic else '未找到'}），拒绝")
    og = lambda k: html.unescape((re.search(r'property="og:' + k + r'" content="([^"]*)"', page) or [0, ""])[1])
    audio = re.search(r'previews/(\d+)/(\d+_\d+)-lq\.mp3', page)
    title = og("audio:title")
    artist = og("audio:artist")
    hq = f"https://cdn.freesound.org/previews/{audio.group(1)}/{audio.group(2)}-hq.ogg"
    _, data = get(hq)
    os.makedirs(raw_dir, exist_ok=True)
    fn = os.path.join(raw_dir, f"{sid}.ogg")
    open(fn, "wb").write(data)
    meta = dict(id=int(sid), title=title, author=artist, page_url=final, license="CC0 1.0",
                license_url="https://creativecommons.org/publicdomain/zero/1.0/",
                preview_url=hq, source_kind="freesound-hq-preview-ogg-48k",
                downloaded=datetime.date.today().isoformat(), bytes=len(data),
                sha256=hashlib.sha256(data).hexdigest())
    os.makedirs(snap_dir, exist_ok=True)
    json.dump(meta, open(snap_path, "w", encoding="utf8"), ensure_ascii=False, indent=1)
    return meta


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--raw-dir", required=True)
    ap.add_argument("--snap-dir", required=True)
    ap.add_argument("--jobs", type=int, default=5)
    ap.add_argument("ids", nargs="+")
    a = ap.parse_args()
    from concurrent.futures import ThreadPoolExecutor

    def job(sid):
        for attempt in range(3):
            try:
                m = fetch_one(sid, a.raw_dir, a.snap_dir)
                return f'{sid} ok {m["bytes"]//1024} KB | {m["author"]} | {m["title"][:60]}'
            except SystemExit as e:
                return str(e)
            except Exception as e:
                err = e
                time.sleep(2)
        return f"{sid} FAIL {err}"

    with ThreadPoolExecutor(max_workers=a.jobs) as ex:
        for line in ex.map(job, a.ids):
            print(line, flush=True)

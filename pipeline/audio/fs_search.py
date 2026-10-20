#!/usr/bin/env python3
"""Freesound CC0 搜索（无需登录的公开搜索页）。仅开发机使用。

用法: python fs_search.py "query" [--min 20] [--max 600] [--pages 1] [--sort downloads]
输出: id | 用户 | 时长 | 采样率 | 下载数 | 标题
产出：无（读取公开搜索页并打印结果）。
"""
import argparse, html, re, sys, time, urllib.parse, urllib.request

UA = "Mozilla/5.0 (UESTC_RPG audio pipeline; contact: repo owner)"


def fetch(url, timeout=40):
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read()


def search(query, dmin=0, dmax="*", page=1, sort="downloads desc", lic='"Creative Commons 0"'):
    f = f'license:{lic} duration:[{dmin} TO {dmax}]'
    qs = urllib.parse.urlencode({"q": query, "f": f, "s": sort, "page": page})
    h = fetch("https://freesound.org/search/?" + qs).decode("utf8", "replace")
    out = []
    for m in re.finditer(r'<div\s+class="bw-player"(.*?)tabindex', h, re.S):
        blk = m.group(1)
        g = lambda k: (re.search(k + r'="([^"]*)"', blk) or [None, ""])[1]
        out.append(dict(id=g("data-sound-id"), user=g("data-username"), uid=g("data-user-id"),
                        title=html.unescape(g("data-title")), dur=float(g("data-duration") or 0),
                        sr=g("data-samplerate"), dl=g("data-num-downloads")))
    return out


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("query")
    ap.add_argument("--min", default="0")
    ap.add_argument("--max", default="*")
    ap.add_argument("--pages", type=int, default=1)
    ap.add_argument("--sort", default="downloads desc")
    a = ap.parse_args()
    for p in range(1, a.pages + 1):
        for r in search(a.query, a.min, a.max, p, a.sort):
            print(f'{r["id"]:>8} | {r["user"][:16]:16} | {r["dur"]:7.1f}s | {r["sr"][:6]:6} | {r["dl"]:>6} | {r["title"]}')
        time.sleep(1)

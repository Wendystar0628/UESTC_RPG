#!/usr/bin/env python3
"""语音识别体检（G21a：入库和成品都过语音识别，不许听出人声）。仅开发机使用。

需要 faster-whisper（在独立 venv 里装，不进仓库依赖）与本机已缓存的模型：
  python asr_check.py --assets <game/assets/audio> --model <模型目录> [--only beds,animals] --out <json>
Whisper 在纯噪声上会幻觉出短语，所以只作“可疑清单”：输出每个有识别结果的文件、文本、no_speech_prob 与 avg_logprob，由人复核。
"""
import argparse, json, os, subprocess, sys, time
import numpy as np
from faster_whisper import WhisperModel


def decode(path):
    r = subprocess.run(['ffmpeg', '-v', 'error', '-i', path, '-f', 'f32le', '-ar', '16000', '-ac', '1', '-'], capture_output=True)
    return np.frombuffer(r.stdout, dtype=np.float32)


ap = argparse.ArgumentParser()
ap.add_argument("--assets", required=True)
ap.add_argument("--model", required=True)
ap.add_argument("--only", default="")
ap.add_argument("--device", default="cpu")
ap.add_argument("--out", required=True)
a = ap.parse_args()
only = [s for s in a.only.split(",") if s]
m = WhisperModel(a.model, device=a.device, compute_type="int8" if a.device == "cpu" else "float16")
res = {}
if os.path.exists(a.out):
    res = json.load(open(a.out, encoding="utf8"))
t0 = time.time()
for root, _, names in os.walk(a.assets):
    for n in sorted(names):
        if not n.endswith((".wav", ".ogg")):
            continue
        rel = os.path.relpath(os.path.join(root, n), a.assets).replace("\\", "/")
        if only and rel.split("/")[0] not in only:
            continue
        if rel in res:
            continue
        segs, info = m.transcribe(decode(os.path.join(root, n)), vad_filter=False, beam_size=1, condition_on_previous_text=False,
                                  no_speech_threshold=0.6, log_prob_threshold=-1.0, temperature=0.0)
        out = [dict(t=round(s.start, 1), text=s.text.strip(), nsp=round(s.no_speech_prob, 2), lp=round(s.avg_logprob, 2)) for s in segs if s.text.strip()]
        res[rel] = dict(lang=info.language, p=round(info.language_probability, 2), segs=out)
        print(rel, len(out), flush=True)
        json.dump(res, open(a.out, "w", encoding="utf8"), ensure_ascii=False, indent=1)
print("done", round(time.time() - t0), "s")

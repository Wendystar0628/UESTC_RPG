"""将分析器已确认的样本写成限量曲线与高耗时帧投影。
用法：由 analyze.write_session 调用；产出 derived/<id>/curves.json，展示层只选择现成档位。
"""
import math
import statistics
from collections import defaultdict
from perf_model import CALC, at, frame_rate, normalized_gpu_load, number

CURVE_LIMITS = (900, 450, 225, 112, 56)
HOTFRAME_LIMIT = 1000
EXPERIENCE_REASON = '隔离桌面只判成本，不测帧时与帧率'
PHASE_LABELS = dict(physMs='物理区间', procMs='处理区间', lateMs='处理后区间', drawMs='绘制区间', tailMs='绘制后区间')

SERIES = [('dtMs', '帧时', 'ms', 'experience'), ('mainViewportGpuMs', '主视口 GPU', 'ms', 'cost'),
          ('mainViewportCpuMs', '视口 CPU 渲染', 'ms', 'cost'), ('frameSetupCpuMs', '帧准备 CPU', 'ms', 'cost'),
          ('gpuUtilFraction', 'GPU 占用', '0–1', 'experience'), ('wholeGpuMemoryMiB', 'NVML 整卡已用显存', 'MiB', 'cost'),
          ('engineVideoMemoryMiB', '引擎显存记账', 'MiB', 'cost'), ('powerW', '功耗', 'W', 'cost'),
          ('gpuClockMHz', '显卡频率', 'MHz', 'cost'), ('temperatureC', '显卡温度', '°C', 'cost'),
          *[(key, label, 'ms', 'cost') for key, label in PHASE_LABELS.items()],
          ('gcPauseMs', '垃圾回收停顿', 'ms', 'cost'), ('allocatedBytes', '全进程分配', 'B', 'cost'),
          ('fps', '帧率', 'FPS', 'experience'), ('measuredViewportGpuMs', '已确认受测视口 GPU 合计', 'ms', 'cost'),
          ('normalizedGpuLoad', '归一化 GPU 负载', '倍', 'experience'),
          ('gpuUtilSampleAgeMs', '占用读数原生样本龄', 'ms', 'experience'),
          ('gpuUtilSamplingWindowMs', '占用读数原生采样窗口', 'ms', 'experience')]


def bucket_curves(summary, manifest, streams, limit):
    frames = streams['frames']
    start = at(manifest, 'clock.recordingStartMs')
    start = start if number(start) else frames[0]['monotonicMs']
    stop = max([start] + [row['monotonicMs'] for name in ('frames', 'samples', 'hw') for row in streams[name]])
    seconds = max(1, math.ceil((stop - start) / 1000 / limit))
    groups = defaultdict(lambda: defaultdict(list))
    counts = defaultdict(int)
    allowed = {key for key, _, _, scope in SERIES if scope == 'cost' or summary['scope'] == 'full'}
    for name in ('frames', 'samples', 'hw'):
        for row in streams[name]:
            bucket = min(limit - 1, max(0, int((row['monotonicMs'] - start) / (seconds * 1000))))
            if name == 'frames':
                counts[bucket] += 1
                if 'measuredViewportGpuMs' in allowed and number(row['_measuredGpuMs']):
                    groups[bucket]['measuredViewportGpuMs'].append(row['_measuredGpuMs'])
                if 'fps' in allowed and row['_dtValid'] and row['dtMs'] > 0:
                    groups[bucket]['fps'].append(row['dtMs'])
            if name == 'hw' and 'normalizedGpuLoad' in allowed:
                value = normalized_gpu_load(row)
                if value is not None:
                    groups[bucket]['normalizedGpuLoad'].append(value)
            for key in allowed:
                if not number(row.get(key)):
                    continue
                if name == 'frames' and ((key == 'dtMs' and not row['_dtValid']) or
                                         (key in PHASE_LABELS and not row['_phaseValid'])):
                    continue
                groups[bucket][key].append(row[key])
    buckets = []
    for bucket in range(min(limit, int((stop - start) / (seconds * 1000)) + 1)):
        values = [round(start + bucket * seconds * 1000, 3), round(min(stop, start + (bucket + 1) * seconds * 1000), 3), counts[bucket]]
        for key, *_ in SERIES:
            known = groups[bucket][key]
            if key == 'fps':
                mean, maximum = (frame_rate(known), frame_rate([min(known)])) if known else (None, None)
            else:
                mean, maximum = (statistics.fmean(known), max(known)) if known else (None, None)
            values.extend([round(mean, 4) if mean is not None else None, round(maximum, 4) if maximum is not None else None])
        buckets.append(values)
    reasons = {key: (EXPERIENCE_REASON if scope == 'experience' and summary['scope'] == 'cost' else
                    '当前记录未提供占用读数的原生样本时间，查询时刻不能代替样本时间' if key == 'gpuUtilSampleAgeMs' else
                    '当前记录未提供占用读数的原生采样窗口，查询间隔不能代替采样窗口' if key == 'gpuUtilSamplingWindowMs' else
                    '受测视口没有完整且来源已确认的 GPU 回读' if key == 'measuredViewportGpuMs' else '缺测或有效样本不足')
               for key, _, _, scope in SERIES if not any(groups[bucket][key] for bucket in groups)}
    return dict(limit=limit, bucketSeconds=seconds,
                columns=['fromMs', 'toMs', 'frames'] + [key + suffix for key, *_ in SERIES for suffix in ('Mean', 'Max')],
                buckets=buckets, seriesReasons=reasons, reason=EXPERIENCE_REASON if summary['scope'] == 'cost' else None)


def curve_document(summary, manifest, streams, hotframes):
    columns = ['collectionFrame', 'monotonicMs', 'dtMs', *PHASE_LABELS,
               'mainViewportGpuMs', 'mainViewportCpuMs', 'frameSetupCpuMs']
    rows = []
    for frame in hotframes[:HOTFRAME_LIMIT]:
        row = [frame.get(key) for key in columns]
        if summary['scope'] == 'cost' or not frame['_dtValid']:
            row[2] = None
        if not frame['_phaseValid']:
            row[3:8] = [None] * 5
        rows.append(row)
    return dict(calc=CALC, id=summary['id'],
                levels=[bucket_curves(summary, manifest, streams, limit) for limit in CURVE_LIMITS]
                       if streams and streams['frames'] else [],
                hotframeColumns=columns, hotframes=rows, hotframesTotal=len(hotframes))

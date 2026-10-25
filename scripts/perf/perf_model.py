"""读取性能六件、保留来源与缺测，并计算 ENG-PERF-DATA 的基础指标。
用法：由 analyze.py 与 perf.py 导入；输入会话六件、字段字典与预算，产出内存中的派生指标。
"""
import csv
import json
import math
import statistics
from collections import defaultdict
from pathlib import Path

CALC = json.loads(Path(__file__).with_name('derived-fields.json').read_text(encoding='utf-8'))['calc']
PHASES = ('physMs', 'procMs', 'lateMs', 'drawMs', 'tailMs')
COSTS = ('mainViewportGpuMs', 'mainViewportCpuMs', 'frameSetupCpuMs')
EXPERIENCE_FIELDS = ('averageFps', 'frameMs', 'low1Fps', 'low01Fps', 'low1TailN',
                     'low01TailN', 'n', 'droppedFrameFraction', 'droppedTimeFraction',
                     'heavyFramesPerMinute', 'severeFrameCount', 'framePacingP95Ms',
                     'measuredGpuBusyFraction', 'gpuUtilFraction', 'normalizedGpuLoad', 'windows')


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'), parse_constant=reject_constant)


def reject_constant(value):
    raise ValueError('JSON 不允许非有限数: ' + value)


def at(value, dotted, default=None):
    for key in dotted.split('.'):
        if not isinstance(value, dict) or key not in value:
            return default
        value = value[key]
    return value


def number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def quantile(values, q):
    values = sorted(v for v in values if number(v))
    return values[max(0, math.ceil(q * len(values)) - 1)] if values else None


def distribution(values):
    values = [v for v in values if number(v)]
    return dict(n=len(values), mean=statistics.fmean(values) if values else None,
                p50=quantile(values, .5), p95=quantile(values, .95),
                p99=quantile(values, .99), p999=quantile(values, .999),
                maximum=max(values) if values else None)


def parse_cell(raw, column):
    if raw is None:
        raise ValueError('CSV 行缺列')
    if raw == 'null':
        if not column['nullable']:
            raise ValueError(f"非空字段缺值: {column['name']}")
        return None
    kind = column['type']
    if kind == 'integer':
        return int(raw)
    if kind == 'number':
        value = float(raw)
        if not math.isfinite(value):
            raise ValueError(f"非有限数: {column['name']}")
        if column.get('unit') == 'ratio' and not 0 <= value <= 1:
            raise ValueError(f"比例越界: {column['name']}")
        return value
    if kind == 'boolean':
        if raw not in ('true', 'false'):
            raise ValueError(f"布尔格式错误: {column['name']}")
        return raw == 'true'
    if kind in ('array', 'object'):
        value = json.loads(raw, parse_constant=reject_constant)
        if not isinstance(value, list if kind == 'array' else dict):
            raise ValueError(f"结构字段类型错误: {column['name']}")
        return value
    if kind == 'string':
        return raw
    raise ValueError(f'未支持类型: {kind}')


def read_stream(directory, spec, schema, event_stream=False):
    path = directory / spec['file']
    if spec['kind'] == 'jsonl':
        rows = [json.loads(line, parse_constant=reject_constant) for line in path.read_text(encoding='utf-8-sig').splitlines() if line.strip()]
    else:
        with path.open(encoding='utf-8-sig', newline='') as stream:
            reader = csv.DictReader(stream)
            if reader.fieldnames != [c['name'] for c in spec['columns']]:
                raise ValueError(f'列名或顺序不符: {path.name}')
            rows = [{c['name']: parse_cell(row.get(c['name']), c) for c in spec['columns']}
                    for row in reader]
    previous, previous_time = None, None
    for row in rows:
        if row.get('schema') != schema:
            raise ValueError(f'行 schema 不符: {path.name}')
        sequence = row.get('sequence')
        if not isinstance(sequence, int) or (previous is not None and sequence <= previous):
            raise ValueError(f'流序号不递增: {path.name}')
        previous = sequence
        if spec['file'] == 'hw.csv' and row.get('rowKind') not in ('nvml', 'samplingFacts'):
            raise ValueError('硬件行种类不符')
        stamp = row.get('monotonicMs')
        if not number(stamp) or (not event_stream and previous_time is not None and stamp < previous_time):
            raise ValueError(f'流时刻不递增: {path.name}')
        previous_time = stamp
    return sorted(rows, key=lambda row: (row['monotonicMs'], row['sequence'])) if event_stream else rows


def manifest_path(directory):
    final = Path(directory) / 'manifest.json'
    return final if final.is_file() else Path(directory) / 'manifest.pending.json'


def load_session(directory, dictionary):
    directory = Path(directory).resolve()
    path = manifest_path(directory)
    manifest = read_json(path)
    snapshot = manifest.get('fieldDictionary')
    if not isinstance(snapshot, dict) or snapshot.get('schema') != manifest.get('schema'):
        raise ValueError('字段字典快照 schema 与记录不符')
    recorded = snapshot.get('streams')
    if not isinstance(recorded, dict):
        raise ValueError('字段字典快照流清单缺失')
    for name, spec in dictionary['streams'].items():
        original = recorded.get(name)
        if not isinstance(original, dict) or any(original.get(key) != spec[key] for key in ('file', 'kind')):
            raise ValueError('字段字典快照流结构不符: ' + name)
        columns = original.get('columns')
        if not isinstance(columns, list) or any(not isinstance(c, dict) for c in columns):
            raise ValueError('字段字典快照列结构不符: ' + name)
        by_name = {c.get('name'): c for c in columns}
        if len(by_name) != len(columns):
            raise ValueError('字段字典快照列名重复: ' + name)
        for column in spec['columns']:
            source = by_name.get(column['name'])
            if source is None or any(source.get(key) != column.get(key) for key in ('type', 'nullable', 'unit')):
                raise ValueError('当前指标所需字段语义不符: ' + name + '/' + column['name'])
    # Only analyze_all's exclusive lease certifies an abandoned pending session.
    # Its streams can be truncated; index the manifest without parsing unfinished rows.
    if path.name == 'manifest.pending.json' or manifest.get('status') != 'closed':
        return manifest, None
    streams = {name: read_stream(directory, recorded[name], snapshot['schema'], event_stream=name == 'events')
               for name in dictionary['streams']}
    streams = {name: [{column['name']: row.get(column['name']) for column in dictionary['streams'][name]['columns']}
                      for row in rows] for name, rows in streams.items()}
    return manifest, streams


def scope_of(manifest):
    desktop = manifest.get('desktop') or {}
    if desktop.get('kind') == 'interactive' and desktop.get('isInputDesktop') is True:
        return 'full', None
    reason = '隔离桌面不测' if desktop.get('kind') == 'isolated' else '桌面事实未确认，只测成本'
    return 'cost', reason


def target_ms(manifest):
    pacing = manifest.get('pacing') or {}
    limit = pacing.get('maxFps')
    limits = [limit] if number(limit) and limit > 0 else []
    vsync = pacing.get('vsync')
    if vsync not in (None, False, 0, 'disabled', 'off'):
        refresh = at(manifest, 'display.refreshHz')
        if not number(refresh) or refresh <= 0:
            return None
        limits.append(refresh)
    return 1000 / min(limits) if limits else None


def mark_context(frames, events, dictionary):
    flags = {item['name']: item['value'] for item in dictionary['contextFlags']}
    mask = sum(flags[name] for name in dictionary['nonSteadyRules']['contextFlagNames'])
    intervals = []
    rules = {rule['name']: rule for rule in dictionary['nonSteadyRules']['rules']}
    for rule in rules.values():
        # Flags already contain active operations and settling from the owner.
        if rule.get('activeFlag'):
            continue
        for event in events:
            if event['event'] not in rule['events']:
                continue
            payload = event['payload']
            if rule.get('purpose') and payload.get('purpose') != rule['purpose']:
                continue
            if rule.get('eventMode') == 'valueChange' and payload['observation'] == 'changed':
                settling = rules[rule['settleRule']]['settleMs']
                intervals.append((None, None, event.get('collectionFrame')))
                intervals.append((event['monotonicMs'], event['monotonicMs'] + settling, None))
    for frame in frames:
        complete = frame.get('contextComplete') is True and frame.get('contextFlags') is not None
        active = complete and bool(frame['contextFlags'] & mask)
        active |= frame.get('appState', '').lower() in dictionary['nonSteadyRules']['appStates']
        active |= any((capture == frame['collectionFrame'] if capture is not None
                       else start is not None and start <= frame['monotonicMs'] < end) for start, end, capture in intervals)
        frame['_steady'] = bool(complete and not active)
        frame['_contextKnown'] = complete


def prepare_frames(frames, events, dictionary):
    mark_context(frames, events, dictionary)
    previous = None
    seen_draws = defaultdict(set)
    for frame in frames:
        gap = frame.get('gapBefore') is True or (previous is not None and frame['sequence'] != previous['sequence'] + 1)
        frame['_gap'] = gap
        frame['_dtValid'] = not gap and number(frame.get('dtMs')) and frame['dtMs'] > 0
        frame['_phaseValid'] = frame.get('phaseValidity') == 31 and all(number(frame.get(k)) and frame[k] >= 0 for k in PHASES)
        if frame['_phaseValid'] and number(frame.get('dtMs')):
            frame['_phaseValid'] = math.isclose(sum(frame[k] for k in PHASES), frame['dtMs'], rel_tol=1e-5, abs_tol=.01)
        viewports = frame.get('viewportSamplesJson')
        total, confirmed = 0, isinstance(viewports, list)
        frame['_viewports'] = {}
        frame['mainViewportGpuMs'] = frame['mainViewportCpuMs'] = None
        for view in viewports or []:
            key = f"{view['role']}:{view['viewportId']}"
            source = view.get('sourceDrawFrame')
            skipped = view.get('drewThisFrame') is False
            valid = not skipped and view.get('resultUpdated') is True and source is not None
            valid &= source not in seen_draws[key]
            # DECISION-342 accepts main-viewport costs without claiming an exact source.
            approximate = view['role'] == 'main' and not skipped and not valid
            gpu = view.get('gpuRenderMs') if valid or approximate else None
            cpu = view.get('cpuRenderMs') if valid or approximate else None
            gpu = gpu if number(gpu) and gpu >= 0 else None
            cpu = cpu if number(cpu) and cpu >= 0 else None
            frame['_viewports'][key] = {'gpuMs': gpu, 'cpuMs': cpu, 'sourceDrawFrame': source,
                                       'skipped': skipped, 'sourceConfirmed': bool(valid)}
            if view['role'] == 'main':
                frame['mainViewportGpuMs'], frame['mainViewportCpuMs'] = gpu, cpu
            if not skipped:
                confirmed &= valid and number(gpu)
                if valid:
                    seen_draws[key].add(source)
                if number(gpu):
                    total += gpu
        frame['_measuredGpuMs'] = total if confirmed else None
        previous = frame


def frame_rate(values):
    duration = sum(values)
    return 1000 * len(values) / duration if duration else None


def normalized_gpu_load(row):
    if not is_render_adapter(row):
        return None
    keys = ('gpuUtilFraction', 'gpuClockMHz', 'gpuMaxClockMHz')
    return row[keys[0]] * row[keys[1]] / row[keys[2]] if all(number(row.get(key)) for key in keys) and row[keys[2]] > 0 else None


def is_render_adapter(row):
    """只认采集方按原生适配器身份确认的行，不从卡序号或显存读数猜。"""
    return row.get('rowKind') == 'nvml' and row.get('isRenderAdapter') is True


def experience(frames, hw, target, policy, scope, reason):
    if scope != 'full':
        return {**dict.fromkeys(EXPERIENCE_FIELDS), 'reason': reason}
    valid = [f for f in frames if f['_dtValid']]
    values = [f['dtMs'] for f in valid]
    n, duration = len(values), sum(values)
    tails = {ratio: math.ceil(n * ratio) for ratio in (.01, .001)}
    low = lambda ratio, minimum: 1000 * tails[ratio] / sum(sorted(values, reverse=True)[:tails[ratio]]) if n >= minimum else None
    dropped = [v for v in values if target is not None and v > policy['droppedFrameTargetMultiple'] * target]
    pacing = [abs(b['dtMs'] - a['dtMs']) for a, b in zip(frames, frames[1:])
              if a['_dtValid'] and b['_dtValid'] and b['sequence'] == a['sequence'] + 1
              and not b['_gap'] and a['_steady'] and b['_steady']]
    gpu_rows = [f for f in valid if number(f['_measuredGpuMs'])]
    gpu_dt = sum(f['dtMs'] for f in gpu_rows)
    utilization = [h['gpuUtilFraction'] for h in hw if is_render_adapter(h) and number(h.get('gpuUtilFraction'))]
    normalized = [value for h in hw if (value := normalized_gpu_load(h)) is not None]
    return dict(n=n, averageFps=frame_rate(values), frameMs=distribution(values),
                low1Fps=low(.01, 100), low01Fps=low(.001, 1000), low1TailN=tails[.01], low01TailN=tails[.001],
                droppedFrameFraction=len(dropped) / n if target and n else None,
                droppedTimeFraction=sum(dropped) / duration if target and duration else None,
                heavyFramesPerMinute=sum(v > policy['heavyStutterMs'] for v in values) * 60000 / duration if duration else None,
                severeFrameCount=sum(v > policy['severeStutterMs'] for v in values),
                framePacingP95Ms=quantile(pacing, .95),
                measuredGpuBusyFraction=sum(f['_measuredGpuMs'] for f in gpu_rows) / gpu_dt if gpu_dt else None,
                gpuUtilFraction=statistics.fmean(utilization) if utilization else None,
                normalizedGpuLoad=statistics.fmean(normalized) if normalized else None, reason=None)


def costs(frames, samples, hw):
    fields = COSTS + PHASES + ('streamMountMs', 'streamMountedTiles', 'streamMountedTilesDelta',
                              'streamWatchdogTrips', 'pendingStreamRequests', 'pendingStreamBakes',
                              'pendingStreamPreparations', 'pendingStreamMounts', 'pendingStreamRetirements',
                              'visibleDrawCalls', 'visiblePrimitives', 'shadowDrawCalls', 'shadowPrimitives',
                              'gcPauseMs', 'allocatedBytes', 'managedHeapMiB')
    result = {key: distribution(f.get(key) for f in frames if key not in PHASES or f['_phaseValid']) for key in fields}
    result['measuredViewportGpuMs'] = distribution(f['_measuredGpuMs'] for f in frames)
    result['viewports'] = {key: {metric: distribution(f['_viewports'].get(key, {}).get(metric) for f in frames)
                                  for metric in ('cpuMs', 'gpuMs')}
                           for key in sorted({key for f in frames for key in f['_viewports']})}
    result['memory'] = {key: distribution(s.get(key) for s in samples) for key in
                        ('workingSetMiB', 'managedHeapMiB', 'godotStaticMiB', 'engineVideoMemoryMiB', 'textureMemoryMiB', 'bufferMemoryMiB')}
    result['hardware'] = {key: distribution(h.get(key) for h in hw if is_render_adapter(h)) for key in
                          ('wholeGpuMemoryMiB', 'processVideoMemoryMiB', 'gpuClockMHz', 'powerW', 'temperatureC')}
    result['gcTotals'] = {key: sum(f[key] for f in frames if number(f.get(key))) if any(number(f.get(key)) for f in frames) else None
                          for key in ('gcPauseMs', 'gcGen0', 'gcGen1', 'gcGen2', 'allocatedBytes')}
    result['compilationTotals'] = {key: sum(max(0, b[key] - a[key]) for a, b in zip(frames, frames[1:])
                                          if number(a.get(key)) and number(b.get(key)) and not b['_gap'])
                                   if any(number(a.get(key)) and number(b.get(key)) and not b['_gap'] for a, b in zip(frames, frames[1:])) else None
                                   for key in ('pipelineCanvas', 'pipelineMesh', 'pipelineSurface', 'pipelineDraw', 'pipelineSpecialization', 'jitCompiledMethods', 'jitCompilationMs')}
    return result

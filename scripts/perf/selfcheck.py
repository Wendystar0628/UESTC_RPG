"""四例小样本自检，只验证性能分析行为，不代表真实录制或性能达标。
用法：python -I -X utf8 scripts/perf/perf.py selfcheck；输入当前字典和预算，产出四行结果，临时六件与派生自动清除。
"""
import csv
import json
import tempfile
from pathlib import Path
from analyze import analyze_all, summary_for
from perf_model import COSTS, PHASES, at, prepare_frames, read_json, rolling_medians


def manifest(dictionary, identifier='SELFCHECK'):
    return dict(schema=dictionary['schema'], id=identifier, origin='play', status='closed', complete=True,
                startedAt='2000-01-01T00:00:00Z', endedAt='2000-01-01T00:00:05Z', recordedSeconds=5,
                build=dict(commit=None, dirty=False, configuration='Debug', godot='selfcheck'),
                machine=dict(id='selfcheck-only', gpu='RTX 4080', api='selfcheck', driver='selfcheck', os='selfcheck'),
                display=dict(windowWidthPx=3840, windowHeightPx=2160, refreshHz=60),
                desktop=dict(kind='interactive', isInputDesktop=True, windowFocused=True),
                pacing=dict(maxFps=60, vsync=False),
                graphics=dict(preset='fine', effective={'selfcheck': True}, internalWidthPx=3840, internalHeightPx=2160),
                run=dict(args=[], diagnostics=None), kernelBudgets=dict(streamMountMilliseconds=1),
                clock=dict(anchors=[]), fieldDictionary=dictionary,
                streams=[dict(file=spec['file'], state='recorded', droppedRows=0) for spec in dictionary['streams'].values()])


def frame(dictionary, index, gpu=2, dt=10):
    row = {c['name']: None if c['nullable'] else {'integer': 0, 'number': 0, 'string': '', 'boolean': False}.get(c['type'])
           for c in dictionary['streams']['frames']['columns']}
    row.update(schema=dictionary['schema'], sequence=index + 1, collectionFrame=index + 1000,
               monotonicMs=(index + 1) * dt, dtMs=dt, phaseValidity=31,
               physMs=0, procMs=dt / 4, lateMs=dt / 4, drawMs=dt / 4, tailMs=dt / 4,
               mainViewportGpuMs=gpu, mainViewportCpuMs=1, frameSetupCpuMs=1,
               appState='playing', appActivity=0, contextFlags=0, contextComplete=True,
               gapBefore=False, viewportSamplesJson=[viewport(1, 'main', None, gpu)],
               gcPauseMs=0, gcGen0=0, gcGen1=0, gcGen2=0, allocatedBytes=0)
    row.update({key: 0 for key in ('pipelineCanvas', 'pipelineMesh', 'pipelineSurface', 'pipelineDraw', 'pipelineSpecialization', 'jitCompiledMethods')})
    return row


def viewport(identifier, role, source, gpu, skipped=False):
    return dict(viewportId=identifier, role=role, sourceDrawFrame=source, gpuRenderMs=gpu,
                cpuRenderMs=1, drewThisFrame=False if skipped else None,
                resultUpdated=True if source is not None and not skipped else None,
                sourceCpuBeginUs=None, sourceCpuEndUs=None)


def streams(frames):
    # Real sessions contain clock anchors; include one so association handles a nonempty event list.
    when = frames[0]['monotonicMs'] if frames else 0
    anchor = dict(schema=1, sequence=1, event='clock.anchor', collectionFrame=None,
                  monotonicMs=when, payload=dict(unixMs=0, monotonicMs=when, readSpanMs=0))
    return dict(frames=frames, samples=[], hw=[], hwfast=[], events=[anchor])


def write_six(directory, meta, data, dictionary):
    directory.mkdir(parents=True)
    (directory / 'manifest.json').write_text(json.dumps(meta), encoding='utf-8')
    (directory / '.lock').touch()
    for name, spec in dictionary['streams'].items():
        path = directory / spec['file']
        if spec['kind'] == 'jsonl':
            path.write_text(''.join(json.dumps(row) + '\n' for row in data[name]), encoding='utf-8')
        else:
            names = [c['name'] for c in spec['columns']]
            with path.open('w', encoding='utf-8', newline='') as target:
                writer = csv.DictWriter(target, fieldnames=names)
                writer.writeheader()
                for row in data[name]:
                    writer.writerow({key: value if isinstance(value, str) else json.dumps(value, separators=(',', ':')) for key, value in row.items()})


def run(root):
    dictionary, budgets = read_json(root / 'game/data/perf-fields.json'), read_json(root / 'budgets.json')
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    window = policy['medianWindowFrames']
    result = []
    frames = [frame(dictionary, i, gpu=2 if i < warm else 8) for i in range(warm + window + warm)]
    prepare_frames(frames, [], [], dictionary)
    medians = rolling_medians(frames, COSTS + PHASES + ('dtMs',), policy)
    assert medians[-1]['mainViewportGpuMs'] == 8, '稳定抬升未进入 M'
    summary, items, _, _ = summary_for(Path('selfcheck'), manifest(dictionary), streams(frames), dictionary, budgets, {})
    late = [a for a in items if a['type'] == 'costSpike' and a['lastFrame'] == frames[-1]['collectionFrame']]
    assert not late, '稳定抬升被持续当尖峰剔除'
    spikes = [a for a in items if a['type'] == 'costSpike' and a['metric'] == 'mainViewportGpuMs']
    assert spikes and all(a['phase'] is None and not a['candidates'] for a in spikes), '主视口近似成本被逐帧归因'
    assert all(a['lastFrame'] > a['firstFrame'] and '±2' in a['note'] for a in spikes), '近似候选未保留关联窗口'
    assert at(summary, 'steady.cost.mainViewportGpuMs.n') == len(frames), '近似主视口成本未进入分布'
    assert all(f['_measuredGpuMs'] is None for f in frames), '近似主视口成本进入了精确合计'
    result.append('通过 1/4：近似主视口成本进入分布与 M；尖峰仅报±2帧窗口，无逐帧阶段/原因归因。')

    count = min(60, warm - 1)
    frames = [frame(dictionary, i, gpu=6) for i in range(count)]
    frames[20]['gapBefore'] = True
    frames[20]['dtMs'] = 500
    summary, items, _, _ = summary_for(Path('selfcheck'), manifest(dictionary), streams(frames), dictionary, budgets, {})
    assert not any(a['type'] == 'costSpike' for a in items), '冷启动不足暖窗仍出了相对判据'
    assert any(a['type'] == 'budgetExceeded' for a in items), '不足暖窗漏了绝对预算判据'
    assert at(summary, 'all.experience.averageFps') == 100, '跨 gap 间隔进入体验计算'
    assert at(summary, 'all.experience.framePacingP95Ms') == 0, '帧节奏跨了 gap'
    assert at(summary, 'all.experience.low1Fps') is None, '不足样本仍计算 low'
    result.append('通过 2/4：冷启动无相对判据、绝对预算照报；缺口不跨算，low 缺样为 null。')

    frames = [frame(dictionary, i) for i in range(8)]
    for i, row in enumerate(frames):
        # Explicit confirmed-source fixture, distinct from main's approximate native readback.
        row['viewportSamplesJson'][0].update(sourceDrawFrame=i, resultUpdated=True)
        row['viewportSamplesJson'].append(viewport(2, 'reflection', i, 1 if i % 2 == 0 else 999, skipped=bool(i % 2)))
        row['measuredViewportGpuMs'] = 999  # Prove that stale aggregate input is never reused.
    prepare_frames(frames, [], [], dictionary)
    assert [f['_measuredGpuMs'] for f in frames] == [3, 2] * 4, '隔帧倒影重用了旧值或重复加了主视口'
    frames[0]['viewportSamplesJson'][0]['sourceDrawFrame'] = None
    prepare_frames(frames, [], [], dictionary)
    assert frames[0]['_measuredGpuMs'] is None, '来源未知仍算了合计'
    assert frames[0]['mainViewportGpuMs'] == 2, '近似主视口成本被未知精确来源抹掉'
    frames[1]['viewportSamplesJson'][0]['drewThisFrame'] = False
    frames[2]['viewportSamplesJson'][1].update(sourceDrawFrame=None, resultUpdated=None)
    prepare_frames(frames, [], [], dictionary)
    assert frames[1]['mainViewportGpuMs'] is None and frames[1]['mainViewportCpuMs'] is None, '未绘制仍复用主视口读数'
    assert frames[2]['_viewports']['reflection:2']['gpuMs'] is None and frames[2]['_measuredGpuMs'] is None, '未确认倒影仍参与成本或合计'
    result.append('通过 3/4：近似主视口不进合计；跳过行不复用主成本，未确认倒影留 null，已确认来源不重加。')

    with tempfile.TemporaryDirectory(prefix='uestc-perf-selfcheck-') as temporary:
        work = Path(temporary)
        source, out, author = work / 'sessions/SELFCHECK', work / 'perf', work / 'author'
        meta = manifest(dictionary)
        meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
        data = streams([frame(dictionary, i, gpu=6) for i in range(count)])
        write_six(source, meta, data, dictionary)
        first = analyze_all(root, [source], out, author)
        assert first['analyzed'] == ['SELFCHECK'] and not first['errors'], first
        second = analyze_all(root, [source], out, author)
        assert not second['analyzed'] and not second['errors'], second
        summary = read_json(out / 'derived/SELFCHECK/summary.json')
        assert all(value is None for key, value in summary['all']['experience'].items() if key != 'reason'), 'cost 会话泄漏了体验字段'
        assert summary['scopeReason'] == '隔离桌面不测'
        items = read_json(out / 'derived/SELFCHECK/anomalies.json')['items']
        assert any(a['type'] == 'budgetExceeded' for a in items) and not any(a['type'] == 'costSpike' for a in items)
        assert len((out / 'history.jsonl').read_text(encoding='utf-8').splitlines()) == 1, '重复触发重复追加了历史'
        from perf_store import AI_BATCHES_KEEP, exclusive, prune
        with exclusive(source / '.lock') as held:
            assert held
            locked = analyze_all(root, [source], out, author)
            assert str(source.resolve()) in locked['skipped'], '写入锁占用时仍读取会话'
        for index in range(AI_BATCHES_KEEP + 2):
            batch = out / 'runs' / f'batch-{index:02d}' / 'run/session'
            batch.mkdir(parents=True)
            (batch / 'manifest.json').write_text('{"status":"closed"}', encoding='utf-8')
            (batch / '.lock').touch()
        removed = prune(out, [])
        assert len(list((out / 'runs').iterdir())) == AI_BATCHES_KEEP and len(removed) == 2, 'AI 批次未按条数修剪'
        from perf import MAX_CHARS, MAX_LINES, bounded
        text = bounded(('中文行' * 100 for _ in range(1000)))
        assert len(text) <= MAX_CHARS and len(text.splitlines()) <= MAX_LINES
    result.append('通过 4/4：预算独立报、cost 体验全 null；六件往返、写入锁跳过、历史去重、按条数修剪和输出上限通过。')
    return result

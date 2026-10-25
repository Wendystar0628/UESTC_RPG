"""验证快照、逐帧成本、事件时序与异常判据。
用法：由 perf.py selfcheck 编排调用，复用 selfcheck_fixtures，不写真实会话。
"""
import csv
import copy
import json
import tempfile
from pathlib import Path
from analyze import summary_for
from perf_model import COSTS, PHASES, at, load_session, prepare_frames, rolling_medians, target_ms
from perf_anomalies import DEEPEN_REPEAT_COUNT, annotate, anomaly, deepen, frame_anomalies
from selfcheck_fixtures import manifest, frame, viewport, streams, write_six


def check_snapshot(root, dictionary, budgets):
    with tempfile.TemporaryDirectory(prefix='uestc-perf-snapshot-') as temporary:
        source = Path(temporary) / 'session'
        meta = manifest(dictionary)
        meta['fieldDictionary'] = copy.deepcopy(dictionary)
        meta['fieldDictionary']['nonSteadyRules']['appStates'] = ['playing']
        assert meta['fieldDictionary'] != dictionary, '夹具没有改变同 schema 的判定规则'
        write_six(source, meta, streams([frame(dictionary, 0)], dictionary), dictionary)
        loaded, data = load_session(source, dictionary)
        summary, _, _, _ = summary_for(source, loaded, data, dictionary, budgets, {})
        assert at(summary, 'steady.cost.mainViewportGpuMs.n') == 1, '未按当前规则重算旧快照'
        for key, value in (('file', '../frames.csv'), ('columns', [])):
            broken = copy.deepcopy(meta)
            broken['fieldDictionary']['streams']['frames'][key] = value
            (source / 'manifest.json').write_text(json.dumps(broken), encoding='utf-8')
            try:
                load_session(source, dictionary)
            except ValueError:
                pass
            else:
                raise AssertionError('快照的流路径或列结构未校验')
        # The recorded schema and column ordering belong to its own snapshot, not today's writer.
        older = copy.deepcopy(dictionary)
        older['schema'] -= 1
        older['streams']['hw']['columns'].reverse()
        older['streams']['hw']['columns'].append(dict(name='retiredCounter', type='integer', nullable=True, unit=None))
        source_old = Path(temporary) / 'older'
        original = manifest(older, 'OLDER-SNAPSHOT')
        write_six(source_old, original, streams([frame(older, 0)], older), older)
        old_meta, old_data = load_session(source_old, dictionary)
        recalculated = summary_for(source_old, old_meta, old_data, dictionary, budgets, {})[0]
        assert at(recalculated, 'steady.cost.mainViewportGpuMs.n') == 1
        assert old_meta['schema'] == older['schema'] and recalculated['calc'] == summary['calc']
        broken = copy.deepcopy(original)
        broken['fieldDictionary']['streams']['frames']['columns'][0]['type'] = 'string'
        (source_old / 'manifest.json').write_text(json.dumps(broken), encoding='utf-8')
        try:
            load_session(source_old, dictionary)
        except ValueError:
            pass
        else:
            raise AssertionError('当前必需字段语义变化仍被重算')


def check_median(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    frames = [frame(dictionary, i) for i in range(warm + 3)]
    frames[warm]['appState'] = 'entering'
    frames[warm + 1]['gapBefore'] = True
    for row in frames[warm:warm + 2]:
        row['viewportSamplesJson'][0]['gpuRenderMs'] = 999
    prepare_frames(frames, [], [], dictionary)
    assert not frames[warm]['_steady'] and frames[warm + 1]['_gap'], '夹具未触发非稳态与缺口'
    medians = rolling_medians(frames, COSTS, policy)
    assert medians[-1]['mainViewportGpuMs'] == 2, '非稳态或 gap 清掉了 M 或进入了 M'
    frames[-1]['_scene'] = ('another scene',)
    assert rolling_medians(frames, COSTS, policy)[-1]['mainViewportGpuMs'] is None, '换场景没有清 M'
    frames[-1]['_scene'] = frames[-2]['_scene']
    event = dict(event='settings.quality', monotonicMs=frames[-1]['monotonicMs'], payload={'effective': {'scale': .5}})
    prepare_frames(frames, [], [event], dictionary)
    assert rolling_medians(frames, COSTS, policy)[-1]['mainViewportGpuMs'] is None, '换口径没有清 M'


def check_deepen(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    frames = [frame(dictionary, i) for i in range(warm + 1)]
    for key in ('gpuRenderMs', 'cpuRenderMs'):
        frames[-1]['viewportSamplesJson'][0][key] = 100
    prepare_frames(frames, [], [], dictionary)
    medians = rolling_medians(frames, COSTS, policy)
    annotate(frames, [], medians, policy)
    items = frame_anomalies(frames, manifest(dictionary), budgets, medians, 'cost', {})
    spikes = [a for a in items if a['type'] == 'costSpike' and a['metric'] in COSTS[:2]]
    assert len(spikes) == 2 and all(not a['candidates'] for a in spikes), '夹具未触发主视口两项近似尖峰'
    assert not deepen(spikes * DEEPEN_REPEAT_COUNT, manifest(dictionary)), '近似主视口候选被误计为缺原因'
    exact = anomaly('costSpike', 'info', frames[-1], 100, 1, 'selfcheck', 'frameSetupCpuMs')
    assert deepen([exact] * DEEPEN_REPEAT_COUNT, manifest(dictionary)), '普通无原因重复候选没有 deepen'
    exact.update(metric=COSTS[0], firstFrame=-1)
    mixed = deepen(spikes + [exact] * DEEPEN_REPEAT_COUNT, manifest(dictionary))
    assert len(mixed) == 1 and mixed[0]['firstFrame'] == -1, '近似候选借用了精确候选的无原因计数'


def check_manifest(root, dictionary, budgets):
    meta = manifest(dictionary)
    meta['display']['refreshHz'] = None
    assert target_ms(meta) == 1000 / meta['pacing']['maxFps'], '真实 disabled 被当成开启垂直同步'


def check_event_order(root, dictionary, budgets):
    with tempfile.TemporaryDirectory(prefix='uestc-perf-events-') as temporary:
        source = Path(temporary) / 'session'
        data = streams([frame(dictionary, i) for i in range(2)], dictionary)
        anchor = data['events'][0]
        data['events'] = [{**anchor, 'sequence': sequence, 'monotonicMs': stamp, 'event': event}
                          for sequence, stamp, event in ((1, 100, 'clock.anchor'), (2, 30, 'gc.detail'),
                                                         (3, 30, 'hang.begin'), (4, 20, 'record.gap'))]
        write_six(source, manifest(dictionary), data, dictionary)
        loaded = load_session(source, dictionary)[1]
        assert [row['sequence'] for row in loaded['events']] == [4, 2, 3, 1], '事件未按时刻与序号稳定排序'
        path = source / dictionary['streams']['events']['file']
        data['events'][1]['sequence'] = data['events'][0]['sequence']
        path.write_text(''.join(json.dumps(row) + '\n' for row in data['events']), encoding='utf-8')
        try:
            load_session(source, dictionary)
        except ValueError:
            pass
        else:
            raise AssertionError('乱序事件的重复 sequence 未拒绝')
        path.write_text(json.dumps(anchor) + '\n', encoding='utf-8')
        frames_path = source / dictionary['streams']['frames']['file']
        rows = list(csv.DictReader(frames_path.read_text(encoding='utf-8').splitlines()))
        rows[1]['monotonicMs'] = str(float(rows[0]['monotonicMs']) - 1)
        with frames_path.open('w', encoding='utf-8', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
            writer.writeheader()
            writer.writerows(rows)
        try:
            load_session(source, dictionary)
        except ValueError:
            pass
        else:
            raise AssertionError('逐帧流时刻倒退被接受')


def check_cost_windows(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    window = policy['medianWindowFrames']
    frames = [frame(dictionary, i, gpu=2 if i < warm else 8) for i in range(warm + window + warm)]
    prepare_frames(frames, [], [], dictionary)
    medians = rolling_medians(frames, COSTS + PHASES + ('dtMs',), policy)
    assert medians[-1]['mainViewportGpuMs'] == 8, '稳定抬升未进入 M'
    summary, items, _, _ = summary_for(Path('selfcheck'), manifest(dictionary), streams(frames, dictionary), dictionary, budgets, {})
    late = [a for a in items if a['type'] == 'costSpike' and a['lastFrame'] == frames[-1]['collectionFrame']]
    assert not late, '稳定抬升被持续当尖峰剔除'
    spikes = [a for a in items if a['type'] == 'costSpike' and a['metric'] == 'mainViewportGpuMs']
    assert spikes and all(a['phase'] is None and not a['candidates'] for a in spikes), '主视口近似成本被逐帧归因'
    assert all(a['lastFrame'] > a['firstFrame'] and '±2' in a['note'] for a in spikes), '近似候选未保留关联窗口'
    assert at(summary, 'steady.cost.mainViewportGpuMs.n') == len(frames), '近似主视口成本未进入分布'
    assert all(f['_measuredGpuMs'] is None for f in frames), '近似主视口成本进入了精确合计'


def check_cold_gap(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    count = min(60, warm - 1)
    frames = [frame(dictionary, i, gpu=budgets['deviceTargets']['developer']['mainViewportGpuP95MsUpper'] * 2) for i in range(count)]
    frames[20]['gapBefore'] = True
    frames[20]['dtMs'] = 500
    meta = manifest(dictionary)
    meta['origin'] = 'ai'
    summary, items, _, _ = summary_for(Path('selfcheck'), meta, streams(frames, dictionary), dictionary, budgets, dict(conditions=dict(suite='fixed')))
    assert not any(a['type'] == 'costSpike' for a in items), '冷启动不足暖窗仍出了相对判据'
    assert any(a['type'] == 'budgetExceeded' for a in items), '不足暖窗漏了绝对预算判据'
    assert at(summary, 'all.experience.averageFps') == 100, '跨 gap 间隔进入体验计算'
    assert at(summary, 'all.experience.framePacingP95Ms') == 0, '帧节奏跨了 gap'
    assert at(summary, 'all.experience.low1Fps') is None, '不足样本仍计算 low'


def check_viewport_sources(root, dictionary, budgets):
    frames = [frame(dictionary, i) for i in range(8)]
    for i, row in enumerate(frames):
        # Explicit confirmed-source fixture, distinct from main's approximate native readback.
        row['viewportSamplesJson'][0].update(sourceDrawFrame=i, resultUpdated=True)
        row['viewportSamplesJson'].append(viewport(2, 'reflection', i, 1 if i % 2 == 0 else 999, skipped=bool(i % 2)))
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

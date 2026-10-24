"""验证逐帧上下文权威、实际窗口变化和硬件事实保真。
用法：由 perf.py selfcheck 调用，复用 selfcheck_fixtures，不写真实会话。
"""
import copy
import tempfile
from pathlib import Path
from analyze import analyze_all
from perf_model import prepare_frames, load_session, read_json, at
from selfcheck_fixtures import manifest, frame, streams, write_six


def check_context(root, dictionary, budgets):
    excluded = dictionary['nonSteadyRules']['contextFlagNames']
    assert excluded, '字典未声明非稳态上下文位'
    frames = [frame(dictionary, i) for i in range(len(dictionary['contextFlags']))]
    for row, flag in zip(frames, dictionary['contextFlags']):
        row['contextFlags'] = flag['value']
    prepare_frames(frames, [], [], dictionary)
    assert all(row['_steady'] == (flag['name'] not in excluded)
               for row, flag in zip(frames, dictionary['contextFlags'])), '上下文位判定未遵守字典唯一清单'
    frames = [frame(dictionary, i) for i in range(2)]
    capture = dict(event='sync.readback', collectionFrame=frames[1]['collectionFrame'],
                   monotonicMs=frames[0]['monotonicMs'], payload=dict(purpose='saveThumbnail'))
    prepare_frames(frames, [], [capture], dictionary)
    assert frames[0]['_steady'] and not frames[1]['_steady'], '缩略图未按真实采集帧号排除'

    flags = {item['name']: item['value'] for item in dictionary['contextFlags']}
    commands = [('save.load', 'ISaveService', 'queued'), ('save.load', 'ISaveService', 'started'),
                ('save.load', 'ISaveService', 'completed'), ('game.enter', 'IAppFlow', 'started'),
                ('game.enter', 'IAppFlow', 'completed'), ('map.prepare', 'IMapService', 'started'),
                ('map.prepare', 'IMapService', 'joined'), ('map.prepare', 'IMapService', 'completed'),
                ('settings.apply', 'IQualityService', 'queued'), ('settings.apply', 'IQualityService', 'started'),
                ('settings.apply', 'IQualityService', 'completed'), ('map.arrival', None, 'begin'),
                ('map.arrival', None, 'detached')]
    events = [dict(event=name, monotonicMs=i + 1, payload=dict(owner=owner, operation=7, phase=phase))
              for i, (name, owner, phase) in enumerate(commands)]
    frames = [frame(dictionary, i) for i in range(3)]
    frames[0]['contextFlags'] = flags['newGameOrLoad'] | flags['qualityApplying'] | flags['mapTransition']
    frames[1]['contextFlags'] = flags['qualitySettling'] | flags['transitionSettling']
    prepare_frames(frames, [], events, dictionary)
    assert [row['_steady'] for row in frames] == [False, False, True], '当前命令阶段/operation 或 detached 重建覆盖了逐帧权威'
    for row in frames:
        row['contextFlags'] = 0
    prepare_frames(frames, [], events[:1] + events[-2:], dictionary)
    assert all(row['_steady'] for row in frames), '未结束事件或 arrival detached 留下了悬窗'


def check_window_changes(root, dictionary, budgets):
    """Regression：当前值不造变化窗，真实变化按同一画质稳定时长排除。"""
    rules = {rule['name']: rule for rule in dictionary['nonSteadyRules']['rules']}
    window_rule = rules['windowChange']
    settling = rules[window_rule['settleRule']]['settleMs']
    start = settling + 100
    stamps = [start - 1, start, start + settling - 1, start + settling, start + 2 * settling]
    for name, values in (('window.size', dict(widthPx=1920, heightPx=1080)),
                         ('window.mode', dict(mode='Windowed'))):
        frames = [dict(frame(dictionary, i), monotonicMs=stamp) for i, stamp in enumerate(stamps)]
        current = dict(event=name, monotonicMs=0, payload=dict(values, observation='current'))
        prepare_frames(frames, [], [current], dictionary)
        assert all(row['_steady'] for row in frames), '初始窗口快照虚构了变化'
        changed_values = dict(widthPx=1280, heightPx=720) if name == 'window.size' else dict(mode='Fullscreen')
        assert changed_values != values, '夹具未改变实际窗口值'
        changed = dict(event=name, monotonicMs=start, payload=dict(changed_values, observation='changed'))
        prepare_frames(frames, [], [current, changed], dictionary)
        assert [row['_steady'] for row in frames] == [True, False, False, True, True], '实际窗口变化边界或稳定时长错误'
        changed.update(monotonicMs=start + .5, collectionFrame=frames[1]['collectionFrame'])
        prepare_frames(frames, [], [current, changed], dictionary)
        assert not frames[1]['_steady'], '同采集帧较晚发生的实际窗口变化漏掉了发生帧'
    frames = [frame(dictionary, 0)]
    frames[0]['contextComplete'] = False
    prepare_frames(frames, [], [], dictionary)
    assert not frames[0]['_steady'] and not frames[0]['_contextKnown'], '未知上下文被事件或状态补成稳态'


def check_hardware_facts(root, dictionary, budgets):
    """Regression：事实行独立于 GPU 行，原生时间与原始 JSON 原样往返和投影。"""
    with tempfile.TemporaryDirectory(prefix='uestc-perf-facts-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        source = author / 'FACTS'
        data = streams([frame(dictionary, 0), frame(dictionary, 1)], dictionary)
        base = {column['name']: None for column in dictionary['streams']['hw']['columns']}
        facts = dict(base, schema=dictionary['schema'], sequence=1, monotonicMs=10, rowKind='samplingFacts',
                     samplingFactsJson=dict(sessionId='FACTS', renderAdapterLuid=4660,
                         wholeGpuUtilFraction=dict(value=None, capability='unsupported', queryStartMs=3,
                                                   queryEndMs=7, averageStartMs=None, averageEndMs=None)),
                     cpuCountersJson=dict(current=dict(systemQueryStartMs=4, systemQueryEndMs=6,
                         processQueryStartMs=4.5, processQueryEndMs=6.5, kernel100ns=9007199254740993),
                         previous=dict(systemQueryStartMs=1, systemQueryEndMs=2), processFraction=None),
                     gpuEnginesJson=dict(collectTimestamp100ns=9007199254740993, previousCollectMs=2,
                         rawQueryCode=0, engines=[dict(instance='pid_42_luid_0x0_0x1234_phys_0_eng_1_engtype_3D',
                         utilFraction=1.2, cStatus=0, rawFirstValue=9007199254740993, rawTimestamp100ns=9007199254740995)]),
                     renderAdapterJson=dict(luid=4660, luidText='0x1234', status=dict(capability='supported', nativeCode=0)))
        gpu = dict(base, schema=dictionary['schema'], sequence=2, monotonicMs=11, rowKind='nvml',
                   deviceIndex=0, gpuUtilFraction=.5, gpuClockMHz=1000, gpuMaxClockMHz=2000)
        data['hw'] = [facts, gpu]
        original = copy.deepcopy(data['hw'])
        write_six(source, manifest(dictionary, 'FACTS'), data, dictionary)
        loaded = load_session(source, dictionary)[1]
        assert loaded['hw'] == original, '事实 JSON、空值或各自原生时间在加载时被改写'
        analyzed = analyze_all(root, out, author)
        assert not analyzed['errors'], analyzed
        summary = read_json(out / 'derived/FACTS/summary.json')
        assert at(summary, 'all.experience.gpuUtilFraction') == .5, '非显卡事实行变成零卡样本'
        assert at(summary, 'all.experience.normalizedGpuLoad') == .25, '事实行进入显卡归一化'
        hardware = read_json(out / 'derived/FACTS/hardware.json')
        assert hardware['hw'] == original, '投影丢失事实行、原始 JSON 或原生时间基'
        facts_only = copy.deepcopy(data)
        facts_only['hw'] = [facts]
        second = author / 'FACTS-ONLY'
        write_six(second, manifest(dictionary, 'FACTS-ONLY'), facts_only, dictionary)
        analyzed = analyze_all(root, out, author)
        assert not analyzed['errors'], analyzed
        summary = read_json(out / 'derived/FACTS-ONLY/summary.json')
        assert at(summary, 'all.experience.gpuUtilFraction') is None, '无 NVML 显卡行被补零'
        assert at(summary, 'coverage.hw.rows') == 1, '事实行被当作缺少硬件数据'
        assert not any(item['field'] == 'hw' for item in summary['validity']), '有效事实流被误报为未知 GPU/缺采集'

"""验证曲线派生、观察台与命令行投影及缺数原因。
用法：由 perf.py selfcheck 编排调用，复用 selfcheck_fixtures，不写真实会话。
"""
import io
import json
import sys
import tempfile
from pathlib import Path
from contextlib import redirect_stderr, redirect_stdout
from unittest.mock import patch
from analyze import analyze_all
from perf_model import at, read_json
from selfcheck_fixtures import manifest, frame, streams, write_six


def check_derived_curves(root, dictionary, budgets):
    with tempfile.TemporaryDirectory(prefix='uestc-perf-curves-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        source = author / 'CURVES'
        gpu = at(budgets, 'deviceTargets.developer.mainViewportGpuP95MsUpper') * 2
        rows = [frame(dictionary, i, gpu=gpu) for i in range(2)]
        rows[1]['dtMs'] = 30
        rows[1]['phaseValidity'] = 0
        data = streams(rows, dictionary)
        data['hw'] = [dict(monotonicMs=10, isRenderAdapter=True, gpuUtilFraction=.5, gpuClockMHz=1000, gpuMaxClockMHz=2000)]
        # The round trip uses the real schema; unspecified nullable hardware fields remain missing.
        data['hw'][0] = {**{c['name']: None for c in dictionary['streams']['hw']['columns']},
                         **data['hw'][0], 'schema': dictionary['schema'], 'sequence': 1, 'rowKind': 'nvml'}
        meta = manifest(dictionary, 'CURVES')
        meta['origin'] = 'ai'
        write_six(source, meta, data, dictionary)
        (source / 'run.json').write_text(json.dumps(dict(conditions=dict(suite='fixed'))), encoding='utf-8')
        analyzed = analyze_all(root, out, author)
        assert not analyzed['errors'], analyzed
        curves = read_json(out / 'derived/CURVES/curves.json')
        detail = curves['levels'][0]
        values = dict(zip(detail['columns'], detail['buckets'][0]))
        assert values['fpsMean'] == 50 and values['fpsMax'] == 100, '派生帧率未按有效帧时计算'
        assert values['normalizedGpuLoadMean'] == .25, '派生负载未沿硬件同条读数归一化'
        assert detail['seriesReasons']['gpuUtilSampleAgeMs'], '缺测原生时间原因丢失'
        assert curves['hotframesTotal'] == len(rows) == len(curves['hotframes']), '越预算夹具未选出两帧高耗时证据'
        assert curves['hotframes'][1][3:8] == [None] * 5, '无效五段泄漏到逐帧投影'
        assert all(len(level['buckets']) <= level['limit'] for level in curves['levels']), '派生桶数超过声明上限'


def check_monitor_projection(root, dictionary, budgets):
    sys.path.insert(0, str(root / '.collab/tools'))
    from monitor.read import perf as reader
    from monitor.view import perf as view
    import analyze
    with patch.object(reader.U, 'REPO', str(root)):
        with tempfile.TemporaryDirectory(prefix='uestc-perf-monitor-') as temporary:
            out, author = Path(temporary) / 'output/perf', Path(temporary) / 'author'
            source = author / 'MONITOR'
            write_six(source, manifest(dictionary, 'MONITOR'), streams([frame(dictionary, 0)], dictionary), dictionary)
            analyzed = analyze_all(root, out, author)
            assert not analyzed['errors'], analyzed
            history_path = out / 'history.jsonl'
            reads = []
            original_read = Path.read_text

            def read_text(path, *args, **kwargs):
                if path == history_path:
                    reads.append(path)
                assert path.suffix != '.csv', '观察台读取了逐帧或硬件流'
                return original_read(path, *args, **kwargs)

            with patch.object(reader, 'OUT', out), patch.object(reader.store, 'author_root', return_value=author), \
                 patch.object(reader.repo, 'mainline_commits', return_value={'commits': []}), \
                 patch.object(Path, 'read_text', read_text), \
                 patch.object(analyze, 'load_session', side_effect=AssertionError('观察台重新解析了原始流')):
                payload = view.build(None)
            assert reads == [history_path], '同轮读取了不止一次性能历史'
            assert payload['details']['MONITOR']['columns'], '观察台未投影现成派生曲线'
            assert payload['retention']['historyPerGroup']['value'] == 1, '历史组现值错误'
            good = author / 'GOOD'
            write_six(good, manifest(dictionary, 'GOOD'), streams([frame(dictionary, 0)], dictionary), dictionary)
            (source / 'frames.csv').write_text('truncated', encoding='utf-8')
            with patch.object(reader, 'OUT', out), patch.object(reader.store, 'author_root', return_value=author), \
                 patch.object(reader.repo, 'mainline_commits', return_value={'commits': []}), \
                 patch.object(analyze, 'analyze_all', side_effect=lambda **args: analyze_all(output=out, **args)) as calls:
                for _ in range(2):
                    payload = view.build(None)
                    assert [entry['id'] for entry in payload['sessions']] == ['GOOD'], '坏会话拖垮好会话或旧历史伪装当前数据'
                    assert payload['sessionErrors'][0]['source'] == str(source.resolve()) and payload['sessionErrors'][0]['reason']
                    assert payload['sessionErrors'][0]['kindLabel'] == '内容错误', '坏会话缺少后端中文种类'
                    assert payload['latest']['play'] == 'GOOD' and payload['details']['GOOD']['columns']
                assert calls.call_count == 1, '连续采集为同一坏会话空转分析器'
            (source / 'manifest.json').write_text('{broken manifest', encoding='utf-8')
            with patch.object(reader, 'OUT', out), patch.object(reader.store, 'author_root', return_value=author), \
                 patch.object(reader.repo, 'mainline_commits', return_value={'commits': []}), \
                 patch.object(analyze, 'analyze_all', side_effect=lambda **args: analyze_all(output=out, **args)):
                payload = view.build(None)
            assert payload['latest']['play'] == 'GOOD' and len(payload['sessionErrors']) == 1, '坏作者清单拖垮整页或重复报错'
            assert (out / 'derived/MONITOR/summary.json').is_file(), '夹具没有留下坏来源的旧派生'
            import perf
            for arguments, expected in ((['show', 'MONITOR'], 1), (['compare', 'GOOD', 'MONITOR'], 1),
                                        (['compare', 'MONITOR', 'GOOD'], 1), (['slice', 'MONITOR'], 1),
                                        (['anomalies', 'MONITOR'], 1), (['show', 'GOOD'], 0),
                                        (['slice', 'GOOD'], 0),
                                        (['anomalies', 'GOOD'], 0), (['latest'], 0)):
                stdout, stderr = io.StringIO(), io.StringIO()
                with patch.object(sys, 'argv', ['perf.py', '--root', temporary, *arguments]), \
                     redirect_stdout(stdout), redirect_stderr(stderr):
                    status = perf.main()
                assert status == expected, (arguments, status, stdout.getvalue(), stderr.getvalue())
                if expected and arguments[0] != 'compare':
                    assert str(source.resolve()) in stderr.getvalue(), 'CLI 未指明失败来源'
                elif arguments == ['latest']:
                    error = payload['sessionErrors'][0]
                    assert '会话 GOOD |' in stdout.getvalue() and '会话 MONITOR |' not in stdout.getvalue()
                    assert all(error[key] in stdout.getvalue() for key in ('source', 'kind', 'reason')), 'latest 未列全来源错误'


def check_missing_projection(root, dictionary, budgets):
    """Regression：不完整窗口仍展示已判规则数，缺钩子日志保留缺数原因。"""
    sys.path.insert(0, str(root / '.collab/tools'))
    from monitor.view import collab, health
    now = 2 * 86400
    at = collab.U.iso(now - 30)
    event = dict(at=at, sourceAt=at, event='started', payload=dict(id='selfcheck', side='codex',
            name='invalid-name', model='gpt-6.1-sol', effort='high'))
    reasons = collab._window_reasons(collab.U.iso(now - 3600), 7, now, ['n', 'ok', 'rate'])
    result = collab._dispatch([event], now - 3600, now, False, reasons)
    assert len(result['bad']) == 1 and result['bad'][0]['rules'] == ['R2'], '夹具未触发明确命名违规'
    for rule in result['rules']:
        assert rule['bad'] == sum(rule['key'] in item['rules'] for item in result['bad']), '规则计数与违规明细不符'
    assert result['rules'][0]['unknown'] == 1 and result['n'] is None, '未判个数或窗口未知被抹掉'
    with patch.object(health.hooklog, 'selftest_result', return_value=None), \
         patch.object(health.hooklog, 'log_records', return_value=None):
        hooks = health._hooks({})
    assert hooks['errors'] is None and any('errors' in r['fields'] for r in hooks['reasons']), '缺日志没有错误统计原因'
    with patch.object(health.hooklog, 'selftest_result', return_value=None), \
         patch.object(health.hooklog, 'log_records', return_value=[]):
        hooks = health._hooks({})
    assert hooks['errors'] == [] and not hooks['reasons'], '存在空日志被当成缺日志'

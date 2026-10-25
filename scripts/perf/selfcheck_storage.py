"""验证会话锁、错误缓存、保留权威与六件往返。
用法：由 perf.py selfcheck 编排调用，复用 selfcheck_fixtures，不写真实会话。
"""
import json
import tempfile
from pathlib import Path
from unittest.mock import patch
from analyze import analyze_all
from perf_model import read_json
from selfcheck_fixtures import manifest, frame, streams, write_six


def check_pending(root, dictionary, budgets):
    from perf_store import AI_BATCHES_KEEP, exclusive
    with tempfile.TemporaryDirectory(prefix='uestc-perf-pending-') as temporary:
        work = Path(temporary)
        out, author = work / 'perf', work / 'author'
        source = out / 'runs/abandoned/session'
        meta = manifest(dictionary, 'ABANDONED')
        meta.update(status='recording', complete=False, endedAt=None)
        write_six(source, meta, streams([], dictionary), dictionary)
        (source / 'manifest.json').rename(source / 'manifest.pending.json')
        # A crashed stream may end in the middle of a row; pending indexing never parses it.
        (source / 'frames.csv').write_text('truncated', encoding='utf-8')
        (source / '.lock').unlink()
        missing_lock = analyze_all(root, out, author)
        assert str(source.resolve()) in missing_lock['skipped'], '没有可独占锁的 pending 被读进索引'
        (source / '.lock').touch()
        with exclusive(source / '.lock') as held:
            assert held
            locked = analyze_all(root, out, author)
            assert str(source.resolve()) in locked['skipped'], '活动 pending 没有被发现并锁定跳过'
            assert not read_json(out / 'index.json')['sessions'], '活动 pending 被读进索引'
        analyzed = analyze_all(root, out, author)
        assert analyzed['analyzed'] == ['ABANDONED'] and not analyzed['errors'], analyzed
        entry = read_json(out / 'index.json')['sessions'][0]
        assert entry['status'] == 'partial', '可独占的 pending 未作为 partial 进索引'
        assert read_json(out / 'derived/ABANDONED/summary.json')['all'] is None, 'pending 解析了未闭合流'
        for i in range(AI_BATCHES_KEEP):
            newer = out / 'runs' / f'newer-{i:02d}/session'
            write_six(newer, manifest(dictionary, f'NEWER-{i}'), streams([], dictionary), dictionary)
        with exclusive(source / '.lock') as held:
            assert held
            active = analyze_all(root, out, author)
            assert not active['errors'] and source.exists(), '修剪删除了锁仍占用的 pending'
        final = analyze_all(root, out, author)
        assert not final['errors'], final
        assert not source.exists(), '锁已释放的 pending 永不过期'
        assert not (out / 'derived/ABANDONED').exists(), '过期 pending 留下派生'


def check_processed_error(root, dictionary, budgets):
    import analyze
    with tempfile.TemporaryDirectory(prefix='uestc-perf-error-') as temporary:
        out, author = Path(temporary) / 'perf', Path(temporary) / 'author'
        source = author / 'BROKEN'
        write_six(source, manifest(dictionary, 'BROKEN'), streams([frame(dictionary, 0)], dictionary), dictionary)
        (source / 'frames.csv').write_text('truncated', encoding='utf-8')
        with patch.object(analyze, 'load_session', wraps=analyze.load_session) as loads:
            first = analyze_all(root, out, author)
            assert first['errors'] and not first['analyzed'], '夹具未触发原始流解析失败'
            second = analyze_all(root, out, author)
            assert second['errors'] == first['errors'], '错误原因没有保留'
            assert loads.call_count == 1, '同一错误输入被重复分析'
        assert not analyze.analysis_needed(root, out, author), '已处理错误仍被判为待分析'
        (source / 'frames.csv').write_text('changed', encoding='utf-8')
        assert analyze.analysis_needed(root, out, author), '错误输入变化后未重新判鲜'
        (source / 'manifest.json').unlink()
        assert analyze.analysis_needed(root, out, author), '删除错误来源后没有重新判鲜'
        assert not analyze_all(root, out, author)['errors'], '已经删除的错误仍留在索引'

        derived_source = author / 'DERIVED'
        write_six(derived_source, manifest(dictionary, 'DERIVED'), streams([frame(dictionary, 0)], dictionary), dictionary)
        with patch.object(analyze, 'curve_document', side_effect=RuntimeError('selfcheck derived content invalid')) as derives:
            first = analyze_all(root, out, author)
            assert first['errors'][0]['kind'] == 'content', first
            assert analyze_all(root, out, author)['errors'] == first['errors']
            assert derives.call_count == 1 and not analyze.analysis_needed(root, out, author), '派生内容失败反复重跑'
        (derived_source / 'frames.csv').touch()
        assert analyze.analysis_needed(root, out, author), '派生内容失败的输入变化未重试'
        assert analyze_all(root, out, author)['analyzed'] == ['DERIVED']
        (derived_source / 'manifest.json').unlink()

        source = author / 'RETRY'
        write_six(source, manifest(dictionary, 'BROKEN'), streams([frame(dictionary, 0)], dictionary), dictionary)
        assert analyze_all(root, out, author)['analyzed'] == ['BROKEN']
        with patch.object(analyze, 'load_session', side_effect=PermissionError('selfcheck read denied')) as loads:
            # 输入改变后读失败；IO 恢复不需要再次改变输入。
            (source / 'frames.csv').touch()
            failed = analyze_all(root, out, author)
            assert failed['errors'][0]['kind'] == 'io' and not read_json(out / 'index.json')['sessions']
            assert analyze.analysis_needed(root, out, author), 'IO 失败被输入戳缓存'
            index_stamp = (out / 'index.json').stat().st_mtime_ns
            analyze_all(root, out, author)
            assert loads.call_count == 2, 'IO 错误没有重新尝试'
            assert (out / 'index.json').stat().st_mtime_ns == index_stamp, '同一 IO 错误重写 index 触发通知空转'
        recovered = analyze_all(root, out, author)
        assert recovered['analyzed'] == ['BROKEN'] and not recovered['errors'], recovered
        (out / 'derived/BROKEN/anomalies.json').unlink()
        assert analyze.analysis_needed(root, out, author), '缺失派生文件没有重新判鲜'
        assert analyze_all(root, out, author)['analyzed'] == ['BROKEN']

        other = author / 'SAME-ID'
        write_six(other, manifest(dictionary, 'BROKEN'), streams([frame(dictionary, 0)], dictionary), dictionary)
        conflict = analyze_all(root, out, author)
        assert conflict['errors'][0]['kind'] == 'conflict', conflict
        assert not analyze.analysis_needed(root, out, author), '未变冲突反复启动分析器'
        (source / 'manifest.json').unlink()
        assert analyze.analysis_needed(root, out, author), '冲突持有方删除后未重判'
        resolved = analyze_all(root, out, author)
        assert resolved['analyzed'] == ['BROKEN'] and not resolved['errors'], resolved
        assert read_json(out / 'derived/BROKEN/summary.json')['source'] == str(other.resolve())
        (other / 'manifest.json').unlink()
        assert analyze.analysis_needed(root, out, author), '成功来源消失后未重判'
        analyze_all(root, out, author)
        assert not (out / 'derived/BROKEN').exists(), '已失去会话清单的来源仍保留派生'
        check_stale_content_cache(root, out, author, dictionary)


def check_stale_content_cache(root, out, author, dictionary):
    """Regression：旧内容错误不能借当前有效摘要永远滞留，当前坏输入仍缓存。"""
    import analyze
    source = author / 'STALE-CONTENT'
    write_six(source, manifest(dictionary, 'STALE-CONTENT'), streams([frame(dictionary, 0)], dictionary), dictionary)
    assert analyze_all(root, out, author)['analyzed'] == ['STALE-CONTENT']
    index_path = out / 'index.json'
    summary = read_json(out / 'derived/STALE-CONTENT/summary.json')
    for stale in ('calc', 'stamp'):
        index = read_json(index_path)
        stamp = dict(summary['inputStamp'])
        if stale == 'stamp':
            stamp['schema'] = -1
        index['errors'] = [dict(source=str(source.resolve()), id='STALE-CONTENT', kind='content',
                                reason='selfcheck stale content', calc=analyze.CALC - (stale == 'calc'), inputStamp=stamp)]
        index_path.write_text(json.dumps(index), encoding='utf-8')
        assert analyze.analysis_needed(root, out, author), '过期内容错误借用了现存摘要缓存'
        with patch.object(analyze, 'load_session', wraps=analyze.load_session) as loads:
            repaired = analyze_all(root, out, author)
            assert repaired['analyzed'] == ['STALE-CONTENT'] and not repaired['errors'] and loads.call_count == 1


def check_retention_authority(root, dictionary, budgets):
    from perf_store import diagnostic_attachments, history_group
    with tempfile.TemporaryDirectory(prefix='uestc-perf-retention-') as temporary:
        runs = Path(temporary) / 'runs'
        record = runs / 'batch'
        record.mkdir(parents=True)
        attachment, raw = record / 'capture.bin', record / 'frames.csv'
        attachment.write_bytes(b'abcd')
        raw.write_text('raw', encoding='utf-8')
        (record / 'diagnostics.json').write_text(json.dumps({'attachments': ['capture.bin', 'capture.bin', 'frames.csv', '../outside.bin']}), encoding='utf-8')
        (runs / 'outside.bin').write_bytes(b'outside')
        assert diagnostic_attachments(runs) == [attachment.resolve()], '诊断附件重复计数或混入原始/越界文件'
        a = dict(origin='play', comparisonKey={'scene': 'a'}, scope='full')
        b = dict(origin='play', comparisonKey={'scene': 'b'}, scope='cost')
        assert history_group(a) == history_group(b), '作者历史没有共用单组'
        a['origin'], b['origin'] = 'ai', 'ai'
        assert history_group(a) != history_group(b), 'AI 比较键与口径未分组'


def check_roundtrip(root, dictionary, budgets):
    policy = budgets['perfAnomalies']
    warm = policy['medianMinimumFrames']
    count = min(60, warm - 1)
    with tempfile.TemporaryDirectory(prefix='uestc-perf-selfcheck-') as temporary:
        work = Path(temporary)
        out, author = work / 'perf', work / 'author'
        source = author / 'SELFCHECK'
        meta = manifest(dictionary)
        meta['desktop'] = dict(kind='isolated', isInputDesktop=False, windowFocused=False)
        data = streams([frame(dictionary, i, gpu=budgets['deviceTargets']['developer']['mainViewportGpuP95MsUpper'] * 2) for i in range(count)], dictionary)
        write_six(source, meta, data, dictionary)
        first = analyze_all(root, out, author)
        assert first['analyzed'] == ['SELFCHECK'] and not first['errors'], first
        second = analyze_all(root, out, author)
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
            locked = analyze_all(root, out, author)
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

"""提供性能分析自检共用的当前 schema 六件夹具。
用法：由 scripts/perf 的职责自检模块导入，不单独运行。
"""
import csv
import json


def configure_monitor(root):
    """工作树只复用只读协作库；投影用本树文件，所有可写路径由夹具另行绑定。"""
    import importlib.util
    import subprocess
    import sys
    from pathlib import Path
    common = Path(subprocess.check_output(['git', '-C', str(root), 'rev-parse', '--path-format=absolute', '--git-common-dir'], text=True).strip())
    sys.path.append(str(common.parent / '.collab/tools'))
    # Load this checkout's CLI before the monitor reader adds its shared tool path.
    import perf
    import monitor.view
    local = root / '.collab/tools/monitor/view/perf.py'
    if local.is_file():
        spec = importlib.util.spec_from_file_location('monitor.view.perf', local)
        module = importlib.util.module_from_spec(spec)
        sys.modules['monitor.view.perf'] = module
        spec.loader.exec_module(module)
        monitor.view.perf = module
    sys.path.insert(0, str(root / 'scripts/perf'))


def manifest(dictionary, identifier='SELFCHECK'):
    return dict(schema=dictionary['schema'], id=identifier, origin='play', status='closed', complete=True,
                startedAt='2000-01-01T00:00:00Z', endedAt='2000-01-01T00:00:05Z', recordedSeconds=5,
                build=dict(commit=None, dirty=False, configuration='Debug', godot='selfcheck'),
                machine=dict(id='selfcheck-only', gpu='RTX 4080', api='selfcheck', driver='selfcheck', os='selfcheck'),
                display=dict(windowWidthPx=3840, windowHeightPx=2160, refreshHz=60, windowMode='fullscreen'),
                desktop=dict(kind='interactive', isInputDesktop=True, windowFocused=True),
                pacing=dict(maxFps=60, vsync='disabled'),
                graphics=dict(preset='fine', effective={'selfcheck': True}, targetWidthPx=3840, targetHeightPx=2160,
                              internalWidthPx=3840, internalHeightPx=2160),
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


def streams(frames, dictionary):
    # Real sessions contain clock anchors; include one so association handles a nonempty event list.
    when = frames[0]['monotonicMs'] if frames else 0
    anchor = dict(schema=dictionary['schema'], sequence=1, event='clock.anchor', source='selfcheck', collectionFrame=None,
                  monotonicMs=when, payload=dict(unixMs=0, monotonicMs=when, readSpanMs=0))
    return dict(frames=frames, samples=[], hw=[], hwfast=[], events=[anchor])


def ai_run(scene='baseline-selfcheck', batch='fixture', phase='a1', repetition=1, kind='repeat3', scenes=None):
    return dict(scene=scene, purpose='baseline', status='valid', reasons=[], exitCode=0, timedOut=False,
                coverage=dict(sample=dict(observed=1, required=1)), conditions=dict(suite='fixed'), sampleSegmentId='sample',
                protocol=dict(kind=kind, batchId=batch, scenes=scenes or [scene], phase=phase, repetition=repetition))


def mark_sample(data, dictionary, meta, begin=0, end=None):
    end = end if end is not None else data['frames'][-1]['monotonicMs'] + 1
    for phase, when in (('begin', begin), ('end', end)):
        data['events'].append(dict(schema=dictionary['schema'], sequence=0, event='segment', source='selfcheck',
                                   collectionFrame=data['frames'][0]['collectionFrame'] if data['frames'] else 0,
                                   monotonicMs=when, payload=dict(id='sample', phase=phase)))
    meta['samplingConditions'] = dict(segmentId='sample', monotonicMs=begin, renderDrawFrame=0,
                                     collectionFrame=data['frames'][0]['collectionFrame'] if data['frames'] else 0)
    data['events'].sort(key=lambda row: row['monotonicMs'])
    for sequence, row in enumerate(data['events'], 1):
        row['sequence'] = sequence
    return data


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



"""观察台第二版项目采集：只发布作者高层信息与真实统计。
用法：python -I -X utf8 dashboard/tools/collect.py（live --loop 输入变化即采，无变化每两分钟刷新）。
产出：dashboard/data/auto/{attention,feedback,progress,team,system,hookfeed,stats,records,docs,index}.js。
"""
import glob
import os
import re
import sys
import time
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(ROOT, '.collab', 'tools'))
import datafile
from monitor import util as U
from monitor.read import collab
from monitor.view import attention, progress, team, system, stats, hookfeed
SOURCE = 'collect.py'

def rel(p):
    return os.path.relpath(p, ROOT).replace(os.sep, '/')


def read_text(path):
    return open(os.path.join(ROOT, path), encoding='utf-8-sig', errors='ignore').read().replace('\r\n', '\n')


def brief(text):
    """去掉强调与反引号，收成一行；不截断（页面先显示摘要，细节点开看）。"""
    return re.sub(r'\s+', ' ', re.sub(r'[`*]', '', text or '')).strip()


def cells(line):
    return [c.strip() for c in line.strip().strip('|').split('|')]


def front(text):
    """文档头部 front-matter 里的简单键值（只认 `键: 值` 的单行）。"""
    m = re.match(r'^---\n(.*?)\n---\n', text, re.S)
    return dict(re.findall(r'^(\w+):\s*(.*)$', m.group(1), re.M)) if m else {}


def code_pattern(codes):
    """匹配文档编码的正则：长编码优先，免得 ENG-GATES 吃掉 ENG-GATES-SHOTS。"""
    return re.compile(r'(?<![A-Za-z0-9-])(' + '|'.join(re.escape(c) for c in sorted(codes, key=len, reverse=True)) + r')(?![A-Za-z0-9-])')


def index_domains(index_text, pattern):
    """CORE-INDEX §1 文档地图里的域与主文档，顺序照原文：[(域名, [编码…])]。域是"**名字**（`文件夹`）"开头的段，
    段里先出现的编码是它的主文档；"信息在哪""仓库目录"这类不带文件夹的段不算域，一份文档只归第一次出现的那个域。"""
    m = re.search(r'^## 1 .*?(?=^## |\Z)', index_text, re.S | re.M)
    seen, out = set(), []
    for block in re.split(r'\n(?=\*\*)', m.group(0) if m else ''):
        name = re.match(r'\*\*([^*]+)\*\*（', block)
        codes = [c for c in dict.fromkeys(pattern.findall(block)) if c not in seen]
        if name and codes:
            seen.update(codes)
            out.append((name.group(1), codes))
    return out


def doc_files():
    out = []
    for p in sorted(glob.glob(os.path.join(ROOT, 'docs', '**', '*.md'), recursive=True)):
        r = rel(p)
        if not r.startswith('docs/godot-api/') and r != 'docs/SECTIONS.md':
            out.append(r)
    return out


def docs_data():
    # 继续沿用 CORE-INDEX 域及 parent 排序，只保留作者需要的编码、简题、摘要。
    items = []
    for path in doc_files():
        text = read_text(path)
        fm = front(text)
        code = fm.get('id')
        if not code:
            continue
        items.append({'code': code, 'title': fm.get('title', code), 'summary': fm.get('summary', ''),
                      '_parent': fm.get('parent'), '_body': text})
    by_code = {d['code']: d for d in items}
    pattern = code_pattern(by_code)
    top = by_code.get('CORE-INDEX')
    children = {}
    for d in items:
        if d['_parent'] in by_code:
            children.setdefault(d['_parent'], []).append(d)
    for parent, ds in children.items():
        body = by_code[parent]['_body']
        ds.sort(key=lambda d: (body.find(d['code']) if d['code'] in body else len(body), d['code']))
    def subtree(d):
        return [d] + [x for child in children.get(d['code'], []) for x in subtree(child)]
    domains, placed = [], set()
    if top:
        domains.append({'name': '顶层', 'docs': [top]})
        placed.add(top['code'])
    for name, mains in index_domains(top['_body'] if top else '', pattern):
        group = [x for code in mains if code not in placed for x in subtree(by_code[code])]
        placed.update(x['code'] for x in group)
        domains.append({'name': name, 'docs': group})
    loose = [d for d in items if d['code'] not in placed]
    if loose:
        domains.append({'name': '待归入索引', 'docs': loose})
    return {'domains': [{'name': d['name'], 'docs': [{k: x[k] for k in ('code', 'title', 'summary')} for x in d['docs']]} for d in domains]}


def decision_text(text):
    """从登记结论取一句短摘，不发布实现路径和提交哈希。"""
    text = re.sub(r'\[([^]]+)\]\([^)]+\)', r'\1', text)
    text = re.sub(r'`[^`]*[\\/][^`]*`', '', text)
    text = brief(text)
    text = re.sub(r'(?<![A-Za-z0-9])[0-9a-fA-F]{7,40}(?![A-Za-z0-9])', '', text)
    text = re.sub(r'[A-Za-z]:[\\/][^\s，。；、“”「」（）()<>]+', '', text)
    text = re.sub(r'(?:[^\s，。；：、“”「」（）()<>/\\]+[\\/])+[^\s，。；：、“”「」（）()<>/\\]+', '', text)
    text = re.sub(r'(?<![\w.-])[\w.-]+\.[A-Za-z][A-Za-z0-9]{0,9}(?![\w.-])', '', text)
    text = re.split(r'[。；;]', text, 1)[0].strip(' ，、：:')
    if len(text) > 40:
        cut = max(text.rfind(mark, 0, 40) for mark in ('，', '、', '：', ':'))
        text = text[:cut].rstrip(' ，、：:') if cut >= 12 else text[:39].rstrip(' ，、：:') + '…'
    return text or '结论尚无可公开的文字摘要'


def decisions_data(limit=15):
    """只整理资料页要展示的最近结论；时刻取编号首次进入 Git 的真实新增。"""
    rows = []
    for p in sorted(glob.glob(os.path.join(ROOT, 'docs', 'registry', 'REG-DECISIONS-*.md'))):
        for line in read_text(rel(p)).splitlines():
            values = cells(line)
            if len(values) >= 5 and re.fullmatch(r'D-\d+', values[0]):
                rows.append({'id': values[0], 'text': decision_text(values[1])})
    rows = sorted(rows, key=lambda row: -int(row['id'][2:]))[:limit]
    selected = {row['id'] for row in rows}
    registered, at = {}, None
    if selected:
        pattern = r'^\|\s*(' + '|'.join(sorted(selected)) + r')\s*\|'
        log = U.git('log', '--reverse', '-p', '--format=%x1e%ct', '--unified=0', '-G', pattern,
                    '--', ':(glob)docs/**/*DECISIONS*.md')
        for line in log.split("\n"):
            if line.startswith('\x1e'):
                stamp = line[1:].strip()
                at = U.iso(float(stamp)) if stamp.isdigit() else None
            else:
                found = re.match(r'^\+\|\s*(D-\d+)\s*\|', line)
                if found and found[1] in selected and at:
                    registered.setdefault(found[1], at)
                    if len(registered) == len(selected):
                        break
    return {'decisions': [{**row, 'at': registered.get(row['id'])} for row in rows]}


def main():
    datasets, failed = {}, []
    def take(name, fn):
        try:
            datasets[name] = fn()
        except Exception as error:
            failed.append(name)
            message = f'{type(error).__name__}: {error}'
            print(f'collect：{name} 采集失败（{message}）', file=sys.stderr)
            datasets[name] = {'errors': [message]}
        return datasets[name]
    def require(*names):
        for name in names:
            if name in failed:
                raise ValueError('所需来源采集失败：' + name)
    facts, sampled = None, None
    try:
        rows = collab.queue_rows()
        facts = progress.evidence(rows)
    except Exception as error:
        rows = None
        queue_error = error
    def progress_data():
        if rows is None:
            raise queue_error
        return progress.build(rows, facts)
    take('progress', progress_data)
    try:
        if rows is None:
            raise queue_error
        sampled = team.sample(rows)
    except Exception as error:
        sample_error = error
    def team_data():
        if sampled is None:
            raise sample_error
        return team.build(rows, sampled)
    take('team', team_data)
    take('docs', docs_data)
    def system_data():
        require('docs')
        return system.build(datasets['docs'])
    take('system', system_data)
    take('hookfeed', hookfeed.build)
    take('records', decisions_data)
    def statistics():
        require('progress', 'team', 'system', 'docs')
        return stats.build(datasets['progress'], datasets['team'], datasets['system'], datasets['docs'],
                           {**facts, 'agents': sampled[1]})
    take('stats', statistics)
    for name, data in datasets.items():
        datafile.write('auto', name, data, source=SOURCE)
    # 与两个交互 API 共用写锁，避免采集中途覆盖已经确认的状态。
    from post import locked
    with locked():
        for name, fn in (('attention', attention.build), ('feedback', attention.feedback)):
            datafile.write('auto', name, take(name, fn), source=SOURCE)
    from post import prune_all
    prune_all()
    progress.save('dashboard-collect.json', {'at': U.iso(time.time()), 'errors': failed})
    datafile.refresh_index()
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())

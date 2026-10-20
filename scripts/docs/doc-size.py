"""统计 docs/ 里每份文档的字数，列出超体量上限的（上限只写在 STD-DOC §5 的表里，这里每次从表里读）。

用法：
  python -I -X utf8 scripts/docs/doc-size.py            每份文档一行，从大到小，超限的标 ✗
  python -I -X utf8 scripts/docs/doc-size.py --over     只列超限项；有超限时退出码为 1
  python -I -X utf8 scripts/docs/doc-size.py --json --files docs/x.md ...   只量给定文件，输出超限项的 JSON（提交后钩子用）

字数 = 去掉文件头后的字符数；一节 = 从一个标题到下一个标题（不认代码块里的 #）。
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
D = os.path.join(ROOT, 'docs')
# STD-DOC §5 表里"部分"一栏的开头 → 本脚本的键；放宽上限只改那张表
ROWS = {'CORE-INDEX 除 §1 外的各节': 'core0', 'CORE-INDEX §1': 'core1', 'WF-ENTRY': 'entry', '每份文档的 §0': 's0',
        '§0.1': 'hooks', '单节': 'section', '单份规格': 'doc', '单份地图': 'map'}


def limits():
    path = glob.glob(os.path.join(D, 'standards', 'STD-DOC-*.md'))[0]
    sec = open(path, encoding='utf-8').read().split('## 5 体量上限', 1)[1].split('\n## ', 1)[0]
    out = {}
    for part, n in re.findall(r'^\| (.+?) \| ([\d,]+) 字 \|$', sec, re.M):
        key = next((k for head, k in ROWS.items() if part.startswith(head)), None)
        if key:
            out[key] = int(n.replace(',', ''))
    missing = sorted(set(ROWS.values()) - set(out))
    if missing:
        sys.exit('STD-DOC §5 的上限表里读不到：' + '、'.join(missing))
    return out


LIMIT = limits()
REGISTRY = {'REG-DECISIONS', 'REG-GLOSSARY', 'PLAN-WP'}  # 登记类（连同它们的子文档）：每条一行，不量全文与单节


def registry(code):
    return any(code == r or code.startswith(r + '-') for r in REGISTRY)


def docs_in(paths=None):
    if paths:
        files = [os.path.join(ROOT, p) if not os.path.isabs(p) else p for p in paths]
    else:
        files = glob.glob(os.path.join(D, '**', '*.md'), recursive=True)
    out = []
    for p in files:
        rel = os.path.relpath(p, D).replace(chr(92), '/')
        if rel.startswith(('godot-api/', '..')) or rel == 'SECTIONS.md' or not os.path.isfile(p):
            continue
        out.append(p)
    return sorted(out)


def measure(path):
    text = open(path, encoding='utf-8').read().replace('\r\n', '\n')
    m = re.match(r'^---\n(.*?)\n---\n', text, re.S)
    code = re.search(r'^id:\s*(\S+)', m.group(1), re.M).group(1) if m else os.path.basename(path)
    body = text[m.end():] if m else text
    sections, cur, fence = [], ['', []], False
    for line in body.split('\n'):
        if line.lstrip().startswith('```'):
            fence = not fence
        h = None if fence else re.match(r'^(#{2,4}) (.*)$', line)
        if h:
            sections.append(cur)
            cur = [h.group(2).strip(), []]
        else:
            cur[1].append(line)
    sections.append(cur)
    secs = [(title, len('\n'.join(lines))) for title, lines in sections if title]
    return code, len(body), secs


def check(path):
    """返回 (编码, 正文字数, [(哪一处, 字数, 上限)])"""
    code, total, secs = measure(path)
    over = []
    if not registry(code):
        cap = LIMIT['map'] if code.startswith('MAP-') else LIMIT['doc']
        if total > cap:
            over.append(('全文', total, cap))
    for title, n in secs:
        if code == 'CORE-INDEX' and re.match(r'^\d+ ', title):   # 顶层各节整节注入：§1 信息地图一档，其余一档
            cap = LIMIT['core1'] if title.startswith('1 ') else LIMIT['core0']
        elif title.startswith('0 '):
            cap = LIMIT['s0']
        elif '接入要点' in title:
            cap = LIMIT['hooks']
        elif code == 'WF-ENTRY' and re.match(r'^[1-9] ', title):
            cap = LIMIT['entry']
        elif registry(code):
            continue
        else:
            cap = LIMIT['section']
        if n > cap:
            over.append((title, n, cap))
    return code, total, over


def main(argv):
    files = argv[argv.index('--files') + 1:] if '--files' in argv else None
    results = [check(p) + (os.path.relpath(p, ROOT).replace(chr(92), '/'),) for p in docs_in(files)]
    bad = [r for r in results if r[2]]
    if '--json' in argv:
        print(json.dumps([{'path': p, 'code': c, 'total': t, 'over': [list(o) for o in ov]} for c, t, ov, p in bad], ensure_ascii=False))
        return 0
    if '--over' not in argv:
        for c, t, ov, p in sorted(results, key=lambda r: -r[1]):
            print('%s %-20s %7d 字  %s' % ('✗' if ov else ' ', c, t, p))
        print('共 %d 份，%d 字；超限 %d 份' % (len(results), sum(r[1] for r in results), len(bad)))
    for c, t, ov, p in bad:
        for where, n, cap in ov:
            print('超限  %-20s %s：%d 字（上限 %d）' % (c, where, n, cap))
    return 1 if bad and '--over' in argv else 0


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    sys.exit(main(sys.argv[1:]))

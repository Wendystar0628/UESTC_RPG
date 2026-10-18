"""观察台数据文件的读写：每个数据文件是一行 `Dash.put("名字", <JSON>);`，页面用 <script> 直接加载（file:// 下也能用）。
采集脚本、发布工具都经这里读写，不手写数据文件。写入先写临时文件再改名，页面不会读到半个文件。

用法：
  from datafile import read, write
  write('auto', 'queue', {...})        # dashboard/data/auto/queue.js
  data = read('curated', 'notices')    # 没有这个文件时返回 None
"""
import json
import os
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # dashboard/
DATA = os.path.join(ROOT, 'data')
KINDS = ('auto', 'curated')


def path(kind, name):
    assert kind in KINDS, kind
    return os.path.join(DATA, kind, name + '.js')


def read(kind, name):
    p = path(kind, name)
    if not os.path.isfile(p):
        return None
    text = open(p, encoding='utf-8').read().strip()
    head = f'Dash.put({json.dumps(name)}, '
    if not (text.startswith(head) and text.endswith(');')):
        raise ValueError(f'{p} 不是观察台数据文件')
    return json.loads(text[len(head):-2])


def write(kind, name, payload, source=None):
    """payload 是 dict；自动补 updated（ISO 时刻）与 source（谁写的）。"""
    payload = dict(payload)
    payload['updated'] = time.strftime('%Y-%m-%dT%H:%M:%S')
    if source:
        payload['source'] = source
    p = path(kind, name)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    tmp = p + '.tmp'
    with open(tmp, 'w', encoding='utf-8') as f:
        f.write(f'Dash.put({json.dumps(name)}, {json.dumps(payload, ensure_ascii=False)});\n')
    os.replace(tmp, p)
    return p

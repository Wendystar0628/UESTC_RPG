"""把 Godot C# API 导出合成为按类拆分的 Markdown 参考，供开发代理检索。

输入（都在 docs/godot-api/raw/）：
  csharp/api.json                 ApiDump 用反射导出的 C# 签名
  csharp/GodotSharp*.xml          .NET 版自带的 C# 文档注释
  extension_api_with_docs.json    godot --dump-extension-api-with-docs，用来对照引擎名（snake_case）
输出（docs/godot-api/）：
  index.md、classes/<类型>.md、signatures.txt

用法：python -I scripts/godot-api/gen-api-docs.py
产出：docs/godot-api/classes/；docs/godot-api/signatures.txt；docs/godot-api/index.md。
"""
import json
import os
import re
import shutil
import xml.etree.ElementTree as ET
from collections import defaultdict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'docs', 'godot-api')
RAW = os.path.join(OUT, 'raw')
CLS = os.path.join(OUT, 'classes')

SKIP_NESTED = {'MethodName', 'PropertyName', 'SignalName'}
SKIP_NS_PREFIX = ('Godot.NativeInterop', 'Godot.Bridge', 'Godot.SourceGenerators')

# ---------- 载入 ----------
api = json.load(open(os.path.join(RAW, 'csharp', 'api.json'), encoding='utf-8'))
types = [t for t in api['types'] if t['ns'] and t['ns'].startswith('Godot')
         and not t['ns'].startswith(SKIP_NS_PREFIX)]
by_full = {t['fullName']: t for t in types}

docs = {}
for fn in ('GodotSharp.xml', 'GodotSharpEditor.xml'):
    tree = ET.parse(os.path.join(RAW, 'csharp', fn))
    for m in tree.getroot().iter('member'):
        docs[m.get('name')] = m

eng = json.load(open(os.path.join(RAW, 'extension_api_with_docs.json'), encoding='utf-8'))
eng_classes = {c['name']: c for c in eng['classes']}
eng_builtin = {c['name']: c for c in eng['builtin_classes']}
version = eng['header']['version_full_name']


def eng_names(cname):
    c = eng_classes.get(cname) or eng_builtin.get(cname)
    if not c:
        return {}
    names = {}
    for key, label in (('methods', 'method'), ('properties', 'property'), ('members', 'property'),
                       ('signals', 'signal'), ('constants', 'constant')):
        for x in c.get(key, []) or []:
            names[x['name']] = label
    for e in c.get('enums', []) or []:
        names[e['name']] = 'enum'
        for v in e.get('values', []):
            names[v['name']] = 'constant'
    return names


def snake(name):
    s = re.sub(r'(?<=[a-z0-9])([A-Z])', r'_\1', name)
    s = re.sub(r'(?<=[A-Z])([A-Z][a-z])', r'_\1', s)
    return s.lower()


# ---------- 页面归属 ----------
top_level = [t for t in types if not t['declaringType']]
page_of = {}  # fullName -> 页面文件名（不含路径）
for t in top_level:
    base = t['name'].split('<')[0]
    arity = t['fullName'].rsplit('`', 1)[1] if '`' in t['fullName'] else ''
    page = base if t['ns'] == 'Godot' else t['ns'].replace('Godot.', '') + '.' + base
    page_of[t['fullName']] = page + (f'-{arity}' if arity else '')
nested = defaultdict(list)
for t in types:
    if t['declaringType']:
        leaf = t['fullName'].rsplit('.', 1)[-1]
        if leaf in SKIP_NESTED:
            continue
        nested[t['declaringType']].append(t)
        root = t['declaringType']
        while root in by_full and by_full[root]['declaringType']:
            root = by_full[root]['declaringType']
        if root in page_of:
            page_of[t['fullName']] = page_of[root]


# ---------- 文档注释 → Markdown ----------
def short_cref(cref, here):
    kind, _, rest = cref.partition(':')
    rest = re.sub(r'\(.*\)$', '', rest)
    rest = re.sub(r'`+\d+', '', rest)
    if kind == 'T':
        full = rest
        member = None
    else:
        full, _, member = rest.rpartition('.')
        if member == '#ctor':
            member = None
    display_type = full.replace('Godot.', '', 1) if full.startswith('Godot.') else full
    page = page_of.get(full)
    if member:
        text = (member if full == here else f'{display_type}.{member}') + ('()' if kind == 'M' else '')
    else:
        text = display_type
    if page and full != here:
        return f'[`{text}`]({page}.md)'
    return f'`{text}`'


def inline(el, here, depth=0):
    out = [el.text or '']
    for ch in el:
        tag = ch.tag
        if tag == 'see' or tag == 'seealso':
            if ch.get('cref'):
                out.append(short_cref(ch.get('cref'), here))
            elif ch.get('langword'):
                out.append(f"`{ch.get('langword')}`")
            elif ch.get('href'):
                out.append(f"[{(ch.text or ch.get('href')).strip()}]({ch.get('href')})")
        elif tag in ('c',):
            out.append('`' + ''.join(ch.itertext()).strip() + '`')
        elif tag == 'code':
            out.append('\n\n```csharp\n' + ''.join(ch.itertext()).strip('\n') + '\n```\n\n')
        elif tag == 'b':
            out.append('**' + inline(ch, here).strip() + '**')
        elif tag == 'i':
            out.append('*' + inline(ch, here).strip() + '*')
        elif tag == 'para':
            out.append('\n\n' + inline(ch, here).strip() + '\n\n')
        elif tag == 'br':
            out.append('\n')
        elif tag in ('paramref', 'typeparamref'):
            out.append(f"`{ch.get('name')}`")
        elif tag == 'a':
            out.append(f"[{inline(ch, here).strip()}]({ch.get('href')})")
        elif tag == 'list':
            for item in ch.iter('item'):
                out.append('\n- ' + inline(item, here).strip())
            out.append('\n')
        else:
            out.append(inline(ch, here))
        out.append(ch.tail or '')
    return ''.join(out)


def pascal(n):
    if re.fullmatch(r'[A-Z0-9_]+', n):
        return ''.join(w.capitalize() for w in n.split('_') if w)
    return ''.join(w[:1].upper() + w[1:] for w in n.split('_') if w)


MEMBERS = defaultdict(set)
for _t in types:
    for _m in _t['members']:
        MEMBERS[_t['name']].add(_m['name'])


def csharpify(m):
    """引擎文档里残留的 GDScript 写法（`Vector3.ZERO`、`@GlobalScope.deg_to_rad`）换成 C# 名。"""
    owner, member = m.group(1), m.group(2)
    p = pascal(member)
    if owner == '@GlobalScope':
        for host in ('Mathf', 'GD', 'GodotObject'):
            if p in MEMBERS.get(host, ()):
                return f'`{host}.{p}`'
        return m.group(0)
    if p in MEMBERS.get(owner, ()):
        return f'`{owner}.{p}`'
    return m.group(0)


def clean(text):
    text = re.sub(r'`(@GlobalScope|[A-Z][A-Za-z0-9]*)\.([A-Za-z_][A-Za-z0-9_]*)(?:\(\))?`', csharpify, text)
    text = re.sub(r'[ \t]+\n', '\n', text)
    text = re.sub(r'\n[ \t]+', '\n', text)
    text = re.sub(r'\n{3,}', '\n\n', text)
    return text.strip()


def doc_for(doc_id, here, seen=None):
    seen = seen or set()
    m = docs.get(doc_id)
    if m is None or doc_id in seen:
        return ''
    seen.add(doc_id)
    inh = m.find('inheritdoc')
    if inh is not None and inh.get('cref'):
        return doc_for(inh.get('cref'), here, seen)
    parts = []
    s = m.find('summary')
    if s is not None:
        parts.append(inline(s, here))
    params = [(p.get('name'), clean(inline(p, here))) for p in m.findall('param')]
    params = [(n, t) for n, t in params if t]
    if params:
        parts.append('\n\n参数：\n' + '\n'.join(f'- `{n}`：{t}' for n, t in params))
    r = m.find('returns')
    if r is not None and clean(inline(r, here)):
        parts.append('\n\n返回：' + clean(inline(r, here)))
    return clean(''.join(parts))


def first_sentence(text):
    text = re.sub(r'```.*?```', '', text, flags=re.S).strip()
    para = text.split('\n\n')[0].replace('\n', ' ')
    m = re.match(r'(.+?[.。!?])(\s|$)', para)
    s = m.group(1) if m else para
    return s[:220] + ('…' if len(s) > 220 else '')


# ---------- 继承与分类 ----------
def base_full(t):
    b = t['baseType']
    if not b:
        return None
    b = b.split('<')[0]
    for cand in (f"Godot.{b}", b):
        if cand in by_full:
            return cand
    return None


def chain(t):
    out = []
    cur = t
    while cur:
        bf = base_full(cur)
        if not bf:
            break
        out.append(by_full[bf]['name'])
        cur = by_full[bf]
    return out


def category(t):
    if t['assembly'] == 'GodotSharpEditor':
        return '编辑器'
    if t['ns'] != 'Godot':
        return '其他命名空间'
    if t['kind'] == 'struct':
        return '内置值类型'
    if t['kind'] == 'enum':
        return '全局枚举'
    if t['kind'] in ('delegate', 'interface'):
        return '委托与接口'
    if t['kind'] == 'static class':
        return '静态类与单例'
    c = chain(t) + [t['name']]
    if 'Node3D' in c:
        return '3D 节点'
    if 'Control' in c:
        return '界面节点（Control）'
    if 'Node2D' in c or 'CanvasItem' in c:
        return '2D 节点'
    if 'Node' in c:
        return '其他节点'
    if 'Resource' in c:
        return '资源（Resource）'
    return '其他对象'


# ---------- 渲染一页 ----------
KIND_ORDER = [('ctor', '构造函数'), ('property', '属性'), ('method', '方法'), ('operator', '运算符'),
              ('event', '事件（信号）'), ('constant', '常量'), ('field', '字段'), ('enumValue', '枚举值'),
              ('invoke', '签名')]
sig_lines = []


def render_type(t, level=1):
    here = t['fullName']
    en = eng_names(t['name'].split('.')[-1].split('<')[0])
    lines = []
    h = '#' * level
    title = t['name'] if level == 1 else t['name'].split('.')[-1]
    lines.append(f"{h} {title}")
    meta = [f"命名空间 `{t['ns']}`", f"{t['kind']}"]
    ch = chain(t)
    if ch:
        meta.append('继承 ' + ' → '.join(f'[`{n}`]({page_of.get("Godot." + n, n)}.md)' for n in ch))
    if t['assembly'] == 'GodotSharpEditor':
        meta.append('仅编辑器（GodotSharpEditor）')
    lines.append('；'.join(meta))
    d = doc_for(t['docId'], here)
    if d:
        lines.append(d)
    by_kind = defaultdict(list)
    for m in t['members']:
        by_kind[m['kind']].append(m)
    for k, label in KIND_ORDER:
        ms = by_kind.get(k)
        if not ms:
            continue
        lines.append(f"{'#' * (level + 1)} {label}")
        if k == 'enumValue':
            rows = []
            for m in ms:
                dd = first_sentence(doc_for(m['docId'], here))
                rows.append(f"- `{m['sig']}`" + (f"：{dd}" if dd else ''))
                sig_lines.append(f"{here} :: {m['sig']}")
            lines.append('\n'.join(rows))
            continue
        for m in sorted(ms, key=lambda x: (x['name'], x['sig'])):
            sig_lines.append(f"{here} :: {m['sig']}")
            block = [f"{'#' * (level + 2)} {m['name']}", f"```csharp\n{m['sig']}\n```"]
            s = snake(m['name'])
            if k in ('property', 'method', 'event', 'constant') and s in en:
                block.append(f"引擎名：`{s}`")
            dd = doc_for(m['docId'], here)
            if dd:
                block.append(dd)
            lines.append('\n\n'.join(block))
    for n in sorted(nested.get(here, []), key=lambda x: x['name']):
        lines.append(render_type(n, min(level + 1, 4)))
    return '\n\n'.join(lines)


# ---------- 输出 ----------
if os.path.isdir(CLS):
    shutil.rmtree(CLS)
os.makedirs(CLS)
index_rows = defaultdict(list)
for t in sorted(top_level, key=lambda x: x['name'].lower()):
    page = page_of[t['fullName']]
    body = render_type(t)
    with open(os.path.join(CLS, page + '.md'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(f"<!-- 由 scripts/godot-api/gen-api-docs.py 生成，{version}。不要手改。 -->\n\n{body}\n")
    brief = first_sentence(doc_for(t['docId'], t['fullName']))
    if not brief:
        ec = eng_classes.get(t['name']) or eng_builtin.get(t['name'])
        brief = (ec or {}).get('brief_description', '') or ''
    brief = re.sub(r'\]\(([^)/]+\.md)\)', r'](classes/\1)', brief.replace('\n', ' '))
    index_rows[category(t)].append((t['name'], page, t['kind'], brief.replace('|', '\\|')))

with open(os.path.join(OUT, 'signatures.txt'), 'w', encoding='utf-8', newline='\n') as f:
    f.write(f"# {version} C# 公开 API，每行一个成员：<类型> :: <签名>\n")
    f.write('\n'.join(sorted(set(sig_lines))) + '\n')

QUICK = [
    ('场景与节点', ['Node', 'Node3D', 'SceneTree', 'PackedScene', 'ResourceLoader', 'ResourceSaver', 'WorkerThreadPool', 'Engine', 'OS', 'Time']),
    ('网格与几何生成', ['ArrayMesh', 'SurfaceTool', 'MeshDataTool', 'ImporterMesh', 'Mesh', 'MeshInstance3D', 'MultiMesh', 'MultiMeshInstance3D', 'GeometryInstance3D', 'VisualInstance3D', 'OccluderInstance3D', 'ArrayOccluder3D']),
    ('材质、着色器与纹理', ['Shader', 'ShaderMaterial', 'ShaderInclude', 'Image', 'ImageTexture', 'Texture2DArray', 'ImageTexture3D', 'Texture2DRD', 'Texture3DRD']),
    ('渲染服务器与后期', ['RenderingServer', 'RenderingDevice', 'Compositor', 'CompositorEffect', 'RenderSceneBuffersRD', 'RenderData', 'RDShaderFile', 'Viewport', 'SubViewport', 'Camera3D', 'CameraAttributesPractical', 'Performance']),
    ('光照、天空与环境', ['DirectionalLight3D', 'OmniLight3D', 'Light3D', 'WorldEnvironment', 'Environment', 'Sky', 'ShaderMaterial']),
    ('物理与行走', ['CharacterBody3D', 'StaticBody3D', 'CollisionObject3D', 'PhysicsServer3D', 'PhysicsDirectSpaceState3D', 'HeightMapShape3D', 'BoxShape3D', 'CylinderShape3D', 'ConvexPolygonShape3D', 'ConcavePolygonShape3D', 'KinematicCollision3D']),
    ('输入与相机', ['Input', 'InputEvent', 'InputEventMouseMotion', 'InputEventKey', 'InputMap', 'DisplayServer']),
    ('界面', ['Control', 'CanvasLayer', 'Theme', 'Label', 'Button', 'Tween', 'TranslationServer', 'FontFile', 'FontVariation']),
    ('数据与文件', ['FileAccess', 'DirAccess', 'Json', 'ConfigFile', 'ProjectSettings']),
    ('数学与内置值类型', ['Vector2', 'Vector3', 'Vector3I', 'Transform3D', 'Basis', 'Quaternion', 'Aabb', 'Plane', 'Color', 'Rid', 'StringName', 'NodePath', 'Variant', 'Mathf', 'GD']),
]
known = {t['name'] for t in top_level}
cat_order = ['3D 节点', '资源（Resource）', '静态类与单例', '内置值类型', '界面节点（Control）', '其他节点',
             '2D 节点', '其他对象', '全局枚举', '委托与接口', '其他命名空间', '编辑器']
with open(os.path.join(OUT, 'index.md'), 'w', encoding='utf-8', newline='\n') as f:
    f.write(f"# Godot C# API 索引（{version}）\n\n")
    f.write("由 `scripts/godot-api/gen-api-docs.py` 生成，不要手改。用法和命名规则见 [README](README.md)。"
            f"共 {len(top_level)} 个顶层类型，{len(sig_lines)} 个成员签名。\n\n")
    f.write("## 本项目常用速查\n\n")
    for title, names in QUICK:
        links = [f"[`{n}`](classes/{page_of['Godot.' + n]}.md)" for n in names if n in known and 'Godot.' + n in page_of]
        f.write(f"- **{title}**：" + '、'.join(dict.fromkeys(links)) + '\n')
    for cat in cat_order:
        rows = index_rows.get(cat)
        if not rows:
            continue
        f.write(f"\n## {cat}（{len(rows)}）\n\n| 类型 | 种类 | 简介 |\n| --- | --- | --- |\n")
        for name, page, kind, brief in rows:
            f.write(f"| [`{name}`](classes/{page}.md) | {kind} | {brief} |\n")
print('pages', len(top_level), 'signatures', len(sig_lines))

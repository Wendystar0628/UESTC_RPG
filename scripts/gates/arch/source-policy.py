"""G4 手写脚本与着色器检查：只检查声明和注释。
产出：无（只读脚本与着色器，向标准输出返回 JSON 检查结果）。
用法：由 SourcePolicyGateTests 通过标准输入传入 JSON，不单独运行。
"""
import ast
import io
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tokenize
from tree_sitter import Language, Parser
import tree_sitter_javascript as javascript


PS_SOURCE = r'''
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$request = [Console]::In.ReadToEnd() | ConvertFrom-Json
$violations = [Collections.Generic.List[string]]::new()
function Add-SourceViolation {
    param([string]$Path, [int]$Line, [string]$Kind, [string]$Value)
    foreach ($term in $request.terms) {
        if ($Value.IndexOf($term, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $violations.Add("${Path}:${Line}: $Kind contains '$term'; name and explain the responsibility (STD-CODE).")
        }
    }
}
function Get-AssignedNames {
    param($Target)
    if ($Target -is [Management.Automation.Language.VariableExpressionAst]) {
        $Target.VariablePath.UserPath
    } elseif ($Target -is [Management.Automation.Language.AttributedExpressionAst]) {
        Get-AssignedNames $Target.Child
    } elseif ($Target -is [Management.Automation.Language.ArrayLiteralAst]) {
        foreach ($element in $Target.Elements) { Get-AssignedNames $element }
    }
}
foreach ($path in $request.files) {
    $tokens = $null
    $parseErrors = $null
    $tree = [Management.Automation.Language.Parser]::ParseFile((Join-Path $request.root $path), [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count) { throw ($parseErrors | ForEach-Object { "${path}: $($_.Message)" } | Out-String) }
    foreach ($token in $tokens) {
        if ($token.Kind -eq [Management.Automation.Language.TokenKind]::Comment) {
            Add-SourceViolation $path $token.Extent.StartLineNumber 'comment' $token.Text
        }
    }
    foreach ($node in $tree.FindAll({ param($item) $true }, $true)) {
        $names = @()
        if ($node -is [Management.Automation.Language.FunctionDefinitionAst] -or
            $node -is [Management.Automation.Language.TypeDefinitionAst] -or
            $node -is [Management.Automation.Language.PropertyMemberAst] -or
            $node -is [Management.Automation.Language.FunctionMemberAst]) {
            $names += $node.Name
        } elseif ($node -is [Management.Automation.Language.ParameterAst]) {
            $names += $node.Name.VariablePath.UserPath
        } elseif ($node -is [Management.Automation.Language.ForEachStatementAst]) {
            $names += $node.Variable.VariablePath.UserPath
        } elseif ($node -is [Management.Automation.Language.AssignmentStatementAst]) {
            $names += Get-AssignedNames $node.Left
        }
        foreach ($name in $names) { Add-SourceViolation $path $node.Extent.StartLineNumber 'declaration' $name }
    }
}
ConvertTo-Json -InputObject @($violations) -Compress
'''


def inspect(value, path, line, kind, terms, errors):
    for term in terms:
        if term.casefold() in value.casefold():
            errors.append(f"{path}:{line}: {kind} contains '{term}'; name and explain the responsibility (STD-CODE).")


def python_names(node):
    if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
        return [node.name]
    if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Store):
        return [node.id]
    if isinstance(node, ast.Attribute) and isinstance(node.ctx, ast.Store):
        if isinstance(node.value, ast.Name) and node.value.id in {"self", "cls"}:
            return [node.attr]
    if isinstance(node, ast.arg):
        return [node.arg]
    if isinstance(node, ast.alias) and node.asname:
        return [node.asname]
    if isinstance(node, (ast.ExceptHandler, ast.MatchAs, ast.MatchStar)) and node.name:
        return [node.name]
    if isinstance(node, ast.MatchMapping) and node.rest:
        return [node.rest]
    return []


def inspect_python(text, path, terms, errors):
    tree = ast.parse(text, filename=path)
    for node in ast.walk(tree):
        for name in python_names(node):
            inspect(name, path, node.lineno, "declaration", terms, errors)
        if isinstance(node, (ast.Module, ast.ClassDef, ast.FunctionDef, ast.AsyncFunctionDef)):
            if node.body and isinstance(node.body[0], ast.Expr):
                value = node.body[0].value
                if isinstance(value, ast.Constant) and isinstance(value.value, str):
                    inspect(value.value, path, value.lineno, "documentation", terms, errors)
    for token in tokenize.generate_tokens(io.StringIO(text).readline):
        if token.type == tokenize.COMMENT:
            inspect(token.string, path, token.start[0], "comment", terms, errors)


def inspect_powershell(root, files, terms, errors):
    if not files:
        return
    executable = shutil.which("powershell")
    if executable is None:
        raise FileNotFoundError("PowerShell host not found on PATH: powershell")
    command = [executable, "-NoProfile", "-NonInteractive", "-Command", PS_SOURCE]
    result = subprocess.run(command, input=json.dumps({"root": str(root), "files": files, "terms": terms}),
                            text=True, encoding="utf-8", stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
    errors.extend(json.loads(result.stdout))


def walk(node):
    yield node
    for child in node.children:
        yield from walk(child)


def bindings(node):
    if node is None:
        return []
    if node.type in {"identifier", "shorthand_property_identifier_pattern"}:
        return [node]
    if node.type in {"assignment_pattern", "object_assignment_pattern"}:
        return bindings(node.child_by_field_name("left"))
    if node.type == "pair_pattern":
        return bindings(node.child_by_field_name("value"))
    if node.type in {"formal_parameters", "array_pattern", "object_pattern", "rest_pattern"}:
        return [name for child in node.named_children for name in bindings(child)]
    return []


def javascript_names(node):
    if node.type in {"variable_declarator", "function_declaration", "function_expression", "generator_function",
                     "generator_function_declaration", "class_declaration", "class"}:
        name = node.child_by_field_name("name")
        return bindings(name) if node.type == "variable_declarator" else [name] if name else []
    if node.type in {"formal_parameters", "catch_clause"}:
        return bindings(node if node.type == "formal_parameters" else node.child_by_field_name("parameter"))
    if node.type == "arrow_function":
        return bindings(node.child_by_field_name("parameter"))
    if node.type in {"pair", "method_definition", "field_definition"}:
        name = node.child_by_field_name("key") if node.type == "pair" else node.child_by_field_name("name")
        if node.type == "field_definition":
            name = node.child_by_field_name("property") or name
        return [name] if name and name.type in {"property_identifier", "private_property_identifier", "identifier"} else []
    if node.type in {"import_specifier", "export_specifier"}:
        name = node.child_by_field_name("alias")
        return [name] if name else []
    if node.type in {"namespace_import", "import_clause"}:
        return [child for child in node.named_children if child.type == "identifier"]
    return []


def inspect_batch(text, path, terms, errors):
    for line, value in enumerate(text.splitlines(), 1):
        if re.match(r"^\s*@?(?:rem(?:\s|$)|::)", value, re.IGNORECASE):
            inspect(value, path, line, "comment", terms, errors)
            continue
        name = re.match(r'^\s*@?set\s+(?:/[ap]\s+)?"?([^=\s"]+)\s*=', value, re.IGNORECASE)
        label = re.match(r"^\s*:([^:\s]+)", value)
        for declaration in (name, label):
            if declaration:
                inspect(declaration[1], path, line, "declaration", terms, errors)


def gdscript_group(tokens, start):
    depth = 0
    for index in range(start, len(tokens)):
        value = tokens[index].string
        if value in {"(", "[", "{"}:
            depth += 1
        elif value in {")", "]", "}"}:
            depth -= 1
            if depth == 0:
                return index
    raise ValueError(f"GDScript declaration group at line {tokens[start].start[0]} is not closed")


def gdscript_parameters(tokens, start):
    end = gdscript_group(tokens, start)
    binding = True
    index = start + 1
    while index < end:
        token = tokens[index]
        if token.type == tokenize.NEWLINE:
            index += 1
            continue
        if binding and token.type == tokenize.NAME:
            yield token
        binding = token.string == ","
        index = gdscript_group(tokens, index) + 1 if token.string in {"(", "[", "{"} else index + 1


def gdscript_names(tokens):
    index = 0
    while index < len(tokens):
        token = tokens[index]
        value = token.string
        if value == "@":
            index += 2
            if index < len(tokens) and tokens[index].string == "(":
                index = gdscript_group(tokens, index) + 1
            continue
        following = index + 1
        if token.type != tokenize.NAME or index and tokens[index - 1].string == ".":
            index += 1
            continue
        if value in {"func", "signal", "var", "const", "class_name", "class", "enum", "for"}:
            if following < len(tokens) and tokens[following].type == tokenize.NAME:
                yield tokens[following]
                following += 1
        if value in {"func", "signal", "enum"} and following < len(tokens):
            if tokens[following].string == ("{" if value == "enum" else "("):
                yield from gdscript_parameters(tokens, following)
        if value == "set" and (index == 0 or tokens[index - 1].type == tokenize.NEWLINE):
            if following < len(tokens) and tokens[following].string == "(":
                end = gdscript_group(tokens, following)
                if end + 1 < len(tokens) and tokens[end + 1].string == ":":
                    yield from gdscript_parameters(tokens, following)
        index += 1


def inspect_gdscript(text, path, terms, errors):
    # Godot shares Python's quoted-string and # comment boundaries; declaration heads differ.
    tokens = list(tokenize.generate_tokens(io.StringIO(text).readline))
    for token in tokens:
        if token.type == tokenize.COMMENT:
            inspect(token.string, path, token.start[0], "comment", terms, errors)
        if token.type == tokenize.ERRORTOKEN and token.string in {"'", '"'}:
            raise ValueError(f"{path}:{token.start[0]}: GDScript string is not closed")
    code = [token for token in tokens if token.type not in
            {tokenize.COMMENT, tokenize.NL, tokenize.INDENT, tokenize.DEDENT, tokenize.ENDMARKER}]
    for token in gdscript_names(code):
        inspect(token.string, path, token.start[0], "declaration", terms, errors)


def inspect_other(text, path, terms, errors):
    suffix = Path(path).suffix.lower()
    if suffix in {".cmd", ".bat"}:
        inspect_batch(text, path, terms, errors)
        return
    if suffix not in {".js", ".mjs", ".cjs", ".jsx"}:
        raise ValueError(f"{path}: source extension '{suffix}' is unsupported; integrate its parser before use")
    source = text.encode("utf-8")
    parser = Parser(Language(javascript.language()))
    tree = parser.parse(source)
    if tree.root_node.has_error:
        first = next(node for node in walk(tree.root_node) if node.type == "ERROR" or node.is_missing)
        raise ValueError(f"{path}:{first.start_point.row + 1}: JavaScript parser could not read the source")
    for node in walk(tree.root_node):
        if node.type == "comment":
            inspect(source[node.start_byte:node.end_byte].decode("utf-8"), path,
                    node.start_point.row + 1, "comment", terms, errors)
        for name in javascript_names(node):
            inspect(source[name.start_byte:name.end_byte].decode("utf-8"), path,
                    name.start_point.row + 1, "declaration", terms, errors)


def read_source(root, path):
    data = (root / path).read_bytes()
    try:
        return data.decode("utf-8-sig")
    except UnicodeDecodeError:
        if Path(path).suffix.lower() in {".cmd", ".bat"}:
            return data.decode("cp936")
        raise


def main():
    request = json.load(sys.stdin)
    root, terms, errors = Path(request["root"]), request["terms"], []
    powershell = []
    for path in request["files"]:
        suffix = Path(path).suffix.lower()
        if suffix in {".ps1", ".psm1"}:
            powershell.append(path)
            continue
        text = read_source(root, path)
        if suffix == ".py":
            inspect_python(text, path, terms, errors)
        elif suffix == ".gd":
            inspect_gdscript(text, path, terms, errors)
        else:
            inspect_other(text, path, terms, errors)
    inspect_powershell(root, powershell, terms, errors)
    json.dump(errors, sys.stdout, ensure_ascii=False)


if __name__ == "__main__":
    main()

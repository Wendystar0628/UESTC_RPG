"""下载并安装固定版本的官方 gdUnit4 插件，校验通过才装，不编译也不导入工程。

用法：python scripts/setup/install-godot-test-tools.py
压缩包与校验记录留在被忽略的 output/downloads 目录。
产出：output/downloads/；game/addons/gdUnit4/。
"""

import hashlib
import json
from pathlib import Path, PurePosixPath
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DOWNLOADS = ROOT / "output" / "downloads"
PLUGIN_TAG = "v6.2.2"
PLUGIN_COMMIT = "0127a55f4543dfc24746f6e5c7ba6f94105d51f2"
PLUGIN_ARCHIVE_SHA256 = "ddb44730ffb9cd77fd70d283753450759b00c5016143d7572bf63625f64d5433"
PLUGIN_REPO = "https://github.com/godot-gdunit-labs/gdUnit4"


def fetch(url, path):
    if path.exists():
        return
    partial = path.with_suffix(path.suffix + ".partial")
    request = urllib.request.Request(url, headers={"User-Agent": "UESTC-RPG-D0-setup"})
    with urllib.request.urlopen(request, timeout=120) as source, partial.open("wb") as target:
        while chunk := source.read(4 * 1024 * 1024):
            target.write(chunk)
    partial.replace(path)


def digest(path, algorithm="sha256"):
    h = hashlib.new(algorithm)
    with path.open("rb") as stream:
        while chunk := stream.read(4 * 1024 * 1024):
            h.update(chunk)
    return h.hexdigest()


def safe_member(member):
    p = PurePosixPath(member)
    if p.is_absolute() or ".." in p.parts or "\\" in member or ":" in member:
        raise ValueError(f"Unsafe archive path: {member}")
    return p


def install_file(archive, info, destination):
    payload = archive.read(info)  # CRC is checked by ZipFile.
    if destination.exists():
        if destination.read_bytes() != payload:
            raise FileExistsError(f"Refusing to overwrite different installed file: {destination}")
    else:
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(payload)
    return {"path": str(destination), "bytes": len(payload), "sha256": hashlib.sha256(payload).hexdigest()}


def install_plugin():
    url = f"https://codeload.github.com/godot-gdunit-labs/gdUnit4/zip/refs/tags/{PLUGIN_TAG}"
    archive_path = DOWNLOADS / f"gdUnit4-{PLUGIN_TAG}.zip"
    fetch(url, archive_path)
    if digest(archive_path) != PLUGIN_ARCHIVE_SHA256:
        raise ValueError("Pinned official gdUnit4 tag archive checksum mismatch")
    tag_url = f"https://api.github.com/repos/godot-gdunit-labs/gdUnit4/git/ref/tags/{PLUGIN_TAG}"
    with urllib.request.urlopen(tag_url, timeout=120) as response:
        tag = json.load(response)
    if tag["object"] != {"sha": PLUGIN_COMMIT, "type": "commit", "url": f"https://api.github.com/repos/godot-gdunit-labs/gdUnit4/git/commits/{PLUGIN_COMMIT}"}:
        raise ValueError("Pinned official gdUnit4 tag commit mismatch")
    target = ROOT / "game" / "addons" / "gdUnit4"
    files = []
    with zipfile.ZipFile(archive_path) as archive:
        if archive.testzip() is not None:
            raise ValueError("gdUnit4 archive CRC verification failed")
        root = archive.namelist()[0].split("/")[0] + "/"
        prefix = root + "addons/gdUnit4/"
        for info in archive.infolist():
            safe_member(info.filename)
            if info.filename.startswith(prefix) and not info.is_dir():
                files.append(install_file(archive, info, target / info.filename[len(prefix):]))
        if not (target / "LICENSE").exists():
            files.append(install_file(archive, archive.getinfo(root + "LICENSE"), target / "LICENSE"))
    return {"name": "gdUnit4", "version": PLUGIN_TAG.removeprefix("v"), "commit": PLUGIN_COMMIT, "source": url, "release": f"{PLUGIN_REPO}/releases/tag/{PLUGIN_TAG}", "archive": str(archive_path), "bytes": archive_path.stat().st_size, "sha256": digest(archive_path), "sha512": digest(archive_path, "sha512"), "installation": str(target), "installedFileCount": len(files), "installedBytes": sum(f["bytes"] for f in files), "installedFiles": files}


def main():
    DOWNLOADS.mkdir(parents=True, exist_ok=True)
    plugin = install_plugin()
    (DOWNLOADS / "gdunit4-installation.json").write_text(json.dumps(plugin, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"Installed gdUnit4 {plugin['version']}: {plugin['installedFileCount']} files", flush=True)
    print(json.dumps({k: v for k, v in plugin.items() if k != "installedFiles"}, indent=2), flush=True)


if __name__ == "__main__":
    main()

"""Fetch pinned official D0 tools without building or importing the project.

Run with Python 3: python scripts/setup/install_d0_tools.py
Archives and verification evidence remain in the ignored output/downloads directory.
"""

import hashlib
import json
from pathlib import Path, PurePosixPath
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DOWNLOADS = ROOT / "output" / "downloads"
GODOT_HOME = Path(r"E:\Tools\Godot\Godot_v4.7.2-stable_mono_win64")
TEMPLATE_VERSION = "4.7.2.stable.mono"
GODOT_RELEASE = "https://api.github.com/repos/godotengine/godot-builds/releases/tags/4.7.2-stable"
PLUGIN_TAG = "v6.2.2"
PLUGIN_COMMIT = "0127a55f4543dfc24746f6e5c7ba6f94105d51f2"
PLUGIN_ARCHIVE_SHA256 = "ddb44730ffb9cd77fd70d283753450759b00c5016143d7572bf63625f64d5433"
PLUGIN_REPO = "https://github.com/godot-gdunit-labs/gdUnit4"


def fetch(url, path, expected_size=None):
    if path.exists() and (expected_size is None or path.stat().st_size == expected_size):
        return
    partial = path.with_suffix(path.suffix + ".partial")
    request = urllib.request.Request(url, headers={"User-Agent": "UESTC-RPG-D0-setup"})
    with urllib.request.urlopen(request, timeout=120) as source, partial.open("wb") as target:
        while chunk := source.read(4 * 1024 * 1024):
            target.write(chunk)
    if expected_size is not None and partial.stat().st_size != expected_size:
        raise ValueError(f"Unexpected download size: {partial}")
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


def install_templates():
    release_file = DOWNLOADS / "godot-4.7.2-release.json"
    fetch(GODOT_RELEASE, release_file)
    release = json.loads(release_file.read_text(encoding="utf-8"))
    assets = {a["name"]: a for a in release["assets"]}
    name = "Godot_v4.7.2-stable_mono_export_templates.tpz"
    archive_path = DOWNLOADS / name
    sums_path = DOWNLOADS / "Godot-4.7.2-SHA512-SUMS.txt"
    fetch(assets["SHA512-SUMS.txt"]["browser_download_url"], sums_path, assets["SHA512-SUMS.txt"]["size"])
    expected = next(line.split()[0] for line in sums_path.read_text(encoding="utf-8").splitlines() if line.split()[-1].lstrip("*").split("/")[-1] == name)
    print(f"Downloading {name} ({assets[name]['size']} bytes)", flush=True)
    fetch(assets[name]["browser_download_url"], archive_path, assets[name]["size"])
    actual = digest(archive_path, "sha512")
    if actual != expected:
        raise ValueError("Official SHA-512 verification failed")
    actual_sha256 = digest(archive_path)
    if assets[name].get("digest") not in (None, "sha256:" + actual_sha256):
        raise ValueError("GitHub release SHA-256 verification failed")
    target = GODOT_HOME / "editor_data" / "export_templates" / TEMPLATE_VERSION
    files = []
    with zipfile.ZipFile(archive_path) as archive:
        names = archive.namelist()
        for name_in_zip in names:
            safe_member(name_in_zip)
        version_name = next(n for n in names if n.endswith("/version.txt"))
        version = archive.read(version_name).decode("utf-8").strip()
        if version != TEMPLATE_VERSION:
            raise ValueError(f"Unexpected template version: {version}")
        prefix = version_name.rsplit("/", 1)[0] + "/"
        for info in archive.infolist():
            relative = info.filename.removeprefix(prefix)
            if info.is_dir() or not info.filename.startswith(prefix):
                continue
            # Install the exact Windows x64 debug/release payload and its dependencies.
            if relative == "version.txt" or "windows" in relative.lower() and ("x86_64" in relative.lower() or "x64" in relative.lower()):
                files.append(install_file(archive, info, target / relative))
        if not any("windows_release_x86_64.exe" in f["path"] for f in files):
            raise ValueError("Windows x64 release template missing from archive selection")
    return {"name": archive_path.name, "version": TEMPLATE_VERSION, "source": assets[name]["browser_download_url"], "release": release["html_url"], "archive": str(archive_path), "bytes": archive_path.stat().st_size, "sha512": actual, "officialSha512Verified": True, "sha256": actual_sha256, "installation": str(target), "installedFiles": files, "scope": "Windows x64 only; full official archive retained"}


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
    templates = install_templates()
    (DOWNLOADS / "godot-template-installation.json").write_text(json.dumps(templates, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(json.dumps({"plugin": {k: v for k, v in plugin.items() if k != "installedFiles"}, "templates": {k: v for k, v in templates.items() if k != "installedFiles"}}, indent=2), flush=True)


if __name__ == "__main__":
    main()

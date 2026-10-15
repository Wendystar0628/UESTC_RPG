"""Refresh third_party.json from pinned packages and verified D0 tool receipts.

Downloads package archives for license/provenance inspection only. Does not run
restore, compile, import, install packages into a project, or execute package code.
Both packages.lock.json and project-specific *.packages.lock.json files are read.
Manual entries (including fonts supplied by another package) are preserved.
Recorded validation is preserved for unchanged artifacts; refreshing package
metadata does not run validation or promote a historical result to a new run.
"""

import hashlib
import json
from pathlib import Path
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DOWNLOADS = ROOT / "output" / "downloads"
ALLOWED = {"MIT", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "ISC", "OFL-1.1"}


def package_entry(name, version, scopes):
    filename = f"{name}.{version}.nupkg"
    archive = DOWNLOADS / filename
    url = f"https://api.nuget.org/v3-flatcontainer/{name.lower()}/{version.lower()}/{name.lower()}.{version.lower()}.nupkg"
    if not archive.exists():
        with urllib.request.urlopen(url, timeout=120) as response:
            archive.write_bytes(response.read())
    with zipfile.ZipFile(archive) as content:
        bad = content.testzip()
        if bad:
            raise ValueError(f"Corrupt package member: {name}/{bad}")
        nuspec_name = next(n for n in content.namelist() if n.endswith(".nuspec"))
        metadata = ET.fromstring(content.read(nuspec_name)).find("{*}metadata")
        license_node = metadata.find("{*}license")
        if license_node is None:
            raise ValueError(f"Missing SPDX or bundled license: {name}")
        license_evidence = license_node.text
        if license_node.attrib.get("type") == "expression":
            license_id = license_node.text
        else:
            text = content.read(license_node.text).decode("utf-8-sig")
            if "MIT License" in text or "Permission is hereby granted, free of charge" in text:
                license_id = "MIT"
            elif "Apache License" in text and "Version 2.0" in text:
                license_id = "Apache-2.0"
            else:
                raise ValueError(f"Unrecognized bundled license: {name}")
        if license_id not in ALLOWED:
            raise ValueError(f"License outside project allowlist: {name} {license_id}")
        repository = metadata.find("{*}repository")
        repo = repository.attrib if repository is not None else {}
    payload = archive.read_bytes()
    return {
        "name": name, "version": version, "kind": "nuget", "scopes": sorted(scopes),
        "commit": repo.get("commit"), "commitStatus": "NUSPEC_DECLARED" if repo.get("commit") else "NOT_DECLARED_UPSTREAM",
        "license": license_id, "licenseEvidence": {"archive": "output/downloads/" + filename, "nuspec": nuspec_name, "declaration": license_evidence},
        "source": url, "repository": repo.get("url"), "sha256": hashlib.sha256(payload).hexdigest(),
        "sha512": hashlib.sha512(payload).hexdigest(), "hashStatus": "COMPUTED_FROM_OFFICIAL_NUGET_ARCHIVE", "hashScope": "Complete signed nupkg archive bytes; differs from NuGet lockfile contentHash, which excludes the signature context", "bytes": len(payload),
    }


def main():
    DOWNLOADS.mkdir(parents=True, exist_ok=True)
    manifest_path = ROOT / "third_party.json"
    existing = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.exists() else {}
    manual_entries = [entry for entry in existing.get("entries", [])
                      if entry.get("managedBy") == "manual" or entry.get("kind") == "font"]
    versions = {}
    def register(name, version, scope):
        versions.setdefault((name, version), set()).add(scope)
    props = ET.parse(ROOT / "Directory.Packages.props")
    for node in props.findall(".//PackageVersion"):
        register(node.attrib["Include"], node.attrib["Version"], "central-version")
    register("Godot.NET.Sdk", "4.7.2", "project-sdk")
    lock_files = []
    lock_paths = set(ROOT.rglob("packages.lock.json")) | set(ROOT.rglob("*.packages.lock.json"))
    for lock_path in sorted(lock_paths):
        if any(part in {"output", "bin", "obj", ".godot"} for part in lock_path.parts):
            continue
        relative = lock_path.relative_to(ROOT).as_posix()
        lock_files.append(relative)
        lock = json.loads(lock_path.read_text(encoding="utf-8-sig"))
        for framework in lock.get("dependencies", {}).values():
            for name, node in framework.items():
                if node.get("type", "").lower() != "project" and node.get("resolved"):
                    register(name, node["resolved"], relative)
    entries = [package_entry(name, version, scopes) for (name, version), scopes in sorted(versions.items())]
    plugin = json.loads((DOWNLOADS / "gdunit4-installation.json").read_text(encoding="utf-8"))
    entries.append({
        "name": "gdUnit4", "version": plugin["version"], "commit": plugin["commit"], "kind": "godot-editor-plugin", "license": "MIT",
        "source": plugin["source"], "release": plugin["release"], "sha256": plugin["sha256"], "sha512": plugin["sha512"], "hashStatus": "COMPUTED_FROM_OFFICIAL_TAG_ARCHIVE",
        "bytes": plugin["bytes"], "path": "game/addons/gdUnit4", "licenseFile": "game/addons/gdUnit4/LICENSE", "modified": False,
        "installedFileCount": plugin["installedFileCount"], "installedBytes": plugin["installedBytes"],
        "languagePolicy": "Vendored upstream GDScript and C# bridge are third-party plugin code, not project-authored gameplay code.",
        "compatibility": {"targetGodot": "4.7.2.stable.mono", "upstreamReadmeVersions": ["4.5", "4.5.1", "4.5.2", "4.6", "4.6.1", "4.6.2", "4.6.3", "4.7", "4.7.1"], "readmeEvidence": "https://github.com/godot-gdunit-labs/gdUnit4/blob/v6.2.2/README.md", "loaderMinimumHex": "0x40200", "loaderEvidence": "game/addons/gdUnit4/src/dotnet/GdUnit4CSharpApiLoader.gd", "patchCompatibility": "4.7 family declared; 4.7.2 is not individually enumerated in the upstream README", "runtimeValidation": "NOT_RUN", "dotnetApi": "5.1.0-rc5", "dotnetAdapter": "3.1.1"}
    })
    templates = json.loads((DOWNLOADS / "godot-template-installation.json").read_text(encoding="utf-8"))
    entries.append({
        "name": "Godot .NET Windows x64 export templates", "version": "4.7.2-stable", "kind": "external-build-tool", "commit": "ed1daf0bf001b61586d9930840f2f1394092c079", "license": "MIT", "licenseUrl": "https://github.com/godotengine/godot/blob/4.7.2-stable/LICENSE.txt", "thirdPartyNoticesUrl": "https://github.com/godotengine/godot/blob/4.7.2-stable/COPYRIGHT.txt", "source": templates["source"], "release": templates["release"], "bytes": templates["bytes"], "sha256": templates["sha256"], "sha512": templates["sha512"], "hashStatus": "VERIFIED_OFFICIAL_SHA512_AND_RELEASE_SHA256", "archive": "output/downloads/" + templates["name"], "installation": templates["installation"], "scope": templates["scope"], "installedFiles": templates["installedFiles"], "runtimeValidation": "NOT_RUN"
    })
    generated_keys = {(entry["name"], entry["version"]) for entry in entries}
    previous_entries = {(entry["name"], entry["version"]): entry for entry in existing.get("entries", [])}
    for entry in entries:
        previous = previous_entries.get((entry["name"], entry["version"]))
        if previous is None or previous.get("sha256") != entry.get("sha256"):
            continue
        for field in ("runtimeValidation", "validation", "validationEvidence"):
            if field in previous:
                entry[field] = previous[field]
        if "compatibility" in entry and "runtimeValidation" in previous.get("compatibility", {}):
            entry["compatibility"]["runtimeValidation"] = previous["compatibility"]["runtimeValidation"]
    for entry in manual_entries:
        if (entry["name"], entry["version"]) in generated_keys:
            raise ValueError(f"Manual entry conflicts with generated package: {entry['name']}")
        if entry["license"] not in ALLOWED:
            raise ValueError(f"Manual entry license outside allowlist: {entry['name']}")
    entries.extend(manual_entries)
    result = {"version": 1, "allowedLicenses": sorted(ALLOWED), "entries": entries,
        "nugetClosure": {"lockFiles": sorted(lock_files), "lockFilePatterns": ["packages.lock.json", "*.packages.lock.json"], "status": "LOCKFILES_INCLUDED" if lock_files else "PENDING_RESTORE_LOCKFILES", "policy": "Read every lockfile independently without modifying it. Run this script after restoring all projects to include every resolved transitive package; missing lockfiles are not license validation success."},
        "audio": {"manifest": "game/data/audio/licenses.json", "owner": "WP-53e", "assetsDownloadedInD0": False},
        "validation": existing.get("validation", {"build": "NOT_RUN", "godotImport": "NOT_RUN", "export": "NOT_RUN"})}
    (ROOT / "third_party.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"Recorded {len(entries)} entries; NuGet lockfiles: {len(lock_files)}")


if __name__ == "__main__":
    main()

"""Refresh NuGet sources in third_party.json from existing restore assets.

Reads NuGet libraries from obj/**/project.assets.json without running restore or
recreating lockfiles. Existing package metadata and non-NuGet entries are preserved.
New package metadata requires a local nupkg archive; nothing is downloaded.
Manual entries (including fonts supplied by another package) are preserved.
Python wheel inventory may retain explicit pending license reviews; that is not
license approval. The certifi exception is restricted to its approved artifact.
Recorded validation is preserved for unchanged artifacts; refreshing package
metadata does not run validation or promote a historical result to a new run.
"""

import hashlib
import json
from pathlib import Path
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
        raise FileNotFoundError(f"Package archive not available locally: {archive}")
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
        "sha512": hashlib.sha512(payload).hexdigest(), "hashStatus": "COMPUTED_FROM_OFFICIAL_NUGET_ARCHIVE", "hashScope": "Complete signed nupkg archive bytes", "bytes": len(payload),
    }


def validate_manual_license(entry):
    if entry["license"] in ALLOWED:
        return
    development_wheel = (
        entry.get("kind") == "python-wheel"
        and entry.get("provider") == "WP-08"
        and entry.get("developmentOnly") is True
        and entry.get("exported") is False
        and entry.get("vendored") is False
    )
    review = entry.get("licenseReview", {})
    if development_wheel and review.get("status") == "PENDING_POLICY_REVIEW":
        return  # Inventory only; the unapproved license remains visible.
    certifi_exception = (
        entry["name"] == "certifi" and entry["version"] == "2026.7.22"
        and entry["license"] == "MPL-2.0"
        and entry.get("sha256") == "62f22742b58a1a33014a2b6b706588a8d7e2a88ae7bd1a6ebe8c992928483775"
        and review.get("status") == "APPROVED_EXCEPTION"
        and review.get("approvedBy") == "Opus"
    )
    if development_wheel and certifi_exception:
        return
    raise ValueError(f"Manual entry license outside allowlist: {entry['name']}")


def main():
    manifest_path = ROOT / "third_party.json"
    existing = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.exists() else {}
    previous_entries = {(entry["name"], entry["version"]): entry
                        for entry in existing.get("entries", [])}
    retained_entries = [entry for entry in existing.get("entries", [])
                        if entry.get("kind") != "nuget" or entry.get("managedBy") == "manual"]
    versions = {}
    def register(name, version, scope):
        versions.setdefault((name, version), set()).add(scope)
    props = ET.parse(ROOT / "Directory.Packages.props")
    for node in props.findall(".//PackageVersion"):
        register(node.attrib["Include"], node.attrib["Version"], "central-version")
    register("Godot.NET.Sdk", "4.7.2", "project-sdk")
    assets_files = []
    for assets_path in sorted(ROOT.rglob("project.assets.json")):
        parts = assets_path.relative_to(ROOT).parts
        if "obj" not in parts or any(part in {"output", "bin"} for part in parts):
            continue
        assets = json.loads(assets_path.read_text(encoding="utf-8-sig"))
        project_path = (assets.get("project", {}).get("restore", {}).get("projectPath")
                        or assets.get("restore", {}).get("projectPath"))
        if not project_path or not (ROOT / project_path).is_file():
            continue
        relative = assets_path.relative_to(ROOT).as_posix()
        assets_files.append(relative)
        for library, node in assets.get("libraries", {}).items():
            if node.get("type") == "package":
                name, version = library.rsplit("/", 1)
                register(name, version, relative)
    if not assets_files:
        raise ValueError("No restore assets found; restore projects before refreshing the manifest")
    missing = [key for key, entry in previous_entries.items()
               if entry.get("kind") == "nuget" and key not in versions
               and entry["name"] != "Microsoft.CodeAnalysis.PublicApiAnalyzers"]
    if missing:
        raise ValueError(f"Restore inputs do not cover registered NuGet packages: {missing}")
    entries = []
    for (name, version), scopes in sorted(versions.items()):
        previous = previous_entries.get((name, version))
        if previous is not None:
            if previous in retained_entries:
                raise ValueError(f"Manual entry conflicts with generated package: {name}")
            entry = {**previous, "scopes": sorted(scopes)}
        else:
            entry = package_entry(name, version, scopes)
        entries.append(entry)
    for entry in retained_entries:
        if entry.get("managedBy") == "manual" or entry.get("kind") == "font":
            validate_manual_license(entry)
    entries.extend(retained_entries)
    closure = dict(existing.get("nugetClosure", {}))
    for field in ("lockFiles", "lockFilePatterns", "status"):
        closure.pop(field, None)
    closure.update({
        "assetsFiles": assets_files,
        "policy": "Read NuGet package libraries from existing obj/**/project.assets.json without modifying restore outputs. Run after restoring all projects; this inventory does not grant license approval."
    })
    result = {**existing, "entries": entries, "nugetClosure": closure}
    (ROOT / "third_party.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"Recorded {len(entries)} entries; NuGet restore assets: {len(assets_files)}")


if __name__ == "__main__":
    main()

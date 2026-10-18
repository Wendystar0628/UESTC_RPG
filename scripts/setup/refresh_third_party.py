"""Refresh the NuGet inventory with the .NET CLI and nuget-license.

Run dotnet tool restore and restore the projects first. This script never restores
or builds projects. Non-NuGet entries and their existing license reviews remain
manual inventory; refreshing them does not approve pending reviews.
"""

import json
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
INPUTS = (
    ROOT / "game/Campus.sln",
    ROOT / "scripts/gates/arch/Campus.Arch.Tests.csproj",
    ROOT / "scripts/godot-api/ApiDump/ApiDump.csproj",
)


def run_json(*arguments):
    result = subprocess.run(arguments, cwd=ROOT, capture_output=True,
                            encoding="utf-8-sig", check=False)
    if result.returncode:
        raise RuntimeError(f"{' '.join(map(str, arguments))}\n"
                           f"{result.stderr or result.stdout}")
    return json.loads(result.stdout)


def package_scopes():
    packages = {}
    projects = set()
    for target in INPUTS:
        listing = run_json("dotnet", "list", str(target), "package",
                           "--include-transitive", "--format", "json",
                           "--output-version", "1", "--no-restore")
        if listing.get("problems"):
            raise ValueError(f"Package listing failed: {listing['problems']}")
        for project in listing["projects"]:
            scope = Path(project["path"]).resolve().relative_to(ROOT).as_posix()
            projects.add(scope)
            for framework in project.get("frameworks", []):
                for field in ("topLevelPackages", "transitivePackages"):
                    for package in framework.get(field, []):
                        key = (package["id"].casefold(), package["resolvedVersion"])
                        packages.setdefault(key, set()).add(scope)
    return packages, sorted(projects)


def validate_manual_license(entry, allowed):
    if entry["license"] in allowed:
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
    existing = json.loads(manifest_path.read_text(encoding="utf-8"))
    allowed = existing["allowedLicenses"]
    scopes, projects = package_scopes()
    with tempfile.TemporaryDirectory(prefix="uestc-licenses-") as temporary:
        inputs = Path(temporary) / "projects.json"
        inputs.write_text(json.dumps([str(ROOT / project) for project in projects]),
                          encoding="utf-8")
        licenses = run_json("dotnet", "tool", "run", "nuget-license", "--",
                            "--json-input", str(inputs), "--include-transitive",
                            "--allowed-license-types", ";".join(allowed),
                            "--output", "Json")
    entries = []
    found = set()
    for package in licenses:
        key = (package["PackageId"].casefold(), package["PackageVersion"])
        if package.get("ValidationErrors"):
            raise ValueError(f"License validation failed: {package}")
        if key not in scopes:
            raise ValueError(f"License result absent from dotnet package list: {key}")
        if key in found:
            continue  # nuget-license can return the same package for several projects.
        found.add(key)
        entries.append({
            "name": package["PackageId"], "version": package["PackageVersion"],
            "kind": "nuget", "scopes": sorted(scopes[key]),
            "license": package["License"],
            "licenseUrl": package.get("LicenseUrl"),
            "licenseInformationOrigin": package["LicenseInformationOrigin"],
            "source": f"https://www.nuget.org/packages/{package['PackageId']}/{package['PackageVersion']}",
            "projectUrl": package.get("PackageProjectUrl"),
        })
    if missing := scopes.keys() - found:
        raise ValueError(f"Packages missing license results: {sorted(missing)}")
    entries.sort(key=lambda entry: (entry["name"].casefold(), entry["version"]))
    for entry in existing["entries"]:
        if entry["kind"] == "nuget":
            continue
        validate_manual_license(entry, allowed)
        entries.append(entry)
    result = {**existing, "version": 2, "entries": entries, "nugetClosure": {
        "inputs": [target.relative_to(ROOT).as_posix() for target in INPUTS],
        "projects": projects,
        "packageListing": "dotnet list package --include-transitive --format json --output-version 1 --no-restore",
        "licenseTool": "nuget-license",
        "policy": "Resolved packages and licenses from previously restored projects; SDKs and development tools are manual entries. Pending manual license reviews remain pending.",
    }}
    manifest_path.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n",
                             encoding="utf-8", newline="\n")
    print(f"Recorded {len(entries)} entries from {len(projects)} projects")


if __name__ == "__main__":
    main()

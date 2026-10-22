# 更新显示用的 Git 提交旁文件；版本、构建种类和模块名单由 MSBuild 原生生成。
# 用法：powershell scripts/build/build-info.ps1 -Root <仓库根>；由 game/Campus.csproj 调用。
# 产出：game/data/build-commit.txt；内容相等不改写。
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Root)
$ErrorActionPreference='Stop'
$rootPath=[IO.Path]::GetFullPath($Root)
. (Join-Path $rootPath 'scripts/lib/common.ps1')
$commit=(Invoke-UESTCGit $rootPath @('rev-parse','--short=12','HEAD')).StdOut.Trim()
if(@(Get-UESTCGitLines $rootPath @('status','--porcelain')).Count -gt 0){$commit+="-dirty"}
$commitFile=Join-Path $rootPath 'game/data/build-commit.txt'
$commitSource="$commit`n"
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($commitFile)) | Out-Null
if(-not(Test-Path -LiteralPath $commitFile) -or [IO.File]::ReadAllText($commitFile) -ne $commitSource){[IO.File]::WriteAllText($commitFile,$commitSource,[Text.UTF8Encoding]::new($false))}

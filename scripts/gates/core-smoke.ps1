# Existing G5 startup/exit and G20 core scopes run against the production main scene.
# The result is shared only inside this gate invocation, so G20 never repeats G5's window pair.
if (-not (Get-Variable UESTCCoreSmokeResults -Scope Script -ErrorAction SilentlyContinue)) {
    $script:UESTCCoreSmokeResults = @{}
}

function New-UESTCCoreCloseObserver {
    param([Parameter(Mandatory)][hashtable]$Observation)
    if (-not ('Uestc.Gates.CoreWindow' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace Uestc.Gates {
    public static class CoreWindow {
        [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr window, uint message, UIntPtr wParam, IntPtr lParam);
        public static bool PostClose(IntPtr window) { return PostMessage(window, 0x0010, UIntPtr.Zero, IntPtr.Zero); }
    }
}
'@
    }
    $callback = {
        param([string]$OutputText)
        if ($Observation.Sent) { return }
        $ready = [regex]::Match($OutputText,'(?m)^CORE_READY_CLOSE (\d+)\r?$')
        if (-not $ready.Success) { return }
        $Observation.Sent = $true
        $Observation.At = [Diagnostics.Stopwatch]::GetTimestamp()
        if (-not [Uestc.Gates.CoreWindow]::PostClose([IntPtr]([long]$ready.Groups[1].Value))) {
            throw 'Could not send WM_CLOSE to the ready production window.'
        }
    }
    return $callback.GetNewClosure()
}

function Invoke-UESTCMainlineSmoke {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][string]$Locks, [Parameter(Mandatory)][string]$Wp,
        [Parameter(Mandatory)][string]$Map, [IO.FileStream]$BuildLease = $null)
    $rootPath = [IO.Path]::GetFullPath($Root)
    $key = $rootPath + '|' + $Godot + '|' + $Map
    if ($script:UESTCCoreSmokeResults.ContainsKey($key)) { return $script:UESTCCoreSmokeResults[$key] }
    $tag = [Guid]::NewGuid().ToString('N').Substring(0,8)
    $work = Join-Path $rootPath ("output/build/gates/{0}-core-{1}" -f $Wp,$tag)
    $shots = Join-Path $rootPath ("output/render-review/{0}-core-{1}" -f $Wp,$tag)
    $saves = Join-Path $work 'saves'
    $null = [IO.Directory]::CreateDirectory($work)
    $null = [IO.Directory]::CreateDirectory($shots)
    $lease = $null
    try {
        if (-not $BuildLease) { $lease = Enter-UESTCBuildLock $Locks }
        $engineArgs = @('--path',(Join-Path $rootPath 'game'),'--rendering-method','forward_plus',
            '--rendering-driver','d3d12','--resolution','1280x720','--fixed-fps','60','--')
        $captureArgs = @("--save-dir=$saves","--out=$shots",'--shots=V1','--ui=0','--audio=off','--quality=fine','--adaptive=0')
        $closeObservation = @{ At=$null; Sent=$false }
        $closeObserver = New-UESTCCoreCloseObserver $closeObservation
        $close = Invoke-UESTCProcess $Godot ($engineArgs + $captureArgs + @('--gate=save-close','--title=1',
            '--save=1',"--map=$Map",'--flow=0','--seed=1','--weather-seed=1')) $rootPath (Join-Path $work 'save-close.log') -Visible -TimeoutSeconds 180 -ObserveOutput $closeObserver
        $closeEnded = [Diagnostics.Stopwatch]::GetTimestamp()
        if ($close.Output -match '(?im)(?:SCRIPT )?ERROR:|shader[^\r\n]*(?:compilation failed|compile error)') {
            throw 'The production close process reported an engine/shader error.'
        }
        foreach ($marker in @('CORE_TITLE_READY','CORE_INPUT_READY','CORE_ROUNDTRIP_READY')) {
            if (-not $close.StdOut.Contains($marker)) { throw "The production close process missed $marker." }
        }
        if ($null -eq $closeObservation.At) { throw 'The close process did not receive WM_CLOSE.' }
        # Same machine monotonic clock; includes the runner's final observation interval.
        $closeMs = ($closeEnded - [long]$closeObservation.At) * 1000.0 / [Diagnostics.Stopwatch]::Frequency
        if ($closeMs -lt 0 -or $closeMs -gt 2000) { throw "WM_CLOSE did not finish within 2 s ($closeMs ms)." }
        $baseline = [regex]::Match($close.StdOut,'(?m)^CORE_GENERATION (\d+)\r?$')
        if (-not $baseline.Success) { throw 'The close process did not expose its pre-exit generation.' }
        $generations = @(Get-ChildItem -LiteralPath (Join-Path $saves 's1') -Filter 'g*.sav' -File |
            ForEach-Object { if ($_.BaseName -match '^g(\d+)$') { [UInt64]$Matches[1] } })
        $latest = ($generations | Measure-Object -Maximum).Maximum
        if (-not $latest -or [UInt64]$latest -le [UInt64]$baseline.Groups[1].Value) {
            throw 'WM_CLOSE did not write a new save generation.'
        }
        $resumeObservation = @{ At=$null; Sent=$false }
        $resumeObserver = New-UESTCCoreCloseObserver $resumeObservation
        $resume = Invoke-UESTCProcess $Godot ($engineArgs + $captureArgs + @('--gate=save-resume','--title=0',
            '--slot=1','--save=off')) $rootPath (Join-Path $work 'save-resume.log') -Visible -TimeoutSeconds 180 -ObserveOutput $resumeObserver
        if ($resume.Output -match '(?im)(?:SCRIPT )?ERROR:|shader[^\r\n]*(?:compilation failed|compile error)') {
            throw 'The production resume process reported an engine/shader error.'
        }
        $loaded = [regex]::Match($resume.StdOut,'(?m)^CORE_GENERATION (\d+)\r?$')
        if (-not $loaded.Success -or [UInt64]$loaded.Groups[1].Value -ne [UInt64]$latest) {
            throw 'The second process did not validate and restore the newly written generation.'
        }
        $before = [regex]::Match($close.StdOut,'(?m)^CORE_STATE (.+)\r?$')
        $after = [regex]::Match($resume.StdOut,'(?m)^CORE_STATE (.+)\r?$')
        if (-not $before.Success -or -not $after.Success -or $before.Groups[1].Value.TrimEnd("`r") -cne $after.Groups[1].Value.TrimEnd("`r")) {
            throw 'The second process changed a persisted field.'
        }
        $firstImage = Join-Path $shots 'save-close.png'
        $secondImage = Join-Path $shots 'save-resume.png'
        if (-not (Test-Path -LiteralPath $firstImage -PathType Leaf) -or -not (Test-Path -LiteralPath $secondImage -PathType Leaf)) {
            throw 'The production lifecycle did not capture both screenshots.'
        }
        $result = [pscustomobject]@{ Status='Passed';
            Scope='G5 startup/exit automatic assertions; full UI/transit mainline remains centralized'; CloseMilliseconds=$closeMs;
            Generation=[UInt64]$latest; PersistedFields='Equal';
            Screenshot='Both captured for Opus visual review; no G7 pixel comparison or tolerance claim';
            Logs=$work; Screenshots=$shots }
    } catch {
        $result = [pscustomobject]@{ Status='Failed'; Scope='G5 startup/exit core'; Error=$_.Exception.Message; Logs=$work; Screenshots=$shots }
    } finally { if ($lease) { Exit-UESTCBuildLock $lease } }
    $script:UESTCCoreSmokeResults[$key] = $result
    return $result
}

function Invoke-UESTCSaveCore {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$Godot,
        [Parameter(Mandatory)][string]$Locks, [Parameter(Mandatory)][string]$Wp,
        [Parameter(Mandatory)][string]$Map, [switch]$SaveTestsAlreadyPassed, [IO.FileStream]$BuildLease = $null)
    $rootPath = [IO.Path]::GetFullPath($Root)
    $lease = $null
    try {
        if (-not $BuildLease) { $lease = Enter-UESTCBuildLock $Locks; $BuildLease = $lease }
        if (-not $SaveTestsAlreadyPassed) {
            $work = Join-Path $rootPath ('output/build/gates/' + $Wp + '-save-core')
            $null = [IO.Directory]::CreateDirectory($work)
            $settings = Join-Path $work 'save.runsettings'
            $engine = [Security.SecurityElement]::Escape($Godot)
            $escapedRoot = [Security.SecurityElement]::Escape($rootPath)
            $xml = '<RunSettings><RunConfiguration><MaxCpuCount>1</MaxCpuCount><TreatNoTestsAsError>true</TreatNoTestsAsError><EnvironmentVariables><GODOT_BIN>' + $engine + '</GODOT_BIN><UESTC_REPOSITORY>' + $escapedRoot + '</UESTC_REPOSITORY></EnvironmentVariables></RunConfiguration><GdUnit4><Parameters>--headless</Parameters><DisplayName>FullyQualifiedName</DisplayName><TolerateExitCodeAfterCompletion>false</TolerateExitCodeAfterCompletion></GdUnit4></RunSettings>'
            [IO.File]::WriteAllText($settings,$xml,(New-Object Text.UTF8Encoding $false))
            $null = Invoke-UESTCProcess dotnet @('test',(Join-Path $rootPath 'game/Campus.Tests.csproj'),'--no-build',
                '--no-restore','--settings',$settings,'--filter','FullyQualifiedName~Campus.Kernel.Save',
                '--logger','trx','--results-directory',$work) $rootPath (Join-Path $work 'save-core.log') -TimeoutSeconds 600
        }
        $window = Invoke-UESTCMainlineSmoke -Root $rootPath -Godot $Godot -Locks $Locks -Wp $Wp -Map $Map -BuildLease $BuildLease
        return [pscustomobject]@{ Status=$window.Status; Scope='G20 roundtrip, migration, corruption and exit-save core';
            SaveTests=$(if ($SaveTestsAlreadyPassed) { 'Covered by this invocation G2' } else { 'Existing save suites passed' }); Window=$window }
    } finally { Exit-UESTCBuildLock $lease }
}

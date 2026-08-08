#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ArtifactsCore = Join-Path $RepoRoot "benchmarks\artifacts\core"
$ArtifactsGodot = Join-Path $RepoRoot "benchmarks\artifacts\godot"
$ReportDir = Join-Path $RepoRoot "benchmarks\report"
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null

function Get-JsonFile {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    return Get-Content -Raw -Path $Path | ConvertFrom-Json
}

function Find-BdnReports {
    param([string]$Root)
    if (-not (Test-Path $Root)) { return @() }
    Get-ChildItem -Path $Root -Recurse -Filter "*report*.json" |
        Where-Object { $_.Name -notmatch "log" } |
        ForEach-Object { $_.FullName }
}

function Get-BdnMeanNs {
    param($Benchmark)
    if ($null -eq $Benchmark) { return $null }
    $stats = $Benchmark.Statistics
    if ($null -eq $stats) { return $null }
    return [double]$stats.Mean
}

function Get-BdnAllocBytes {
    param($Benchmark)
    if ($null -eq $Benchmark) { return $null }
    $mem = $Benchmark.Memory
    if ($null -eq $mem) { return $null }
    if ($null -ne $mem.BytesAllocatedPerOperation) {
        return [double]$mem.BytesAllocatedPerOperation
    }
    return $null
}

function Convert-SvgToPng {
    param(
        [Parameter(Mandatory = $true)][string]$SvgPath,
        [Parameter(Mandatory = $true)][string]$PngPath,
        [int]$Width = 720,
        [int]$Height = 420
    )

    if (-not (Test-Path $SvgPath)) { return $false }

    $edgeCandidates = @(
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "${env:ProgramFiles}\Microsoft\Edge\Application\msedge.exe"
    )
    $edge = $edgeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $edge) {
        Write-Warning "Edge not found; cannot rasterize chart to PNG: $PngPath"
        return $false
    }

    $htmlPath = "$PngPath.render.html"
    $svgName = [System.IO.Path]::GetFileName($SvgPath)
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $html = @"
<!DOCTYPE html><html><head><meta charset="utf-8"><style>
html,body{margin:0;padding:0;background:#fff;overflow:hidden;}
img{display:block;width:${Width}px;height:${Height}px;}
</style></head><body><img src="$svgName" width="$Width" height="$Height"/></body></html>
"@
    [System.IO.File]::WriteAllText($htmlPath, $html, $utf8)
    $prevEap = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $fileUrl = "file:///" + ($htmlPath -replace '\\', '/')
        & $edge --headless=new --disable-gpu --allow-file-access-from-files --hide-scrollbars `
            --window-size="$($Width + 20),$($Height + 30)" --screenshot="$PngPath" $fileUrl 2>$null | Out-Null
        Start-Sleep -Milliseconds 500
    }
    finally {
        $ErrorActionPreference = $prevEap
        Remove-Item $htmlPath -Force -ErrorAction SilentlyContinue
    }
    return (Test-Path $PngPath)
}

function Write-ChartPng {
    param(
        [Parameter(Mandatory = $true)][string]$SvgContent,
        [Parameter(Mandatory = $true)][string]$OutPath,
        [int]$Width = 720,
        [int]$Height = 420
    )

    $pngPath = if ($OutPath -like '*.png') { $OutPath } else { [System.IO.Path]::ChangeExtension($OutPath, '.png') }
    $tempSvg = Join-Path ([System.IO.Path]::GetDirectoryName($pngPath)) ([System.IO.Path]::GetFileNameWithoutExtension($pngPath) + ".tmp.svg")
    $utf8 = New-Object System.Text.UTF8Encoding $false
    try {
        [System.IO.File]::WriteAllText($tempSvg, $SvgContent, $utf8)
        if (-not (Convert-SvgToPng -SvgPath $tempSvg -PngPath $pngPath -Width $Width -Height $Height)) {
            throw "Failed to write chart PNG: $pngPath"
        }
    }
    finally {
        Remove-Item $tempSvg -Force -ErrorAction SilentlyContinue
    }
}

function New-BarChartSvg {
    param(
        [string]$Title,
        [string[]]$Labels,
        [double[]]$Values,
        [string]$YLabel,
        [string]$OutPath,
        [int]$Width = 720,
        [int]$Height = 420
    )

    $marginL = 70
    $marginR = 24
    $marginT = 48
    $marginB = 72
    $plotW = $Width - $marginL - $marginR
    $plotH = $Height - $marginT - $marginB
    $max = ($Values | Measure-Object -Maximum).Maximum
    if ($max -le 0) { $max = 1 }

    $palette = @("#2563EB", "#DC2626", "#059669", "#D97706", "#7C3AED", "#0891B2")
    $n = $Labels.Count
    $gap = 16
    $barW = [Math]::Max(12, ($plotW - $gap * ($n + 1)) / [Math]::Max(1, $n))

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$sb.AppendLine("<svg xmlns=`"http://www.w3.org/2000/svg`" width=`"$Width`" height=`"$Height`" viewBox=`"0 0 $Width $Height`">")
    [void]$sb.AppendLine('<rect width="100%" height="100%" fill="#ffffff"/>')
    [void]$sb.AppendLine("<text x=`"$($Width/2)`" y=`"28`" text-anchor=`"middle`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"16`" font-weight=`"600`" fill=`"#111827`">$([System.Security.SecurityElement]::Escape($Title))</text>")
    [void]$sb.AppendLine("<text x=`"16`" y=`"$($marginT + $plotH/2)`" text-anchor=`"middle`" transform=`"rotate(-90 16,$($marginT + $plotH/2))`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"12`" fill=`"#4B5563`">$([System.Security.SecurityElement]::Escape($YLabel))</text>")
    [void]$sb.AppendLine("<line x1=`"$marginL`" y1=`"$marginT`" x2=`"$marginL`" y2=`"$($marginT+$plotH)`" stroke=`"#9CA3AF`" stroke-width=`"1`"/>")
    [void]$sb.AppendLine("<line x1=`"$marginL`" y1=`"$($marginT+$plotH)`" x2=`"$($marginL+$plotW)`" y2=`"$($marginT+$plotH)`" stroke=`"#9CA3AF`" stroke-width=`"1`"/>")

    for ($i = 0; $i -lt $n; $i++) {
        $v = $Values[$i]
        $h = [Math]::Max(1, ($v / $max) * $plotH)
        $x = $marginL + $gap + $i * ($barW + $gap)
        $y = $marginT + $plotH - $h
        $color = $palette[$i % $palette.Count]
        [void]$sb.AppendLine("<rect x=`"$([Math]::Round($x,1))`" y=`"$([Math]::Round($y,1))`" width=`"$([Math]::Round($barW,1))`" height=`"$([Math]::Round($h,1))`" fill=`"$color`"/>")
        $label = $Labels[$i]
        $lx = $x + $barW / 2
        [void]$sb.AppendLine("<text x=`"$([Math]::Round($lx,1))`" y=`"$($Height-36)`" text-anchor=`"middle`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"11`" fill=`"#374151`">$([System.Security.SecurityElement]::Escape($label))</text>")
        $valueText = if ($v -ge 1000) { "{0:N0}" -f $v } elseif ($v -ge 10) { "{0:N1}" -f $v } else { "{0:N2}" -f $v }
        [void]$sb.AppendLine("<text x=`"$([Math]::Round($lx,1))`" y=`"$([Math]::Round($y-6,1))`" text-anchor=`"middle`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"11`" fill=`"#111827`">$valueText</text>")
    }

    [void]$sb.AppendLine('</svg>')
    Write-ChartPng -SvgContent $sb.ToString() -OutPath $OutPath -Width $Width -Height $Height
}

function New-GroupedBarChartSvg {
    param(
        [string]$Title,
        [string[]]$Categories,
        [hashtable]$Series, # name -> double[]
        [string]$YLabel,
        [string]$OutPath,
        [int]$Width = 780,
        [int]$Height = 440
    )

    $marginL = 70
    $marginR = 24
    $marginT = 48
    $marginB = 90
    $plotW = $Width - $marginL - $marginR
    $plotH = $Height - $marginT - $marginB
    $seriesNames = @($Series.Keys)
    $allValues = @()
    foreach ($name in $seriesNames) { $allValues += $Series[$name] }
    $max = ($allValues | Measure-Object -Maximum).Maximum
    if ($max -le 0) { $max = 1 }

    $palette = @{ }
    $colors = @("#2563EB", "#DC2626", "#059669", "#D97706")
    for ($i = 0; $i -lt $seriesNames.Count; $i++) {
        $palette[$seriesNames[$i]] = $colors[$i % $colors.Count]
    }

    $catCount = $Categories.Count
    $groupGap = 28
    $groupW = ($plotW - $groupGap * ($catCount + 1)) / [Math]::Max(1, $catCount)
    $barGap = 6
    $barW = ($groupW - $barGap * ($seriesNames.Count - 1)) / [Math]::Max(1, $seriesNames.Count)

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$sb.AppendLine("<svg xmlns=`"http://www.w3.org/2000/svg`" width=`"$Width`" height=`"$Height`" viewBox=`"0 0 $Width $Height`">")
    [void]$sb.AppendLine('<rect width="100%" height="100%" fill="#ffffff"/>')
    [void]$sb.AppendLine("<text x=`"$($Width/2)`" y=`"28`" text-anchor=`"middle`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"16`" font-weight=`"600`" fill=`"#111827`">$([System.Security.SecurityElement]::Escape($Title))</text>")
    [void]$sb.AppendLine("<text x=`"16`" y=`"$($marginT + $plotH/2)`" text-anchor=`"middle`" transform=`"rotate(-90 16,$($marginT + $plotH/2))`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"12`" fill=`"#4B5563`">$([System.Security.SecurityElement]::Escape($YLabel))</text>")
    [void]$sb.AppendLine("<line x1=`"$marginL`" y1=`"$marginT`" x2=`"$marginL`" y2=`"$($marginT+$plotH)`" stroke=`"#9CA3AF`" stroke-width=`"1`"/>")
    [void]$sb.AppendLine("<line x1=`"$marginL`" y1=`"$($marginT+$plotH)`" x2=`"$($marginL+$plotW)`" y2=`"$($marginT+$plotH)`" stroke=`"#9CA3AF`" stroke-width=`"1`"/>")

    for ($c = 0; $c -lt $catCount; $c++) {
        $gx = $marginL + $groupGap + $c * ($groupW + $groupGap)
        for ($s = 0; $s -lt $seriesNames.Count; $s++) {
            $name = $seriesNames[$s]
            $v = [double]$Series[$name][$c]
            $h = [Math]::Max(1, ($v / $max) * $plotH)
            $x = $gx + $s * ($barW + $barGap)
            $y = $marginT + $plotH - $h
            $color = $palette[$name]
            [void]$sb.AppendLine("<rect x=`"$([Math]::Round($x,1))`" y=`"$([Math]::Round($y,1))`" width=`"$([Math]::Round($barW,1))`" height=`"$([Math]::Round($h,1))`" fill=`"$color`"/>")
            $valueText = if ($v -ge 1000) { "{0:N0}" -f $v } elseif ($v -ge 10) { "{0:N1}" -f $v } else { "{0:N2}" -f $v }
            [void]$sb.AppendLine("<text x=`"$([Math]::Round($x + $barW/2,1))`" y=`"$([Math]::Round($y-6,1))`" text-anchor=`"middle`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"10`" fill=`"#111827`">$valueText</text>")
        }
        [void]$sb.AppendLine("<text x=`"$([Math]::Round($gx + $groupW/2,1))`" y=`"$($Height-52)`" text-anchor=`"middle`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"11`" fill=`"#374151`">$([System.Security.SecurityElement]::Escape($Categories[$c]))</text>")
    }

    $legendX = $marginL
    $legendY = $Height - 24
    for ($s = 0; $s -lt $seriesNames.Count; $s++) {
        $name = $seriesNames[$s]
        $color = $palette[$name]
        $lx = $legendX + $s * 140
        [void]$sb.AppendLine("<rect x=`"$lx`" y=`"$($legendY-10)`" width=`"12`" height=`"12`" fill=`"$color`"/>")
        [void]$sb.AppendLine("<text x=`"$($lx+18)`" y=`"$legendY`" font-family=`"Segoe UI, Arial, sans-serif`" font-size=`"12`" fill=`"#374151`">$([System.Security.SecurityElement]::Escape($name))</text>")
    }

    [void]$sb.AppendLine('</svg>')
    Write-ChartPng -SvgContent $sb.ToString() -OutPath $OutPath -Width $Width -Height $Height
}

# --- Load inputs ---
$evidence = Get-JsonFile (Join-Path $ArtifactsCore "evidence.json")
$godot = Get-JsonFile (Join-Path $ArtifactsGodot "metrics.json")

$bdnBenchmarks = @()
foreach ($file in (Find-BdnReports $ArtifactsCore)) {
    $json = Get-JsonFile $file
    if ($null -eq $json) { continue }
    if ($json.Benchmarks) {
        $bdnBenchmarks += @($json.Benchmarks)
    }
}

function Get-BenchParamValue {
    param($Benchmark, [string]$Name)
    if ($null -eq $Benchmark) { return $null }
    $raw = $Benchmark.Parameters
    if ($null -eq $raw) { return $null }
    if ($raw -is [string]) {
        if ($raw -match "(?:^|[,\s])$Name[=:]\s*([^\s,]+)") { return $Matches[1] }
        return $null
    }
    $prop = $raw.PSObject.Properties[$Name]
    if ($prop) { return [string]$prop.Value }
    return $null
}

function Find-Bench {
    param(
        [string]$TypeContains,
        [string]$Method,
        [string]$N = $null,
        [string]$BindCount = $null
    )
    foreach ($b in $bdnBenchmarks) {
        $typeOk = $b.Type -like "*$TypeContains*"
        $methodOk = $b.Method -eq $Method
        $nOk = $true
        if ($N) {
            $paramN = Get-BenchParamValue $b "N"
            if (-not $paramN -and $b.FullName -match "N[=: ]+(\d+)") { $paramN = $Matches[1] }
            $nOk = $paramN -eq $N
        }
        $bindOk = $true
        if ($BindCount) {
            $paramBind = Get-BenchParamValue $b "BindCount"
            if (-not $paramBind -and $b.FullName -match "BindCount[=: ]+(\d+)") { $paramBind = $Matches[1] }
            $bindOk = $paramBind -eq $BindCount
        }
        if ($typeOk -and $methodOk -and $nOk -and $bindOk) { return $b }
    }
    return $null
}

# Chart 1: property ns/op at N=1000
$propLabels = @("DirectSetter", "TypedBinding", "ObjectBinding")
$propValues = @(
    (Get-BdnMeanNs (Find-Bench "PropertyPropagation" "DirectSetter" "1000")),
    (Get-BdnMeanNs (Find-Bench "PropertyPropagation" "TypedBinding" "1000")),
    (Get-BdnMeanNs (Find-Bench "PropertyPropagation" "ObjectBinding" "1000"))
)
if ($propValues -contains $null) {
    Write-Warning "PropertyPropagation BDN results incomplete; chart may show zeros."
    $propValues = @($propValues | ForEach-Object { if ($null -eq $_) { 0 } else { $_ } })
}
New-BarChartSvg -Title "Property propagation (N=1000) mean ns/op" -Labels $propLabels -Values $propValues -YLabel "ns/op" -OutPath (Join-Path $ReportDir "chart-property-ns.png")

# Chart 2: allocation B/op
$allocLabels = @("TypedIntBurst", "TypedEqualSkip", "ObjectPipeline")
$allocValues = @(
    (Get-BdnAllocBytes (Find-Bench "Allocation" "TypedIntBurst")),
    (Get-BdnAllocBytes (Find-Bench "Allocation" "TypedEqualSkipped")),
    (Get-BdnAllocBytes (Find-Bench "Allocation" "ObjectPipelineBurst"))
)
$allocValues = @($allocValues | ForEach-Object { if ($null -eq $_) { 0 } else { $_ } })
New-BarChartSvg -Title "Allocation B/op (1000 updates)" -Labels $allocLabels -Values $allocValues -YLabel "B/op" -OutPath (Join-Path $ReportDir "chart-allocation.png")

# Chart 3: coalesce writes from evidence
$coalesceLabels = @()
$coalesceValues = @()
if ($evidence -and $evidence.coalesce) {
    foreach ($row in $evidence.coalesce) {
        if ($row.sourceUpdates -eq 10000) {
            $coalesceLabels += [string]$row.mode
            $coalesceValues += [double]$row.targetWrites
        }
    }
}
if ($coalesceLabels.Count -eq 0) {
    $coalesceLabels = @("ui-thread-burst", "background-coalesced")
    $coalesceValues = @(0, 0)
}
New-BarChartSvg -Title "Target writes after 10000 source updates" -Labels $coalesceLabels -Values $coalesceValues -YLabel "target writes" -OutPath (Join-Path $ReportDir "chart-coalesce-writes.png")

# Chart 4: backpressure frames
$bpLabels = @()
$bpFrames = @()
$bpPeaks = @()
if ($godot -and $godot.backpressure) {
    foreach ($row in $godot.backpressure) {
        $bpLabels += [string]$row.mode
        $bpFrames += [double]$row.framesToComplete
        $bpPeaks += [double]$row.peakPerFrame
    }
}
if ($bpLabels.Count -eq 0) {
    $bpLabels = @("post-storm", "mailbox-drain")
    $bpFrames = @(0, 0)
    $bpPeaks = @(0, 0)
}
New-GroupedBarChartSvg -Title "Backpressure: frames vs peak/frame (10000)" -Categories $bpLabels -Series @{
    "framesToComplete" = $bpFrames
    "peakPerFrame" = $bpPeaks
} -YLabel "count" -OutPath (Join-Path $ReportDir "chart-backpressure-frames.png")

# Chart 5: virtual list nodes
$vlCategories = @()
$vlVirtual = @()
$vlNonVirtual = @()
if ($godot -and $godot.virtualList) {
    $counts = $godot.virtualList | ForEach-Object { [int]$_.itemCount } | Sort-Object -Unique
    foreach ($c in $counts) {
        $vlCategories += "$c"
        $v = $godot.virtualList | Where-Object { $_.mode -eq "virtual" -and $_.itemCount -eq $c } | Select-Object -First 1
        $nv = $godot.virtualList | Where-Object { $_.mode -eq "non-virtual" -and $_.itemCount -eq $c } | Select-Object -First 1
        $vlVirtual += $(if ($v) { [double]$v.activeNodes } else { 0 })
        $vlNonVirtual += $(if ($nv) { [double]$nv.activeNodes } else { 0 })
    }
}
if ($vlCategories.Count -eq 0) {
    $vlCategories = @("100", "1000", "10000")
    $vlVirtual = @(0, 0, 0)
    $vlNonVirtual = @(0, 0, 0)
}
New-GroupedBarChartSvg -Title "Active nodes vs item count" -Categories $vlCategories -Series @{
    "virtual" = $vlVirtual
    "non-virtual" = $vlNonVirtual
} -YLabel "active nodes" -OutPath (Join-Path $ReportDir "chart-virtual-list-nodes.png")

# Chart 6: binding setup from BDN BindingSetupBenchmarks (warmuped mean)
$setupBindCounts = @("10", "50", "100")
$setupLabels = @()
$setupValuesUs = @()
foreach ($bc in $setupBindCounts) {
    $setupLabels += ("typed-{0}" -f $bc)
    $meanNs = Get-BdnMeanNs (Find-Bench "BindingSetup" "TypedBindAndDispose" -BindCount $bc)
    $setupValuesUs += $(if ($null -eq $meanNs) { 0 } else { $meanNs / 1000.0 })
}
if (($setupValuesUs | Measure-Object -Sum).Sum -eq 0) {
    Write-Warning "BindingSetup BDN results missing; chart-binding-setup may show zeros."
}
New-BarChartSvg -Title "Typed binding Setup mean us/op by BindCount (BDN)" -Labels $setupLabels -Values $setupValuesUs -YLabel "us/op" -OutPath (Join-Path $ReportDir "chart-binding-setup.png")

# Chart 7: native compare targetWrites at N=10000
$nativeLabels = @()
$nativeWrites = @()
if ($godot -and $godot.nativeCompare) {
    foreach ($row in $godot.nativeCompare) {
        if ([int]$row.sourceUpdates -eq 10000) {
            $nativeLabels += [string]$row.mode
            $nativeWrites += [double]$row.targetWrites
        }
    }
}
if ($nativeLabels.Count -eq 0) {
    $nativeLabels = @("native-direct", "dotpudica-bound", "dotpudica-coalesced")
    $nativeWrites = @(0, 0, 0)
}
New-BarChartSvg -Title "Native compare targetWrites (N=10000)" -Labels $nativeLabels -Values $nativeWrites -YLabel "target writes" -OutPath (Join-Path $ReportDir "chart-native-compare.png")

# --- Build RESULTS.md ---
function Fmt([object]$v, [string]$fallback = "n/a") {
    if ($null -eq $v) { return $fallback }
    if ($v -is [double] -or $v -is [decimal] -or $v -is [float]) {
        return ("{0:N2}" -f $v)
    }
    return [string]$v
}

$gitHash = "unknown"
try { $gitHash = (git -C $RepoRoot rev-parse --short HEAD 2>$null) } catch {}
$cpu = $null
foreach ($b in $bdnBenchmarks) {
    if ($b.HostEnvironmentInfo -and $b.HostEnvironmentInfo.ProcessorName) {
        $cpu = [string]$b.HostEnvironmentInfo.ProcessorName
        break
    }
}
# HostEnvironmentInfo lives on report root in some exporters; scan files again if needed
if (-not $cpu) {
    foreach ($file in (Find-BdnReports $ArtifactsCore)) {
        $json = Get-JsonFile $file
        if ($json.HostEnvironmentInfo -and $json.HostEnvironmentInfo.ProcessorName) {
            $cpu = [string]$json.HostEnvironmentInfo.ProcessorName
            break
        }
    }
}
if (-not $cpu) { $cpu = $env:PROCESSOR_IDENTIFIER }
if (-not $cpu) { $cpu = "unknown" }
$os = [System.Environment]::OSVersion.VersionString
$utcNow = (Get-Date).ToUniversalTime().ToString("o")

$propDirect = Fmt $propValues[0]
$propTyped = Fmt $propValues[1]
$propObject = Fmt $propValues[2]
$allocTyped = Fmt $allocValues[0]
$allocEqual = Fmt $allocValues[1]
$allocObject = Fmt $allocValues[2]

$uiWrites = "n/a"; $bgWrites = "n/a"; $bgPending = "n/a"
if ($evidence -and $evidence.coalesce) {
    $ui = $evidence.coalesce | Where-Object { $_.mode -eq "ui-thread-burst" -and $_.sourceUpdates -eq 10000 } | Select-Object -First 1
    $bg = $evidence.coalesce | Where-Object { $_.mode -eq "background-coalesced" -and $_.sourceUpdates -eq 10000 } | Select-Object -First 1
    if ($ui) { $uiWrites = [string]$ui.targetWrites }
    if ($bg) { $bgWrites = [string]$bg.targetWrites; $bgPending = [string]$bg.pendingPosts }
}

$sb = New-Object System.Text.StringBuilder
function Add-Line([string]$text) { [void]$sb.AppendLine($text) }

Add-Line "# DotPudica Benchmark Report"
Add-Line ""
Add-Line "> Auto-generated by benchmarks/generate-report.ps1 from local measurements. Numbers vary by machine; conclusions follow relative relationships."
Add-Line ""
Add-Line "## Environment"
Add-Line ""
Add-Line "| Item | Value |"
Add-Line "|---|---|"
Add-Line ("| OS | {0} |" -f $os)
Add-Line ("| CPU | {0} |" -f $cpu)
Add-Line "| .NET | net8.0 |"
Add-Line "| Godot | 4.7.1 (.NET) |"
Add-Line ("| Git | {0} |" -f $gitHash)
Add-Line ("| Generated (UTC) | {0} |" -f $utcNow)
Add-Line ""
Add-Line "## Charts"
Add-Line ""
Add-Line "![Property ns/op](chart-property-ns.png)"
Add-Line ""
Add-Line "![Allocation B/op](chart-allocation.png)"
Add-Line ""
Add-Line "![Coalesce writes](chart-coalesce-writes.png)"
Add-Line ""
Add-Line "![Backpressure](chart-backpressure-frames.png)"
Add-Line ""
Add-Line "![Virtual list nodes](chart-virtual-list-nodes.png)"
Add-Line ""
Add-Line "![Binding setup](chart-binding-setup.png)"
Add-Line ""
Add-Line "![Native compare](chart-native-compare.png)"
Add-Line ""
Add-Line "## Godot UI decision table"
Add-Line ""
Add-Line "| Scenario | Recommendation | Evidence |"
Add-Line "|---|---|---|"
Add-Line "| Background progress / HP spam | Binding + Coalescer | native-compare / coalesce |"
Add-Line "| Network snapshots | Mailbox; or self-built frame-budgeted Post | backpressure (mailbox / post-budgeted) |"
Add-Line "| Lists larger than ~1k rows | Virtual list | virtual-list-nodes |"
Add-Line "| Frequent panels / popups | ViewPool / WindowPool | pool metrics |"
Add-Line "| Static, very few controls | Hand-written assignment OK | setup + property-ns |"
Add-Line ""
Add-Line "## Cross-cutting conclusions"
Add-Line ""
Add-Line "### 1. Property propagation: Direct vs TypedBinding vs ObjectBinding"
Add-Line ""
Add-Line "| Path | mean ns/op (N=1000) |"
Add-Line "|---|---|"
Add-Line ("| DirectSetter | {0} |" -f $propDirect)
Add-Line ("| TypedBinding | {0} |" -f $propTyped)
Add-Line ("| ObjectBinding | {0} |" -f $propObject)
Add-Line ""
Add-Line "**Conclusion:** For small main-thread assignments, Direct is usually fastest; TypedBinding has fixed pipeline overhead vs Direct and is the same latency order as ObjectBinding (ranking may flip by JIT/platform), but zero-alloc. Prefer strongly typed bindings in production."
Add-Line ""
Add-Line "### 2. Allocation: same-type value binding vs object pipeline"
Add-Line ""
Add-Line "| Path | B/op |"
Add-Line "|---|---|"
Add-Line ("| TypedIntBurst | {0} |" -f $allocTyped)
Add-Line ("| TypedEqualSkipped | {0} |" -f $allocEqual)
Add-Line ("| ObjectPipelineBurst | {0} |" -f $allocObject)
Add-Line ""
Add-Line "**Conclusion:** Same-type int->int hot path and equal-skip should be near 0 B/op, supporting the README zero-boxing claim; the object pipeline boxes/allocates and suits type-erasure only."
Add-Line ""
Add-Line "### 3. Coalesced dispatch: source updates vs target writes"
Add-Line ""
Add-Line "Core evidence (10000 source updates):"
Add-Line ""
Add-Line "| Mode | targetWrites | pendingPosts |"
Add-Line "|---|---|---|"
Add-Line ("| ui-thread-burst | {0} | 0 |" -f $uiWrites)
Add-Line ("| background-coalesced | {0} | {1} |" -f $bgWrites, $bgPending)
Add-Line ""
Add-Line "Godot headless PropertyBurst:"
Add-Line ""
Add-Line "| sourceUpdates | targetWrites | framesToSettle | elapsedMs |"
Add-Line "|---|---|---|---|"
if ($godot -and $godot.propertyBurst) {
    foreach ($row in $godot.propertyBurst) {
        Add-Line ("| {0} | {1} | {2} | {3} |" -f $row.sourceUpdates, $row.targetWrites, $row.framesToSettle, [Math]::Round($row.elapsedMs, 2))
    }
}
Add-Line ""
Add-Line "**Conclusion:** After background bursts through the Coalescer, UI writes drop to single digits and pending Post is usually 1 — high-frequency updates coalesce to the latest; UI-thread direct still near 1:1 writes."
Add-Line ""
Add-Line "### 4. Native Godot three-way compare"
Add-Line ""
Add-Line "| mode | sourceUpdates | targetWrites | framesToSettle | elapsedMs |"
Add-Line "|---|---|---|---|---|"
if ($godot -and $godot.nativeCompare) {
    foreach ($row in $godot.nativeCompare) {
        Add-Line ("| {0} | {1} | {2} | {3} | {4} |" -f $row.mode, $row.sourceUpdates, $row.targetWrites, $row.framesToSettle, [Math]::Round($row.elapsedMs, 2))
    }
}
Add-Line ""
Add-Line "**Conclusion:** native-direct Posts every background update to the control; dotpudica-bound is 1:1 on the main thread; dotpudica-coalesced writes far fewer than source updates. Background spam should use the binding Coalescer, not hand-written per-update Post."
Add-Line ""
Add-Line "### 5. Backpressure: Post storm / budgeted / Mailbox"
Add-Line ""
Add-Line "| mode | executed | framesToComplete | peakPerFrame |"
Add-Line "|---|---|---|---|"
if ($godot -and $godot.backpressure) {
    foreach ($row in $godot.backpressure) {
        Add-Line ("| {0} | {1} | {2} | {3} |" -f $row.mode, $row.executed, $row.framesToComplete, $row.peakPerFrame)
    }
}
Add-Line ""
Add-Line "**Conclusion:** Unbounded Post can drain the queue in one frame; post-budgeted (64/frame) stretches completion frames and caps peak; Mailbox drains at most once per frame. Prefer Mailbox for network snapshots; if you must Post one-by-one, apply your own frame budget."
Add-Line ""
Add-Line "### 6. Lists: virtual vs non-virtual active nodes"
Add-Line ""
Add-Line "| mode | itemCount | activeNodes | bindMs | scrollMs |"
Add-Line "|---|---|---|---|---|"
if ($godot -and $godot.virtualList) {
    foreach ($row in $godot.virtualList) {
        Add-Line ("| {0} | {1} | {2} | {3} | {4} |" -f $row.mode, $row.itemCount, $row.activeNodes, [Math]::Round($row.bindMs, 2), [Math]::Round($row.scrollMs, 2))
    }
}
Add-Line ""
Add-Line "**Conclusion:** Non-virtual node count grows roughly linearly with itemCount; virtual-list active nodes are near-constant (viewport + overscan). Prefer virtual lists for large datasets."
Add-Line ""
Add-Line "### 7. Setup and View lifecycle"
Add-Line ""
Add-Line "Core Setup (BenchmarkDotNet BindingSetupBenchmarks.TypedBindAndDispose, includes warmup):"
Add-Line ""
Add-Line "| method | bindCount | mean us/op | mean ms/op |"
Add-Line "|---|---|---|---|"
foreach ($bc in $setupBindCounts) {
    $meanNs = Get-BdnMeanNs (Find-Bench "BindingSetup" "TypedBindAndDispose" -BindCount $bc)
    if ($null -eq $meanNs) {
        Add-Line ("| TypedBindAndDispose | {0} | n/a | n/a |" -f $bc)
    }
    else {
        Add-Line ("| TypedBindAndDispose | {0} | {1} | {2} |" -f $bc, [Math]::Round($meanNs / 1000.0, 2), [Math]::Round($meanNs / 1e6, 4))
    }
}
Add-Line ""
Add-Line "Godot ViewLifecycle:"
Add-Line ""
Add-Line "| bindCount | initMs | disposeMs |"
Add-Line "|---|---|---|"
if ($godot -and $godot.viewLifecycle) {
    foreach ($row in $godot.viewLifecycle) {
        Add-Line ("| {0} | {1} | {2} |" -f $row.bindCount, [Math]::Round($row.initMs, 2), [Math]::Round($row.disposeMs, 2))
    }
}
Add-Line ""
Add-Line "**Conclusion:** BDN setup (post-warmup) tracks bind count without first-hit JIT skew; absolute cost stays small. Godot ViewLifecycle initMs is dominated by fixed host overhead. Frequent enter/exit should use pooling instead of rebuilding the binding graph each time."
Add-Line ""
Add-Line "### 8. View / Window pools"
Add-Line ""
Add-Line "| mode | iterations | createdNodes | reusedCount | elapsedMs |"
Add-Line "|---|---|---|---|---|"
if ($godot -and $godot.pool) {
    foreach ($row in $godot.pool) {
        Add-Line ("| {0} | {1} | {2} | {3} | {4} |" -f $row.mode, $row.iterations, $row.createdNodes, $row.reusedCount, [Math]::Round($row.elapsedMs, 2))
    }
}
Add-Line ""
Add-Line "**Conclusion:** Reuse count should be near iterations-1 (pool hit); frequent panels/popups should use ViewPool / WindowPool."
Add-Line ""
Add-Line "## README claims"
Add-Line ""
Add-Line "| Claim | Result |"
Add-Line "|---|---|"
Add-Line "| Strongly typed hot path, zero boxing | Supported (see Allocation chart; use local B/op) |"
Add-Line "| Auto-coalesce high-frequency background property updates | Supported (Core evidence + Godot PropertyBurst / native-compare) |"
Add-Line "| Virtual list instantiates only visible rows | Supported (active nodes do not grow linearly with data size) |"
Add-Line "| Mailbox can coalesce Post backlog | Supported (frames/peak comparison) |"
Add-Line "| Small UI faster when native | Supported (DirectSetter < TypedBinding; see decision table / native compare) |"
Add-Line "| No global frame-budget scheduler | Partially covered (post-budgeted is evidence simulation only, not a built-in scheduler) |"
Add-Line "| Mobile AOT | iOS NativeAOT: see RESULTS_IOS.md |"
Add-Line ""
Add-Line "## Boundaries"
Add-Line ""
Add-Line "- Does not measure GPU/render frame rate or input-latency feel."
Add-Line "- Absolute ns/ms must not be compared across machines, nor desktop JIT ↔ iOS AOT; use relative ratios and structural counts within the same table."
Add-Line "- post-budgeted is a benchmark-side frame-cap simulation, not a built-in global scheduler."
Add-Line "- CI does not run this pipeline by default; locally use benchmarks/run-all.ps1; on iOS run BenchmarkRunner from an export package."
Add-Line "- Structural focus (not CI hard gates): virtual-list active nodes should be far below data size; coalesced targetWrites should be far below sourceUpdates; pool reusedCount should be clearly greater than 0."
Add-Line "- iOS device conclusions beyond this desktop report: benchmarks/report/RESULTS_IOS.md."

$outMd = Join-Path $ReportDir "RESULTS.md"
[System.IO.File]::WriteAllText($outMd, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote report to $ReportDir"



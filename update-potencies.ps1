<#
.SYNOPSIS
  Scrape the FFXIV NA job guide and update HoT/DoT potencies in DamageTerror/Jobs/<JOB>.cs.

.DESCRIPTION
  Matches existing dictionary entries by the action name in the trailing comment
  (e.g. `{ 1881, 55 },  // Combust III`). Status IDs are not in the job guide, so
  the comment is the only join key — entries without a name comment are skipped.

  Three dictionaries are updated:
    DotTickPotencies        — DoT tick potency
    DotInitialHitPotencies  — initial hit potency for DoT-applying skills
    HotTickPotencies        — HoT tick potency

.EXAMPLE
  .\scripts\update-potencies.ps1                # update every job (default)
  .\scripts\update-potencies.ps1 -Job AST       # update just one job
  .\scripts\update-potencies.ps1 -DryRun        # preview changes for every job
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Job,

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$JobUrls = @{
    AST = 'astrologian'; WHM = 'whitemage'; SCH = 'scholar'; SGE = 'sage'
    BRD = 'bard';        MCH = 'machinist'; DNC = 'dancer'
    BLM = 'blackmage';   SMN = 'summoner';  RDM = 'redmage';  PCT = 'pictomancer'; BLU = 'bluemage'
    PLD = 'paladin';     WAR = 'warrior';   DRK = 'darkknight'; GNB = 'gunbreaker'
    MNK = 'monk';        DRG = 'dragoon';   NIN = 'ninja';    SAM = 'samurai';     RPR = 'reaper'; VPR = 'viper'
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$JobsDir  = Join-Path $RepoRoot 'DamageTerror\DamageTerror\Jobs'

function Get-PotenciesFromPage {
    param([string]$JobAbbr)

    $slug = $JobUrls[$JobAbbr.ToUpper()]
    if (-not $slug) { throw "Unknown job '$JobAbbr'. Known: $($JobUrls.Keys -join ', ')" }

    $url = "https://na.finalfantasyxiv.com/jobguide/$slug/"
    Write-Host "[$JobAbbr] fetching $url" -ForegroundColor Cyan
    $html = (Invoke-WebRequest -Uri $url -UseBasicParsing -UserAgent 'Mozilla/5.0').Content

    $actionRx  = [regex]'(?s)<tr id="pve_action__\d+">(.*?)</tr>'
    $nameRx    = [regex]'<strong>([^<]+)</strong>'
    $contentRx = [regex]'(?s)<td class="content">\s*(.*?)\s*</td>'

    $result = @{}

    foreach ($m in $actionRx.Matches($html)) {
        $block = $m.Groups[1].Value
        $nm = $nameRx.Match($block)
        $cm = $contentRx.Match($block)
        if (-not $nm.Success -or -not $cm.Success) { continue }

        $name = [System.Net.WebUtility]::HtmlDecode($nm.Groups[1].Value).Trim()

        # Normalize description: <br> -> newline, drop other tags, decode entities
        $text = $cm.Groups[1].Value
        $text = [regex]::Replace($text, '(?i)<br\s*/?>', "`n")
        $text = [regex]::Replace($text, '<[^>]+>', '')
        $text = [System.Net.WebUtility]::HtmlDecode($text).Trim()

        $info = @{ DotTick = $null; DotInitial = $null; HotTick = $null }

        # HoT tick (with initial heal): Cure Potency right after "Additional Effect: Regen"
        $hotRegen = [regex]::Match($text, '(?is)Additional Effect:\s*Regen\s*\n\s*Cure Potency:\s*(\d+)')
        if ($hotRegen.Success) {
            $info.HotTick = [int]$hotRegen.Groups[1].Value
        }

        # HoT tick (pure): "healing over time" anywhere, then Cure Potency
        if ($null -eq $info.HotTick) {
            $pureHot = [regex]::Match($text, '(?is)healing over time[\s\S]*?Cure Potency:\s*(\d+)')
            if ($pureHot.Success) {
                $info.HotTick = [int]$pureHot.Groups[1].Value
            }
        }

        # DoT tick (with initial hit): "Additional Effect: <something>" line followed by Potency + Duration
        # Matches Aero ("Wind damage over time"), Dia, Caustic Bite ("Poison"), etc.
        $dotInit = [regex]::Match($text, '(?is)Additional Effect:[^\n]+\n\s*Potency:\s*(\d+)\s*\n\s*Duration:')
        if ($dotInit.Success) {
            $info.DotTick = [int]$dotInit.Groups[1].Value
            $before = $text.Substring(0, $dotInit.Index)
            $initial = [regex]::Match($before, '(?i)potency of\s*(\d+)')
            if ($initial.Success) {
                $info.DotInitial = [int]$initial.Groups[1].Value
            }
        }

        # DoT tick (pure): "damage over time" then Potency (no separate initial hit)
        if ($null -eq $info.DotTick) {
            $pureDot = [regex]::Match($text, '(?is)damage over time[^\n]*\n\s*Potency:\s*(\d+)')
            if ($pureDot.Success) {
                $info.DotTick = [int]$pureDot.Groups[1].Value
            }
        }

        if ($info.DotTick -or $info.DotInitial -or $info.HotTick) {
            $result[$name] = $info
        }
    }

    return $result
}

function Update-JobFile {
    param(
        [string]$JobAbbr,
        [hashtable]$Actions
    )

    $file = Join-Path $JobsDir "$JobAbbr.cs"
    if (-not (Test-Path $file)) {
        Write-Warning "[$JobAbbr] job file not found: $file"
        return
    }

    # Read preserving line endings
    $original = [System.IO.File]::ReadAllText($file)
    $lines = $original -split "(`r?`n)"   # keep separators as alternating array elements

    $dictMap = @{
        DotTickPotencies       = 'DotTick'
        DotInitialHitPotencies = 'DotInitial'
        HotTickPotencies       = 'HotTick'
    }

    $entryRx  = [regex]'^(\s*\{\s*\d+\s*,\s*)(\d+)(\s*\}\s*,\s*//\s*)(.+?)\s*$'
    $closeRx  = [regex]'^\s*\}\s*;\s*$'

    $changes = New-Object System.Collections.Generic.List[string]
    $currentDict = $null

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Detect dictionary entry first — these property declarations span two lines
        # ("...DotTickPotencies { get; } = new Dictionary<uint, int>" then "{").
        # The opening "{" line belongs to no dictionary yet — we only set state when we see the property name.
        foreach ($name in $dictMap.Keys) {
            if ($line -match "\b$name\b" -and $line -match 'Dictionary<uint,\s*int>') {
                $currentDict = $name
                break
            }
        }

        if ($null -eq $currentDict) { continue }

        if ($closeRx.IsMatch($line)) {
            $currentDict = $null
            continue
        }

        $em = $entryRx.Match($line)
        if (-not $em.Success) { continue }

        $oldPot = [int]$em.Groups[2].Value
        $action = $em.Groups[4].Value.Trim()
        $infoKey = $dictMap[$currentDict]

        if (-not $Actions.ContainsKey($action)) { continue }
        $newPot = $Actions[$action][$infoKey]
        if ($null -eq $newPot -or $newPot -eq $oldPot) { continue }

        $lines[$i] = "$($em.Groups[1].Value)$newPot$($em.Groups[3].Value)$($em.Groups[4].Value)"
        $changes.Add(("  {0,-30} {1,-22} {2,4} -> {3,-4}" -f $action, $infoKey, $oldPot, $newPot)) | Out-Null
    }

    if ($changes.Count -eq 0) {
        Write-Host "[$JobAbbr] no updates needed" -ForegroundColor DarkGray
        return
    }

    Write-Host "[$JobAbbr] $($changes.Count) update(s):" -ForegroundColor Green
    $changes | ForEach-Object { Write-Host $_ }

    if ($DryRun) {
        Write-Host "  (DryRun — not writing)" -ForegroundColor Yellow
    } else {
        $updated = -join $lines
        [System.IO.File]::WriteAllText($file, $updated)
    }
}

# === Main ===
$targets = if ($Job) { @($Job.ToUpper()) } else { $JobUrls.Keys | Sort-Object }

$first = $true
foreach ($abbr in $targets) {
    if (-not $first) { Start-Sleep -Milliseconds 500 }   # be polite when scraping all jobs
    $first = $false
    try {
        $actions = Get-PotenciesFromPage -JobAbbr $abbr
        Update-JobFile -JobAbbr $abbr -Actions $actions
    } catch {
        Write-Warning "[$abbr] $_"
    }
}

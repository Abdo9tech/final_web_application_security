param(
    [switch]$DryRun,
    [switch]$Undo,
    [int]$Days = 0,
    [string]$IncludePatterns = "",
    [string]$ExcludeDirs = "",
    [switch]$Yes
)

$ProjectRoot   = $PSScriptRoot
$ArchiveFolder = Join-Path $ProjectRoot "my_deleted_files"
$LogFile       = Join-Path $ArchiveFolder "cleanup_log.txt"

$TargetExtensions = @("*.md","*.txt","*.log","*.tmp","*.temp","*.bak","*.backup","*.old","*.orig","*.swp","*.swo","thumbs.db")
if ($IncludePatterns -ne "") { $TargetExtensions += ($IncludePatterns -split ",") }

$ProtectedDirs = @(".git",".kiro",".vs",".vscode","my_deleted_files","bin","obj","node_modules","Project(DEPI)","DAL","PLL","SecurityReports","SecurityTests")
if ($ExcludeDirs -ne "") { $ProtectedDirs += ($ExcludeDirs -split ",") }

$ProtectedNames = @(".env","README.md","package.json","package-lock.json","requirements.txt","Pipfile","Dockerfile","docker-compose.yml","docker-compose.override.yml",".gitignore","clean.ps1","db-fixes.sql","seed-mock-data.sql","appsettings.json","appsettings.Development.json","appsettings.Production.json")

function Format-FileSize([long]$Bytes) {
    if ($Bytes -ge 1MB) { return ("{0:N2} MB" -f ($Bytes / 1MB)) }
    if ($Bytes -ge 1KB) { return ("{0:N2} KB" -f ($Bytes / 1KB)) }
    return "$Bytes B"
}

function Test-IsProtected([System.IO.FileInfo]$File) {
    if ($File.FullName.StartsWith($ArchiveFolder, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    foreach ($dir in $ProtectedDirs) {
        $dirFull = Join-Path $ProjectRoot $dir
        if ($File.FullName.StartsWith($dirFull, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    if ($ProtectedNames -contains $File.Name) { return $true }
    return $false
}

function Get-CandidateFiles {
    $cutoff = if ($Days -gt 0) { (Get-Date).AddDays(-$Days) } else { $null }
    $seen = @{}
    $result = New-Object System.Collections.Generic.List[System.IO.FileInfo]
    foreach ($pattern in $TargetExtensions) {
        $found = Get-ChildItem -Path $ProjectRoot -Filter $pattern -File -Recurse -ErrorAction SilentlyContinue
        foreach ($f in $found) {
            if ($seen.ContainsKey($f.FullName)) { continue }
            $seen[$f.FullName] = $true
            if (Test-IsProtected $f) { continue }
            if ($null -ne $cutoff -and $f.LastWriteTime -gt $cutoff) { continue }
            $result.Add($f)
        }
    }
    return $result
}

function Invoke-Undo {
    Write-Host ""
    Write-Host "UNDO - Restoring files to original locations" -ForegroundColor Cyan
    if (-not (Test-Path $LogFile)) { Write-Host "No cleanup_log.txt found. Nothing to undo." -ForegroundColor Yellow; return }
    $lines = Get-Content $LogFile -Encoding UTF8 | Where-Object { $_.StartsWith("MOVED") }
    if ($lines.Count -eq 0) { Write-Host "No MOVED entries found. Nothing to undo." -ForegroundColor Yellow; return }
    $restored = 0; $failed = 0
    foreach ($line in $lines) {
        $parts = $line -split ";;"
        if ($parts.Count -lt 3) { continue }
        $originalPath = $parts[1].Trim()
        $archivedPath = $parts[2].Trim()
        if (-not (Test-Path $archivedPath)) { Write-Host "NOT FOUND: $archivedPath" -ForegroundColor Red; $failed++; continue }
        $originalDir = Split-Path $originalPath -Parent
        if (-not (Test-Path $originalDir)) { New-Item -ItemType Directory -Path $originalDir -Force | Out-Null }
        try {
            Move-Item -Path $archivedPath -Destination $originalPath -Force
            Write-Host "Restored: $originalPath" -ForegroundColor Green
            $restored++
        } catch {
            Write-Host "FAILED: $archivedPath => $($_.Exception.Message)" -ForegroundColor Red
            $failed++
        }
    }
    Write-Host ""
    Write-Host "Restored: $restored files" -ForegroundColor Green
    if ($failed -gt 0) { Write-Host "Failed: $failed files" -ForegroundColor Red }
}

function Invoke-Clean {
    $mode = if ($DryRun) { "DRY RUN (nothing will be moved)" } else { "CLEANUP" }
    Write-Host ""
    Write-Host "=== $mode ===" -ForegroundColor Cyan
    $files = Get-CandidateFiles
    if ($files.Count -eq 0) { Write-Host "Nothing to move. Project looks clean!" -ForegroundColor Green; return }
    $totalSize = 0L
    $rows = @()
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
        $totalSize += $f.Length
        $rows += [PSCustomObject]@{ File=$rel; Size=(Format-FileSize $f.Length); Modified=$f.LastWriteTime.ToString("yyyy-MM-dd") }
    }
    Write-Host ""
    $rows | Format-Table -AutoSize | Out-String | Write-Host
    Write-Host ("Total: {0} files | {1} freed" -f $files.Count, (Format-FileSize $totalSize)) -ForegroundColor Cyan
    if ($DryRun) { Write-Host "DRY RUN complete. Run without -DryRun to apply." -ForegroundColor Yellow; return }
    if (-not $Yes) {
        $answer = Read-Host ("Move {0} files to my_deleted_files? [y/N]" -f $files.Count)
        if ($answer -notmatch "^[yY]") { Write-Host "Cancelled." -ForegroundColor Yellow; return }
    }
    if (-not (Test-Path $ArchiveFolder)) { New-Item -ItemType Directory -Path $ArchiveFolder -Force | Out-Null }
    $runHeader = ("# Run: {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
    if (-not (Test-Path $LogFile)) {
        @("# Bookify Hotel Cleanup Log","# Format: MOVED;;OriginalPath;;ArchivedPath;;Size;;LastModified;;Reason","# Restore: .\clean.ps1 -Undo","#",$runHeader) | Set-Content $LogFile -Encoding UTF8
    } else {
        $runHeader | Add-Content $LogFile -Encoding UTF8
    }
    $moved = 0; $failed = 0
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
        $destination = Join-Path $ArchiveFolder $rel
        $destDir = Split-Path $destination -Parent
        try {
            if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
            Move-Item -Path $f.FullName -Destination $destination -Force
            $logLine = ("MOVED;;{0};;{1};;{2};;{3};;unused file type" -f $f.FullName,$destination,(Format-FileSize $f.Length),$f.LastWriteTime.ToString("yyyy-MM-dd HH:mm"))
            $logLine | Add-Content $LogFile -Encoding UTF8
            Write-Host ("Moved: {0}" -f $rel) -ForegroundColor Green
            $moved++
        } catch {
            Write-Host ("FAILED: {0} => {1}" -f $rel, $_.Exception.Message) -ForegroundColor Red
            $failed++
        }
    }
    Write-Host ""
    Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
    Write-Host ("Moved   : {0} files ({1} freed)" -f $moved, (Format-FileSize $totalSize)) -ForegroundColor Green
    if ($failed -gt 0) { Write-Host ("Failed  : {0} files" -f $failed) -ForegroundColor Red }
    Write-Host ("Archive : {0}" -f $ArchiveFolder) -ForegroundColor Cyan
    Write-Host ("Log     : {0}" -f $LogFile) -ForegroundColor Cyan
    Write-Host "Restore : .\clean.ps1 -Undo" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Bookify Hotel - Project Cleanup" -ForegroundColor Magenta
Write-Host ("Root: {0}" -f $ProjectRoot) -ForegroundColor Gray
if ($Days -gt 0) { Write-Host ("Filter: files not modified in last {0} days" -f $Days) -ForegroundColor Gray }

if ($Undo) { Invoke-Undo } else { Invoke-Clean }
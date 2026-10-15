#Requires -Version 5.1
$ErrorActionPreference = "Stop"
$Unity = "C:\Program Files\Unity 6000.6.5f1\Editor\Unity.exe"
# Prefer ASCII junction to avoid Korean path CLI issues
$Project = if (Test-Path "C:\BBR") { "C:\BBR" } else { Split-Path -Parent $PSScriptRoot }
$Log = Join-Path $Project "unity_generate_log.txt"
$LicClient = "C:\Program Files\Unity 6000.6.5f1\Editor\Data\Resources\Licensing\Client\Unity.Licensing.Client.exe"
$Method = "BlockyBattleRoyale.EditorTools.BatchBuildEntry.GenerateAndBuildAndQuit"

Write-Host "=== Blocky BR: license wait + generate + Windows build ==="
Write-Host "Project: $Project"
Write-Host "Method: $Method"

if (Test-Path $LicClient) {
  $deadline = (Get-Date).AddMinutes(5)
  while ((Get-Date) -lt $deadline) {
    $out = & $LicClient --showAllEntitlements 2>&1 | Out-String
    if ($out -match "com.unity.editor" -or ($out -notmatch "No licenses were found" -and $out.Length -gt 80)) {
      Write-Host "License OK."
      break
    }
    Write-Host ("[{0}] waiting for license..." -f (Get-Date -Format "HH:mm:ss"))
    Start-Sleep -Seconds 5
  }
}

if (-not (Test-Path $Unity)) {
  throw "Unity.exe not found: $Unity"
}

# Kill stale editor lock if any
$lock = Join-Path $Project "Temp\UnityLockfile"
if (Test-Path $lock) {
  Write-Host "Warning: UnityLockfile present — ensure no Editor is open."
}

& $Unity -batchmode -nographics -quit `
  -projectPath $Project `
  -executeMethod $Method `
  -logFile $Log

Write-Host "Unity exit code: $LASTEXITCODE"
Write-Host "Log: $Log"
if ($LASTEXITCODE -ne 0) {
  if (Test-Path $Log) { Get-Content $Log -Tail 60 }
  exit $LASTEXITCODE
}

Write-Host "Done. Build (if succeeded): $Project\Builds\Windows\BlockyBattleRoyale.exe"

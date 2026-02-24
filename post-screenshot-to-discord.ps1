# post-screenshot-to-discord.ps1
# Posts a screenshot file to Discord channel via OpenClaw message tool isn't available from PS,
# so this script is meant to be called after a screenshot is captured.
# Usage: .\post-screenshot-to-discord.ps1 [-ScreenshotPath <path>]

param(
    [string]$ScreenshotPath = (Join-Path $PSScriptRoot "screenshot.png"),
    [int]$WaitSeconds = 30
)

$ErrorActionPreference = "Stop"

# Wait for screenshot file to appear (game takes a few seconds)
$elapsed = 0
while (-not (Test-Path $ScreenshotPath) -and $elapsed -lt $WaitSeconds) {
    Start-Sleep -Seconds 1
    $elapsed++
}

if (-not (Test-Path $ScreenshotPath)) {
    Write-Error "Screenshot not found at $ScreenshotPath after ${WaitSeconds}s"
    exit 1
}

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
Write-Host "Found screenshot at $ScreenshotPath, posting to Discord..."
Write-Host "Timestamp: $timestamp"

# Output the path so the caller (CI or OpenClaw) can use it
Write-Output "SCREENSHOT_READY=$ScreenshotPath"
Write-Output "SCREENSHOT_TIMESTAMP=$timestamp"

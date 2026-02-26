<#
.SYNOPSIS
    Runs the leaderboard game with --screenshot flag, waits for the screenshot,
    and posts it to Discord via OpenClaw.

.DESCRIPTION
    This script automates the screenshot-to-Discord pipeline:
    1. Cleans up any old screenshot
    2. Launches the game with --screenshot
    3. Waits for the screenshot file to appear
    4. Posts it to Discord channel 1471208907346153667

.PARAMETER Channel
    Discord channel ID to post to. Defaults to 1471208907346153667.

.PARAMETER TimeoutSeconds
    Max seconds to wait for screenshot. Defaults to 60.
#>

param(
    [string]$Channel = "1471208907346153667",
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$BuildDir = Join-Path $ProjectRoot "Build"
$GameExe = Join-Path $BuildDir "LeaderboardGame.exe"
$ScreenshotPath = Join-Path $BuildDir "screenshot.png"

# Step 1: Clean old screenshot
if (Test-Path $ScreenshotPath) {
    Remove-Item $ScreenshotPath -Force
    Write-Host "[post-screenshot] Removed old screenshot."
}

# Step 2: Launch game with --screenshot
if (-not (Test-Path $GameExe)) {
    Write-Error "Game executable not found at $GameExe. Build the game first."
    exit 1
}

Write-Host "[post-screenshot] Launching game with --screenshot flag..."
$proc = Start-Process -FilePath $GameExe -ArgumentList "--screenshot" -WorkingDirectory $BuildDir -PassThru

# Step 3: Wait for screenshot to appear
Write-Host "[post-screenshot] Waiting up to ${TimeoutSeconds}s for screenshot..."
$elapsed = 0
while ($elapsed -lt $TimeoutSeconds) {
    if (Test-Path $ScreenshotPath) {
        # Give it a moment to finish writing
        Start-Sleep -Seconds 2
        Write-Host "[post-screenshot] Screenshot found at $ScreenshotPath after ${elapsed}s."
        break
    }
    Start-Sleep -Seconds 2
    $elapsed += 2
}

if (-not (Test-Path $ScreenshotPath)) {
    Write-Error "[post-screenshot] Timed out waiting for screenshot."
    exit 1
}

# Step 4: Copy screenshot to workspace-accessible directory with timestamp
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$ScreenshotsDir = Join-Path $ProjectRoot "screenshots"
if (-not (Test-Path $ScreenshotsDir)) {
    New-Item -ItemType Directory -Path $ScreenshotsDir -Force | Out-Null
}
$TimestampedName = "screenshot_${timestamp}.png"
$TimestampedPath = Join-Path $ScreenshotsDir $TimestampedName
Copy-Item $ScreenshotPath $TimestampedPath -Force
# Also keep a "latest" symlink/copy for easy access
$LatestPath = Join-Path $ScreenshotsDir "latest.png"
Copy-Item $ScreenshotPath $LatestPath -Force
Write-Host "[post-screenshot] Screenshot saved to $TimestampedPath"
Write-Host "[post-screenshot] Latest screenshot: $LatestPath"

# Step 5: Upload to Discord
$UploadScript = Join-Path (Split-Path $ProjectRoot -Parent) "discord-upload.js"
if (Test-Path $UploadScript) {
    Write-Host "[post-screenshot] Uploading to Discord channel $Channel..."
    node $UploadScript $Channel $TimestampedPath "Screenshot $timestamp"
} else {
    Write-Host "[post-screenshot] WARNING: discord-upload.js not found at $UploadScript, skipping Discord upload."
}

# Step 6: Output info for the caller
Write-Host ""
Write-Host "SCREENSHOT_READY=true"
Write-Host "SCREENSHOT_PATH=$TimestampedPath"
Write-Host "SCREENSHOT_LATEST=$LatestPath"
Write-Host "SCREENSHOT_TIMESTAMP=$timestamp"
Write-Host "DISCORD_CHANNEL=$Channel"

# Note: Lobster/OpenClaw can now read screenshots directly:
#   - Latest: C:\Users\Administrator\.openclaw\workspace\leaderboard-game\screenshots\latest.png
#   - All:    C:\Users\Administrator\.openclaw\workspace\leaderboard-game\screenshots\screenshot_*.png
# Use the OpenClaw `read` tool on these paths to view them.

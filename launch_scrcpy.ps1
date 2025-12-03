# Set error action preference to Stop to catch errors
$ErrorActionPreference = "Stop"

# Configuration
$RepoOwner = "Genymobile"
$RepoName = "scrcpy"
$BaseDir = "d:\dev\scr-cpy"

# Function to get the latest release tag
function Get-LatestRelease {
    try {
        $Uri = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
        $Response = Invoke-RestMethod -Uri $Uri
        return $Response
    }
    catch {
        Write-Host "Error checking for updates: $_" -ForegroundColor Red
        return $null
    }
}

# Function to download and extract release
function Install-Scrcpy {
    param (
        [string]$Version,
        [string]$DownloadUrl
    )

    $ZipPath = Join-Path $BaseDir "scrcpy-win64-$Version.zip"
    $ExtractPath = $BaseDir

    Write-Host "Downloading scrcpy $Version..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath
    }
    catch {
        Write-Host "Failed to download: $_" -ForegroundColor Red
        return $false
    }

    Write-Host "Extracting..." -ForegroundColor Cyan
    try {
        Expand-Archive -Path $ZipPath -DestinationPath $ExtractPath -Force
        Remove-Item $ZipPath -Force
        Write-Host "Successfully installed scrcpy $Version" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "Failed to extract: $_" -ForegroundColor Red
        return $false
    }
}

# Main Logic
Write-Host "Checking for scrcpy updates..." -ForegroundColor Yellow
$LatestRelease = Get-LatestRelease

if ($LatestRelease) {
    $LatestVersion = $LatestRelease.tag_name
    # Remove 'v' prefix if present for folder name consistency check, though usually folder has v
    # The zip usually extracts to scrcpy-win64-v3.3.3
    $ExpectedFolderName = "scrcpy-win64-$LatestVersion"
    $ExpectedPath = Join-Path $BaseDir $ExpectedFolderName

    if (-not (Test-Path $ExpectedPath)) {
        Write-Host "New version found: $LatestVersion" -ForegroundColor Green
        # Find the asset for win64
        $Asset = $LatestRelease.assets | Where-Object { $_.name -like "scrcpy-win64-*.zip" }
        
        if ($Asset) {
            Install-Scrcpy -Version $LatestVersion -DownloadUrl $Asset.browser_download_url
        } else {
            Write-Host "Could not find Windows 64-bit asset in the release." -ForegroundColor Red
        }
    } else {
        Write-Host "Scrcpy is up to date ($LatestVersion)." -ForegroundColor Green
    }
    
    # Update current path to the latest version
    if (Test-Path $ExpectedPath) {
        $ScrcpyPath = $ExpectedPath
    } else {
        # Fallback to existing if update failed or something went wrong, try to find any valid scrcpy folder
        $ScrcpyPath = Get-ChildItem $BaseDir -Directory | Where-Object { $_.Name -like "scrcpy-win64-*" } | Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName
    }
} else {
    # Offline or API fail, use local
     $ScrcpyPath = Get-ChildItem $BaseDir -Directory | Where-Object { $_.Name -like "scrcpy-win64-*" } | Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName
}

if (-not $ScrcpyPath) {
    Write-Host "No scrcpy installation found in $BaseDir." -ForegroundColor Red
    Read-Host "Press Enter to exit..."
    exit
}

Write-Host "Using scrcpy from: $ScrcpyPath" -ForegroundColor Gray
$AdbPath = Join-Path $ScrcpyPath "adb.exe"
$ScrcpyExe = Join-Path $ScrcpyPath "scrcpy.exe"

# Device Selection
Write-Host "Checking connected devices..." -ForegroundColor Yellow
# Run adb devices and capture output
$AdbOutput = & $AdbPath devices
# Filter out the header "List of devices attached" and empty lines
$Devices = $AdbOutput | Where-Object { $_ -match "\tdevice$" }

if ($Devices.Count -eq 0) {
    Write-Host "No devices connected." -ForegroundColor Red
    Read-Host "Press Enter to exit..."
    exit
}
elseif ($Devices.Count -eq 1) {
    $Serial = $Devices[0].Split("`t")[0]
    Write-Host "One device found: $Serial. Launching..." -ForegroundColor Green
    Start-Process -FilePath $ScrcpyExe -ArgumentList "-s $Serial" -Wait
}
else {
    Write-Host "Multiple devices found:" -ForegroundColor Cyan
    for ($i = 0; $i -lt $Devices.Count; $i++) {
        $Serial = $Devices[$i].Split("`t")[0]
        # Try to get model name for better identification
        $Model = & $AdbPath -s $Serial shell getprop ro.product.model
        Write-Host "[$($i+1)] $Serial ($Model)"
    }

    $Selection = Read-Host "Select device (1-$($Devices.Count))"
    if ($Selection -match "^\d+$" -and $Selection -ge 1 -and $Selection -le $Devices.Count) {
        $SelectedDevice = $Devices[$Selection - 1].Split("`t")[0]
        Write-Host "Launching for $SelectedDevice..." -ForegroundColor Green
        Start-Process -FilePath $ScrcpyExe -ArgumentList "-s $SelectedDevice" -Wait
    } else {
        Write-Host "Invalid selection." -ForegroundColor Red
    }
}

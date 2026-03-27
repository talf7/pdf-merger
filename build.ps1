#Requires -Version 3.0
$ErrorActionPreference = "Stop"

$ScriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir     = Join-Path $ScriptDir "PdfMerger"
$DistDir        = Join-Path $ScriptDir "dist"
$PkgDir         = Join-Path $ScriptDir ".packages"
$PdfSharpDll    = Join-Path $PkgDir "PdfSharp.dll"
$iTextSharpDll  = Join-Path $PkgDir "iTextSharp.dll"
$BouncyCastleDll= Join-Path $PkgDir "BouncyCastle.Crypto.dll"

Write-Host ""
Write-Host "=== PDF Merger - Build Script ===" -ForegroundColor Cyan
Write-Host ""

# ── 1. Create output folder ───────────────────────────────────────────────────
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Path $DistDir | Out-Null
New-Item -ItemType Directory -Path $PkgDir  -Force | Out-Null

# ── Helper: download a NuGet package and extract a DLL ───────────────────────
function Get-NugetDll {
    param($PackageId, $Version, $DestDll, $PreferPath)

    if (Test-Path $DestDll) {
        Write-Host "    $PackageId already cached." -ForegroundColor Green
        return
    }

    Write-Host "    Downloading $PackageId $Version..." -ForegroundColor Yellow
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $nupkg     = Join-Path $PkgDir "$PackageId.zip"
    $extracted = Join-Path $PkgDir "${PackageId}_pkg"

    try {
        Invoke-WebRequest "https://www.nuget.org/api/v2/package/$PackageId/$Version" `
            -OutFile $nupkg -UseBasicParsing
    } catch {
        Write-Host "ERROR: Could not download $PackageId. Check internet connection." -ForegroundColor Red
        Write-Host $_.Exception.Message
        Read-Host "Press Enter to exit"
        exit 1
    }

    Expand-Archive $nupkg -DestinationPath $extracted -Force

    # Try preferred subfolder first, then any match
    $dll = $null
    if ($PreferPath) {
        $dll = Get-ChildItem $extracted -Filter "*.dll" -Recurse |
               Where-Object { $_.FullName -match $PreferPath } |
               Select-Object -First 1
    }
    if ($null -eq $dll) {
        $dll = Get-ChildItem $extracted -Filter "*.dll" -Recurse |
               Where-Object { $_.FullName -match "net4" } |
               Select-Object -First 1
    }
    if ($null -eq $dll) {
        $dll = Get-ChildItem $extracted -Filter "*.dll" -Recurse | Select-Object -First 1
    }
    if ($null -eq $dll) {
        Write-Host "ERROR: DLL not found in $PackageId package." -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }

    Copy-Item $dll.FullName $DestDll
    Write-Host "    $PackageId downloaded OK." -ForegroundColor Green
}

# ── 2. Download dependencies ──────────────────────────────────────────────────
Write-Host "[1/3] Checking dependencies..." -ForegroundColor Yellow
Get-NugetDll -PackageId "PdfSharp"    -Version "1.50.5147" -DestDll $PdfSharpDll    -PreferPath "net4"
Get-NugetDll -PackageId "iTextSharp"  -Version "5.5.13.3"  -DestDll $iTextSharpDll  -PreferPath "net40"
Get-NugetDll -PackageId "BouncyCastle" -Version "1.8.9"    -DestDll $BouncyCastleDll -PreferPath "net40"

# ── 3. Compile ────────────────────────────────────────────────────────────────
Write-Host "[2/3] Compiling..." -ForegroundColor Yellow

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    Write-Host "ERROR: csc.exe not found. Is .NET Framework 4.x installed?" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

$sources = @(
    "$ProjectDir\Program.cs",
    "$ProjectDir\MainForm.cs",
    "$ProjectDir\MainForm.Designer.cs",
    "$ProjectDir\Properties\AssemblyInfo.cs",
    "$ProjectDir\Properties\Settings.Designer.cs",
    "$ProjectDir\Properties\Resources.Designer.cs"
)

$refs = @(
    "System.dll",
    "System.Core.dll",
    "System.Windows.Forms.dll",
    "System.Drawing.dll",
    "System.Configuration.dll",
    "System.Xml.dll",
    "Microsoft.CSharp.dll",
    "`"$PdfSharpDll`"",
    "`"$iTextSharpDll`"",
    "`"$BouncyCastleDll`""
)

$refArgs  = $refs | ForEach-Object { "/r:$_" }
$srcArgs  = $sources | ForEach-Object { "`"$_`"" }
$outExe   = "`"$DistDir\PdfMerger.exe`""

$compileArgs = @("/out:$outExe", "/target:winexe", "/platform:anycpu", "/optimize+") + $refArgs + $srcArgs

$proc = Start-Process -FilePath $csc `
    -ArgumentList $compileArgs `
    -NoNewWindow -Wait -PassThru `
    -RedirectStandardError "$DistDir\_errors.txt" `
    -RedirectStandardOutput "$DistDir\_output.txt"

$errText = Get-Content "$DistDir\_errors.txt" -Raw 2>$null
$outText = Get-Content "$DistDir\_output.txt" -Raw 2>$null
Remove-Item "$DistDir\_errors.txt","$DistDir\_output.txt" -ErrorAction SilentlyContinue

if ($proc.ExitCode -ne 0) {
    Write-Host "ERROR: Compilation failed!" -ForegroundColor Red
    if ($errText) { Write-Host $errText }
    if ($outText) { Write-Host $outText }
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "    Compiled OK." -ForegroundColor Green

# ── 4. Copy runtime files ─────────────────────────────────────────────────────
Copy-Item $PdfSharpDll    "$DistDir\PdfSharp.dll"
Copy-Item $iTextSharpDll  "$DistDir\itextsharp.dll"
Copy-Item $BouncyCastleDll "$DistDir\BouncyCastle.Crypto.dll"
Copy-Item "$ProjectDir\app.config" "$DistDir\PdfMerger.exe.config"

# ── 5. Done ───────────────────────────────────────────────────────────────────
Write-Host "[3/3] Done!" -ForegroundColor Green
Write-Host ""
Write-Host "Output folder: $DistDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Files to distribute (copy these 4 files):" -ForegroundColor White
Write-Host "  - PdfMerger.exe" -ForegroundColor White
Write-Host "  - PdfSharp.dll" -ForegroundColor White
Write-Host "  - itextsharp.dll" -ForegroundColor White
Write-Host "  - BouncyCastle.Crypto.dll" -ForegroundColor White
Write-Host "  - PdfMerger.exe.config" -ForegroundColor White
Write-Host ""

Start-Process explorer.exe $DistDir
Read-Host "Press Enter to close"

#Requires -Version 3.0
$ErrorActionPreference = "Stop"

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "PdfMerger"
$DistDir    = Join-Path $ScriptDir "dist"
$PkgDir     = Join-Path $ScriptDir ".packages"
$DllPath    = Join-Path $PkgDir "PdfSharp.dll"

Write-Host ""
Write-Host "=== PDF Merger - Build Script ===" -ForegroundColor Cyan
Write-Host ""

# ── 1. Create output folder ───────────────────────────────────────────────────
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Path $DistDir | Out-Null

# ── 2. Download PdfSharp (once) ───────────────────────────────────────────────
if (-not (Test-Path $DllPath)) {
    Write-Host "[1/3] Downloading PdfSharp..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $PkgDir -Force | Out-Null

    $nupkg = Join-Path $PkgDir "pdfsharp.zip"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest "https://www.nuget.org/api/v2/package/PdfSharp/1.50.5147" `
            -OutFile $nupkg -UseBasicParsing
    } catch {
        Write-Host "ERROR: Could not download PdfSharp. Check internet connection." -ForegroundColor Red
        Write-Host $_.Exception.Message
        Read-Host "Press Enter to exit"
        exit 1
    }

    $extracted = Join-Path $PkgDir "pdfsharp_pkg"
    Expand-Archive $nupkg -DestinationPath $extracted -Force

    # Prefer net40 build (compatible with .NET Framework 4.x)
    $dll = Get-ChildItem $extracted -Filter "PdfSharp.dll" -Recurse |
           Where-Object { $_.FullName -match "net4" } |
           Select-Object -First 1

    if ($null -eq $dll) {
        $dll = Get-ChildItem $extracted -Filter "PdfSharp.dll" -Recurse | Select-Object -First 1
    }

    if ($null -eq $dll) {
        Write-Host "ERROR: PdfSharp.dll not found in downloaded package." -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }

    Copy-Item $dll.FullName $DllPath
    Write-Host "    PdfSharp downloaded OK." -ForegroundColor Green
} else {
    Write-Host "[1/3] PdfSharp already cached." -ForegroundColor Green
}

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
    "`"$DllPath`""
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
Copy-Item $DllPath "$DistDir\PdfSharp.dll"
Copy-Item "$ProjectDir\app.config" "$DistDir\PdfMerger.exe.config"

# ── 5. Done ───────────────────────────────────────────────────────────────────
Write-Host "[3/3] Done!" -ForegroundColor Green
Write-Host ""
Write-Host "Output folder: $DistDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Files to distribute (copy these 3 files):" -ForegroundColor White
Write-Host "  - PdfMerger.exe" -ForegroundColor White
Write-Host "  - PdfSharp.dll" -ForegroundColor White
Write-Host "  - PdfMerger.exe.config" -ForegroundColor White
Write-Host ""

Start-Process explorer.exe $DistDir
Read-Host "Press Enter to close"

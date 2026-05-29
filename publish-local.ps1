<#
.SYNOPSIS
    Publishes sqlci as single-file, self-contained executables for
    Windows, Linux, and macOS (x64).

.DESCRIPTION
    Builds reliable single-file self-contained executables for all supported
    database providers (SqlServer, PostgreSql, Sqlite).

    Native AOT + trimming has been removed for now because:
      - It requires extra build tools on Windows (Visual C++ linker).
      - It is fragile with Spectre.Console.Cli (heavy reflection).
      - It frequently breaks Microsoft.Data.Sqlite / SQLitePCLRaw native interop
        even when native libraries are embedded.

    The current mode produces larger but dependable binaries that work
    correctly with all database backends.

    Output is written to ./artifacts/
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

$root = $PSScriptRoot
$project = Join-Path $root "src/SqlCi.Cli/SqlCi.Cli.csproj"
$artifacts = Join-Path $root "artifacts"

Write-Information "SqlCi Cross-Platform Publisher"
Write-Information "=============================="

# Clean previous artifacts
if (Test-Path $artifacts) {
    Write-Information "Cleaning previous artifacts..."
    Remove-Item $artifacts -Recurse -Force
}
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

$rids = @(
    @{ Rid = "win-x64";   Ext = ".exe"; Label = "Windows x64"   },
    @{ Rid = "linux-x64"; Ext = "";     Label = "Linux x64"     },
    @{ Rid = "osx-x64";   Ext = "";     Label = "macOS x64"     }
)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

foreach ($target in $rids) {
    $rid = $target.Rid
    $ext = $target.Ext
    $label = $target.Label

    $outputDir = Join-Path $artifacts "sqlci-$rid"
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    Write-Information ""
    Write-Information ">>> Publishing for $label ($rid)  [Single-file, self-contained, no AOT/trimming]"

    # We deliberately avoid Native AOT and trimming for reliability:
    # - Spectre.Console.Cli uses a lot of reflection
    # - Microsoft.Data.Sqlite + SQLitePCLRaw native interop is fragile under trimming/AOT
    #
    # IncludeNativeLibrariesForSelfExtract ensures the native SQLite library
    # (e_sqlite3) is embedded inside the single-file exe for the target RID.
    $publishArgs = @(
        '-p:PublishAot=false',
        '-p:PublishTrimmed=false',
        '-p:IncludeNativeLibrariesForSelfExtract=true'
    )

    # Note: We intentionally do NOT use --no-restore here when publishing multiple RIDs,
    # because cross-RID publishing (e.g. linux-x64 from Windows) requires restore data for that RID.
    $publishOutput = dotnet publish $project `
        -c Release `
        -r $rid `
        -o $outputDir `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -p:InvariantGlobalization=true `
        -p:StripSymbols=true `
        $publishArgs 2>&1

    $publishExitCode = $LASTEXITCODE

    if ($publishExitCode -ne 0) {
        Write-Warning "dotnet publish failed for $rid. See output above for details."
        $publishOutput | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
        continue
    }

    $exeName = "sqlci$ext"
    $exePath = Join-Path $outputDir $exeName

    if (Test-Path $exePath) {
        $sizeBytes = (Get-Item $exePath).Length
        $sizeMB = [math]::Round($sizeBytes / 1MB, 2)
        Write-Information "    ✅ $exePath"
        Write-Information "       Size: $sizeMB MB"

        # Clean up the output folder so it contains ONLY the executable.
        # With IncludeNativeLibrariesForSelfExtract=true the native SQLite bits (etc.)
        # are embedded inside the single-file .exe and extracted on first run, so it is
        # safe (and desirable) to delete the loose runtimes/ and native files here.
        Get-ChildItem -Path $outputDir -Force |
            Where-Object { $_.FullName -ne $exePath } |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

        Write-Information "       (Output folder cleaned — only the executable remains)"
    } else {
        Write-Warning "Expected executable not found at $exePath"
    }
}

$stopwatch.Stop()

Write-Information ""
Write-Information "========================================"
Write-Information "All builds completed in $($stopwatch.Elapsed.ToString('mm\:ss'))"
Write-Information "Artifacts location: $artifacts"
Write-Information ""
Write-Information "Test commands (from repo root):"
Write-Information "  .\artifacts\sqlci-win-x64\sqlci.exe --version"
Write-Information ""
Write-Information "  # Linux / macOS (after copying the binary):"
Write-Information "  chmod +x sqlci"
Write-Information "  ./sqlci --version"
Write-Information ""
Write-Information "NOTE:"
Write-Information "  - Native AOT + trimming has been removed for now (too fragile with Spectre.Console.Cli"
Write-Information "    and Microsoft.Data.Sqlite / SQLite native interop)."
Write-Information "  - The resulting binaries are larger but reliable across all database providers."
Write-Information "  - IncludeNativeLibrariesForSelfExtract ensures the native SQLite library is embedded"
Write-Information "    inside the single-file executable."
Write-Information "  - Always test the final binaries, especially anything involving database connections."
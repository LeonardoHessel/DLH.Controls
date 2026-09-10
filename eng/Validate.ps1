param([string]$PackageVersion)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    New-Item -ItemType Directory -Path artifacts/test-results,artifacts/packages -Force | Out-Null
    function Invoke-DotNet([string]$Log, [string[]]$Arguments) {
        & dotnet @Arguments 2>&1 | Tee-Object -FilePath "artifacts/test-results/$Log.log"
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $Log (exit $LASTEXITCODE)" }
    }
    $versionArgs = @()
    if ($PackageVersion) {
        $checkedVersion = & "$PSScriptRoot/Get-ReleaseVersion.ps1" -Tag ("v" + $PackageVersion)
        $versionArgs = @("-p:Version=$checkedVersion", "-p:PackageVersion=$checkedVersion")
    }
    Invoke-DotNet 'restore' @('restore','DLH.Controls.sln')
    Invoke-DotNet 'build' (@('build','DLH.Controls.sln','-c','Release','--no-restore') + $versionArgs)
    $project = 'tests/DLH.Controls.Wpf.Tests'
    $preview = Join-Path $repo 'artifacts/test-results/preview.png'
    Invoke-DotNet 'integration' @('run','--project',$project,'-c','Release','--no-build','--',$preview)
    Invoke-DotNet 'settings' @('run','--project',$project,'-c','Release','--no-build','--','--settings-only')
    Invoke-DotNet 'configuration' @('run','--project',$project,'-c','Release','--no-build','--','--configuration-only')
    Invoke-DotNet 'pack' (@('pack','src/DLH.Controls.Wpf','-c','Release','--no-build','--no-restore','-o','artifacts/packages') + $versionArgs)
    $packages = @(Get-ChildItem artifacts/packages -Filter '*.nupkg' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
    if ($packages.Count -eq 0) { throw 'No package generated.' }
    foreach ($package in $packages) {
        $zip = [IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            foreach ($name in @('LICENSE.txt','README.md','DLH.Controls.Wpf.nuspec')) {
                if ($null -eq $zip.GetEntry($name)) { throw "Package missing $name" }
            }
            if (!($zip.Entries.FullName -match '^lib/.*/DLH.Controls.Wpf.dll$')) { throw 'Library missing from package' }
            $readmeEntry = $zip.GetEntry('README.md')
            $reader = [IO.StreamReader]::new($readmeEntry.Open())
            try { $packageReadme = $reader.ReadToEnd() } finally { $reader.Dispose() }
            foreach ($instruction in @('paket add DLH.Controls.Wpf','paket.dependencies','paket.references','paket install')) {
                if (!$packageReadme.Contains($instruction, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Package README missing Paket instruction: $instruction"
                }
            }
            foreach ($image in @('custom-tab-control.png','data-grid-view.png')) {
                if (!$packageReadme.Contains("https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/$image", [StringComparison]::Ordinal)) {
                    throw "Package README missing NuGet-compatible image URL: $image"
                }
                if (!(Test-Path (Join-Path $repo "docs/images/$image"))) {
                    throw "Repository missing README image: $image"
                }
            }
        } finally { $zip.Dispose() }
    }
} finally { Pop-Location }

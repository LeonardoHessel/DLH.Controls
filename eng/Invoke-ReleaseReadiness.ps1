param(
    [string]$PackageVersion,
    [switch]$SkipValidation,
    [switch]$CheckPublicPackage,
    [switch]$Interactive,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$IsWindows) { throw 'A homologação de release exige Windows.' }

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repo 'artifacts/release-readiness'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $repo 'artifacts/test-results'),(Join-Path $repo 'artifacts/packages') -Force | Out-Null

[xml]$versionDocument = Get-Content -LiteralPath (Join-Path $repo 'eng/Version.props')
$repositoryVersion = [string]$versionDocument.Project.PropertyGroup.DlhControlsVersion
if ([string]::IsNullOrWhiteSpace($repositoryVersion)) { throw 'eng/Version.props não define DlhControlsVersion.' }
if ([string]::IsNullOrWhiteSpace($PackageVersion)) { $PackageVersion = $repositoryVersion }
$PackageVersion = & (Join-Path $PSScriptRoot 'Get-ReleaseVersion.ps1') -Tag ('v' + $PackageVersion)
if ($PackageVersion -ne $repositoryVersion) {
    throw "A versão solicitada '$PackageVersion' difere de eng/Version.props ('$repositoryVersion')."
}

$startedAt = Get-Date
$automated = [ordered]@{}

function Get-PackageFingerprint([string]$Path) {
    $archiveHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    $zip = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $library = $zip.Entries | Where-Object FullName -match '^lib/.*/DLH.Controls.Wpf.dll$' | Select-Object -First 1
        if ($null -eq $library) { throw "DLL ausente em $Path" }
        $stream = $library.Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $libraryHash = [Convert]::ToHexString($sha.ComputeHash($stream)).ToLowerInvariant() }
        finally { $sha.Dispose(); $stream.Dispose() }
        return [ordered]@{ archiveSha256 = $archiveHash; librarySha256 = $libraryHash }
    } finally { $zip.Dispose() }
}

if (!$SkipValidation) {
    & (Join-Path $PSScriptRoot 'Validate.ps1') -PackageVersion $PackageVersion
    if ($LASTEXITCODE -ne 0) { throw "A validação automática falhou com código $LASTEXITCODE." }
    $automated.localValidation = [ordered]@{ status = 'passed'; evidence = 'eng/Validate.ps1 concluído' }
} else {
    $automated.localValidation = [ordered]@{ status = 'observed'; evidence = 'validação reutilizada de artifacts' }
}

$packagePath = Join-Path $repo "artifacts/packages/DLH.Controls.Wpf.$PackageVersion.nupkg"
if (!(Test-Path -LiteralPath $packagePath)) { throw "Pacote local não encontrado: $packagePath" }
$localFingerprint = Get-PackageFingerprint $packagePath
$automated.package = [ordered]@{
    status = 'passed'
    evidence = $packagePath
    sha256 = $localFingerprint.archiveSha256
    librarySha256 = $localFingerprint.librarySha256
}

$trxPath = Join-Path $repo 'artifacts/test-results/automated.trx'
$testSummary = $null
if (Test-Path -LiteralPath $trxPath) {
    [xml]$trx = Get-Content -LiteralPath $trxPath
    $counters = $trx.TestRun.ResultSummary.Counters
    $testSummary = [ordered]@{
        total = [int]$counters.total
        executed = [int]$counters.executed
        passed = [int]$counters.passed
        failed = [int]$counters.failed
    }
    $automated.tests = [ordered]@{
        status = if ($testSummary.failed -eq 0 -and $testSummary.executed -gt 0) { 'passed' } else { 'failed' }
        evidence = "$($testSummary.passed)/$($testSummary.executed) testes aprovados"
    }
}

$consumerReport = Join-Path $repo 'artifacts/test-results/package-consumers.json'
if (Test-Path -LiteralPath $consumerReport) {
    Copy-Item -LiteralPath $consumerReport -Destination (Join-Path $OutputDirectory 'local-package-consumers.json') -Force
    $automated.localPackageConsumption = [ordered]@{ status = 'passed'; evidence = 'local-package-consumers.json' }
} else {
    $automated.localPackageConsumption = [ordered]@{ status = 'failed'; evidence = 'relatório de consumo ausente' }
}

if ($CheckPublicPackage) {
    $publicDirectory = Join-Path $repo 'artifacts/public-package'
    New-Item -ItemType Directory -Path $publicDirectory -Force | Out-Null
    $publicPackage = Join-Path $publicDirectory "DLH.Controls.Wpf.$PackageVersion.nupkg"
    $publicUrl = "https://api.nuget.org/v3-flatcontainer/dlh.controls.wpf/$($PackageVersion.ToLowerInvariant())/dlh.controls.wpf.$($PackageVersion.ToLowerInvariant()).nupkg"
    $downloaded = $false
    for ($attempt = 1; $attempt -le 12 -and !$downloaded; $attempt++) {
        try {
            Invoke-WebRequest -Uri $publicUrl -OutFile $publicPackage -Headers @{ 'User-Agent' = 'DLH.Controls-release-readiness' }
            $downloaded = $true
        } catch {
            if ($attempt -eq 12) { throw }
            Start-Sleep -Seconds 10
        }
    }
    & (Join-Path $PSScriptRoot 'Test-PackageConsumer.ps1') -PackagePath $publicPackage 2>&1 |
        Tee-Object -FilePath (Join-Path $OutputDirectory 'public-package-consumers.log')
    if ($LASTEXITCODE -ne 0) { throw "O consumo do pacote público falhou com código $LASTEXITCODE." }
    Copy-Item -LiteralPath $consumerReport -Destination (Join-Path $OutputDirectory 'public-package-consumers.json') -Force
    $publicFingerprint = Get-PackageFingerprint $publicPackage
    $automated.publicPackageConsumption = [ordered]@{
        status = 'passed'
        evidence = $publicUrl
        sha256 = $publicFingerprint.archiveSha256
        librarySha256 = $publicFingerprint.librarySha256
        libraryMatchesLocal = $publicFingerprint.librarySha256 -eq $localFingerprint.librarySha256
    }
} else {
    $automated.publicPackageConsumption = [ordered]@{ status = 'not-requested'; evidence = $null }
}

if (!('ReleaseReadinessProbe' -as [type])) {
    Add-Type @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static class ReleaseReadinessProbe
{
    public sealed class DisplayInfo
    {
        public string DeviceName { get; set; } = "";
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public uint DpiX { get; set; }
        public uint DpiY { get; set; }
        public bool Primary { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
    }
    [StructLayout(LayoutKind.Sequential)] private struct HIGHCONTRAST
    {
        public int cbSize;
        public int dwFlags;
        public IntPtr lpszDefaultScheme;
    }
    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr dc, ref RECT rect, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFOEX info);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SystemParametersInfo(uint action, uint parameter, ref HIGHCONTRAST value, uint update);

    public static DisplayInfo[] GetDisplays()
    {
        var result = new List<DisplayInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr monitor, IntPtr dc, ref RECT rect, IntPtr data)
        {
            var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>(), szDevice = "" };
            if (!GetMonitorInfo(monitor, ref info)) return true;
            uint x = 96, y = 96;
            try { GetDpiForMonitor(monitor, 0, out x, out y); } catch { }
            result.Add(new DisplayInfo
            {
                DeviceName = info.szDevice,
                Left = info.rcMonitor.Left,
                Top = info.rcMonitor.Top,
                Width = info.rcMonitor.Right - info.rcMonitor.Left,
                Height = info.rcMonitor.Bottom - info.rcMonitor.Top,
                DpiX = x,
                DpiY = y,
                Primary = (info.dwFlags & 1) != 0
            });
            return true;
        }, IntPtr.Zero);
        return result.ToArray();
    }

    public static bool IsHighContrast()
    {
        var value = new HIGHCONTRAST { cbSize = Marshal.SizeOf<HIGHCONTRAST>() };
        return SystemParametersInfo(0x0042, (uint)value.cbSize, ref value, 0) && (value.dwFlags & 1) != 0;
    }
}
'@
}

$displays = @([ReleaseReadinessProbe]::GetDisplays() | ForEach-Object {
    [ordered]@{
        device = $_.DeviceName
        primary = $_.Primary
        bounds = [ordered]@{ left = $_.Left; top = $_.Top; width = $_.Width; height = $_.Height }
        dpi = [ordered]@{ x = $_.DpiX; y = $_.DpiY; scalePercent = [math]::Round(($_.DpiX / 96) * 100) }
    }
})
$requiredScales = @(100,125,150,200)
$observedScales = @($displays | ForEach-Object { [int]$_.dpi.scalePercent } | Sort-Object -Unique)
$missingScales = @($requiredScales | Where-Object { $_ -notin $observedScales })

$manualDefinitions = @(
    [ordered]@{ id = 'physical-mouse'; title = 'Mouse físico e captura'; steps = @('Arrastar nas quatro posições', 'Soltar fora da janela', 'Cancelar com Esc', 'Usar Alt+Tab durante o arraste', 'Confirmar ausência de cursor ou prévia presos') },
    [ordered]@{ id = 'real-dpi'; title = 'DPI e múltiplos monitores'; steps = @('Validar em 100%, 125%, 150% e 200%', 'Mover a janela entre monitores', 'Conferir contornos, popups, barras e áreas fixadas') },
    [ordered]@{ id = 'keyboard'; title = 'Teclado e foco'; steps = @('Percorrer com Tab e Shift+Tab', 'Usar setas e Ctrl+Tab', 'Reordenar com Ctrl+Shift+setas', 'Confirmar foco visível e ausência de aprisionamento') },
    [ordered]@{ id = 'screen-reader'; title = 'Leitor de tela'; steps = @('Conferir nomes e seleção das abas', 'Conferir anúncio de reordenação', 'Fechar abas selecionadas e não selecionadas', 'Confirmar indisponibilidade quando o fechamento estiver bloqueado') },
    [ordered]@{ id = 'high-contrast'; title = 'Alto contraste'; steps = @('Ativar o modo de alto contraste do Windows', 'Repetir navegação e seleção', 'Conferir foco, contornos, texto, ícones e estados desabilitados') }
)

$manual = @()
$demoProcess = $null
if ($Interactive) {
    $demo = Join-Path $repo 'samples/DLH.Controls.Wpf.Demo/bin/Release/net10.0-windows/DLH.Controls.Wpf.Demo.exe'
    if (Test-Path -LiteralPath $demo) { $demoProcess = Start-Process -FilePath $demo -PassThru }
    Write-Host ''
    Write-Host 'Homologação assistida: execute cada grupo no aplicativo aberto.' -ForegroundColor Cyan
}

foreach ($definition in $manualDefinitions) {
    $status = 'pending'
    $notes = ''
    if ($Interactive) {
        Write-Host ''
        Write-Host $definition.title -ForegroundColor Yellow
        foreach ($step in $definition.steps) { Write-Host " - $step" }
        do { $answer = (Read-Host 'Resultado: [A]provado, [F]alhou ou [B]loqueado').Trim().ToUpperInvariant() } while ($answer -notin @('A','F','B'))
        $status = switch ($answer) { 'A' { 'passed' } 'F' { 'failed' } default { 'blocked' } }
        $notes = Read-Host 'Observações (opcional)'
    }
    $manual += [ordered]@{ id = $definition.id; title = $definition.title; status = $status; notes = $notes; steps = $definition.steps }
}

if ($demoProcess -and !$demoProcess.HasExited) { Stop-Process -Id $demoProcess.Id }

$automaticFailed = @($automated.Values | Where-Object status -eq 'failed').Count -gt 0
$manualFailed = @($manual | Where-Object status -eq 'failed').Count -gt 0
$manualComplete = @($manual | Where-Object status -ne 'passed').Count -eq 0
$overall = if ($automaticFailed -or $manualFailed) { 'failed' } elseif ($manualComplete) { 'approved' } else { 'automated-approved-manual-pending' }

$os = Get-CimInstance Win32_OperatingSystem
$report = [ordered]@{
    schemaVersion = 1
    packageVersion = $PackageVersion
    startedAt = $startedAt.ToUniversalTime().ToString('o')
    completedAt = (Get-Date).ToUniversalTime().ToString('o')
    overallStatus = $overall
    interactive = [bool]$Interactive
    environment = [ordered]@{
        os = $os.Caption
        osVersion = $os.Version
        dotnetSdk = (& dotnet --version)
        culture = [Globalization.CultureInfo]::CurrentCulture.Name
        uiCulture = [Globalization.CultureInfo]::CurrentUICulture.Name
        highContrast = [ReleaseReadinessProbe]::IsHighContrast()
        displays = $displays
        dpiCoverage = [ordered]@{
            requiredScales = $requiredScales
            observedScales = $observedScales
            missingScales = $missingScales
        }
    }
    automated = $automated
    testSummary = $testSummary
    manual = $manual
}

$jsonPath = Join-Path $OutputDirectory 'release-readiness.json'
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$lines = [Collections.Generic.List[string]]::new()
$lines.Add("# Homologação do DLH.Controls.Wpf $PackageVersion")
$lines.Add('')
$lines.Add("- Resultado: **$overall**")
$lines.Add("- Execução: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))")
$lines.Add("- Windows: $($os.Caption) $($os.Version)")
$lines.Add("- .NET SDK: $(& dotnet --version)")
$lines.Add("- Alto contraste ativo durante a coleta: $([ReleaseReadinessProbe]::IsHighContrast())")
$lines.Add('')
$lines.Add('## Evidências automáticas')
$lines.Add('')
$lines.Add('| Validação | Estado | Evidência | SHA-256 do pacote | SHA-256 da DLL | DLL igual à local |')
$lines.Add('|---|---|---|---|---|---:|')
foreach ($entry in $automated.GetEnumerator()) {
    $hash = if ($entry.Value.Contains('sha256')) { $entry.Value.sha256 } else { '' }
    $libraryHash = if ($entry.Value.Contains('librarySha256')) { $entry.Value.librarySha256 } else { '' }
    $matches = if ($entry.Value.Contains('libraryMatchesLocal')) { $entry.Value.libraryMatchesLocal } else { '' }
    $lines.Add("| $($entry.Key) | $($entry.Value.status) | $($entry.Value.evidence) | $hash | $libraryHash | $matches |")
}
$lines.Add('')
$lines.Add('Os hashes do arquivo e da DLL são evidências de rastreabilidade. Diferenças entre a compilação local e a remota podem ocorrer enquanto o projeto não exigir builds binariamente reproduzíveis; a aprovação depende das duas matrizes de consumo.')
$lines.Add('')
$lines.Add('## Monitores detectados')
$lines.Add('')
$lines.Add('| Dispositivo | Primário | Resolução | DPI | Escala |')
$lines.Add('|---|---:|---:|---:|---:|')
foreach ($display in $displays) { $lines.Add("| $($display.device) | $($display.primary) | $($display.bounds.width)x$($display.bounds.height) | $($display.dpi.x)x$($display.dpi.y) | $($display.dpi.scalePercent)% |") }
$lines.Add('')
$lines.Add("Escalas requeridas: $($requiredScales -join '%, ')%.")
$lines.Add("Escalas observadas: $($observedScales -join '%, ')%.")
$lines.Add("Escalas ainda não observadas: $(if ($missingScales.Count) { ($missingScales -join '%, ') + '%' } else { 'nenhuma' }).")
$lines.Add('')
$lines.Add('## Verificações assistidas')
$lines.Add('')
foreach ($item in $manual) {
    $lines.Add("### $($item.title) — $($item.status)")
    $lines.Add('')
    foreach ($step in $item.steps) { $lines.Add("- $step") }
    if ($item.notes) { $lines.Add(""); $lines.Add("Observações: $($item.notes)") }
    $lines.Add('')
}
$lines.Add('> Testes simulados não substituem mouse físico, DPI real, alto contraste ou a avaliação auditiva de um leitor de tela.')
$markdownPath = Join-Path $OutputDirectory 'release-readiness.md'
$lines | Set-Content -LiteralPath $markdownPath -Encoding utf8

Write-Host "Relatório JSON: $jsonPath"
Write-Host "Relatório Markdown: $markdownPath"
Write-Host "Resultado geral: $overall"
if ($automaticFailed -or $manualFailed) { exit 2 }

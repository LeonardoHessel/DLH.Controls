param([Parameter(Mandatory)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$packageDirectory = Split-Path $package -Parent
$workspace = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts/package-consumer'))
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts'))
if (!$workspace.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Diretório consumidor fora de artifacts.' }
if (Test-Path -LiteralPath $workspace) { Remove-Item -LiteralPath $workspace -Recurse -Force }
New-Item -ItemType Directory -Path $workspace -Force | Out-Null

$zip = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entry = $zip.GetEntry('DLH.Controls.Wpf.nuspec')
    if ($null -eq $entry) { throw 'Nuspec ausente no pacote.' }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $version = [string]$nuspec.package.metadata.version
} finally { $zip.Dispose() }
if ([string]::IsNullOrWhiteSpace($version)) { throw 'Versão ausente no pacote.' }

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWPF>true</UseWPF><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><PackageReference Include="DLH.Controls.Wpf" Version="$version" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $workspace 'Consumer.csproj') -Encoding utf8

@'
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DLH.Controls.Wpf;

internal static class Program
{
    private sealed record Row(string Name, int Quantity);

    [STAThread]
    private static int Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/DLH.Controls.Wpf;component/Themes/Generic.xaml", UriKind.Relative)
        });
        var rows = new ObservableCollection<Row> { new("Produto A", 2), new("Produto B", 5) };
        var grid = new DataGridView { ItemsSource = rows, SelectionBehavior = DataGridViewSelectionBehavior.Row };
        grid.Columns.Add(new DataGridTextColumn { Header = "Nome", Binding = new Binding(nameof(Row.Name)), SortMemberPath = nameof(Row.Name) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Quantidade", Binding = new Binding(nameof(Row.Quantity)), SortMemberPath = nameof(Row.Quantity) });
        grid.SetFilter(grid.Columns[1], "1", DataGridViewFilterOperator.GreaterThan);
        using var csv = new StringWriter();
        grid.ExportCsv(csv);
        if (!csv.ToString().Contains("Produto A") || grid.CaptureState().Columns.Count != 2) return 10;

        var tabs = new CustomTabControl { ItemsSource = new ObservableCollection<string> { "Geral", "Estoque" } };
        tabs.Measure(new Size(500, 300)); tabs.Arrange(new Rect(0, 0, 500, 300)); tabs.ApplyTemplate();
        if (tabs.Items.Count != 2 || tabs.CornerRadius.TopLeft <= 0) return 11;
        app.Shutdown();
        return 0;
    }
}
'@ | Set-Content (Join-Path $workspace 'Program.cs') -Encoding utf8

& dotnet restore (Join-Path $workspace 'Consumer.csproj') --source $packageDirectory
if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar o pacote na aplicação consumidora.' }
& dotnet run --project (Join-Path $workspace 'Consumer.csproj') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Aplicação consumidora falhou com código $LASTEXITCODE." }
Write-Host "PASS: pacote $version instalado e executado em aplicação WPF isolada."

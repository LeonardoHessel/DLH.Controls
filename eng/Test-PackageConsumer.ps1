param([Parameter(Mandatory)][string]$PackagePath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$packageDirectory = Split-Path $package -Parent
$workspace = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts/package-consumers'))
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts'))
$reportPath = Join-Path $repo 'artifacts/test-results/package-consumers.json'
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

function New-ConsumerProject([string]$directory, [string]$startupObject = '') {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $startupProperty = if ($startupObject) { "<StartupObject>$startupObject</StartupObject>" } else { '' }
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    $startupProperty
  </PropertyGroup>
  <ItemGroup><PackageReference Include="DLH.Controls.Wpf" Version="$version" /></ItemGroup>
</Project>
"@ | Set-Content (Join-Path $directory 'Consumer.csproj') -Encoding utf8
}

$codeDirectory = Join-Path $workspace 'CodeConsumer'
New-ConsumerProject $codeDirectory
@'
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DLH.Controls.Wpf;
using PackageTabControl = DLH.Controls.Wpf.TabControl;
using PackageScrollBar = DLH.Controls.Wpf.ScrollBar;
using PackageContextMenu = DLH.Controls.Wpf.ContextMenu;

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
        var grid = new DataGridView
        {
            ItemsSource = rows,
            SelectionBehavior = DataGridViewSelectionBehavior.Row,
            CanPinColumns = true,
            CanPinRows = true
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "Nome", Binding = new Binding(nameof(Row.Name)), SortMemberPath = nameof(Row.Name) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Quantidade", Binding = new Binding(nameof(Row.Quantity)), SortMemberPath = nameof(Row.Quantity) });
        grid.SetFilter(grid.Columns[1], "1", DataGridViewFilterOperator.GreaterThan);
        using var csv = new StringWriter();
        grid.ExportCsv(csv);
        if (!csv.ToString().Contains("Produto A") || grid.CaptureState().Columns.Count != 2) return 10;

        var tabs = new PackageTabControl
        {
            ItemsSource = new ObservableCollection<string> { "Geral", "Estoque" },
            CanAddTabs = true,
            CanRenameTabs = true
        };
        tabs.Measure(new Size(500, 300));
        tabs.Arrange(new Rect(0, 0, 500, 300));
        tabs.ApplyTemplate();
        if (tabs.Items.Count != 2 || tabs.CornerRadius.TopLeft <= 0) return 11;

        var horizontal = new PackageScrollBar { Orientation = System.Windows.Controls.Orientation.Horizontal, Thickness = 8, CornerRadius = new CornerRadius(20) };
        var vertical = new PackageScrollBar { Orientation = System.Windows.Controls.Orientation.Vertical, Thickness = 12, CornerRadius = new CornerRadius(20) };
        if (horizontal.Thickness != 8 || horizontal.CornerRadius.TopLeft != 20 || vertical.Orientation != System.Windows.Controls.Orientation.Vertical) return 12;

        var menu = new PackageContextMenu { CornerRadius = new CornerRadius(7) };
        menu.Items.Add(new ToggleMenuItem { Header = "Exibir detalhes", IsChecked = true });
        menu.Items.Add(new ChoiceMenuItem
        {
            Header = "Tema",
            ItemsSource = new[] { "Claro", "Escuro" },
            SelectedIndex = 1
        });
        if (menu.Items.Count != 2 || menu.CornerRadius.TopLeft != 7) return 13;

        app.Shutdown();
        Console.WriteLine("PASS: APIs públicas exercitadas por uma aplicação programática isolada.");
        return 0;
    }
}
'@ | Set-Content (Join-Path $codeDirectory 'Program.cs') -Encoding utf8

$xamlDirectory = Join-Path $workspace 'XamlConsumer'
New-ConsumerProject $xamlDirectory 'PackageConsumer.Program'
@'
<Window x:Class="PackageConsumer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf"
        Title="Consumidor isolado" Width="820" Height="560"
        Left="-10000" Top="-10000" ShowInTaskbar="False" ShowActivated="False">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="150" />
            <RowDefinition Height="*" />
            <RowDefinition Height="90" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <controls:TabControl x:Name="Tabs" SelectedIndex="0" CanAddTabs="True" CanRenameTabs="True" CornerRadius="8">
            <controls:TabControlItem Header="Resumo"><TextBlock Text="Conteúdo carregado pelo XAML" /></controls:TabControlItem>
            <controls:TabControlItem Header="Detalhes"><TextBlock Text="Segunda aba" /></controls:TabControlItem>
        </controls:TabControl>

        <controls:DataGridView x:Name="RowsGrid" Grid.Row="1" AutoGenerateColumns="False"
                               SelectionBehavior="Row" CanPinColumns="True" CanPinRows="True"
                               RowKeyMemberPath="Name" CornerRadius="8">
            <controls:DataGridView.Columns>
                <DataGridTextColumn Header="Nome" Binding="{Binding Name}" SortMemberPath="Name" />
                <DataGridTextColumn Header="Quantidade" Binding="{Binding Quantity}" SortMemberPath="Quantity" />
            </controls:DataGridView.Columns>
        </controls:DataGridView>

        <Grid Grid.Row="2" Margin="0,10,0,0">
            <Grid.ColumnDefinitions><ColumnDefinition /><ColumnDefinition Width="40" /></Grid.ColumnDefinitions>
            <controls:ScrollBar x:Name="HorizontalBar" Orientation="Horizontal" Minimum="0" Maximum="100" Value="35"
                                ViewportSize="20" Thickness="10" VerticalAlignment="Bottom" />
            <controls:ScrollBar x:Name="VerticalBar" Grid.Column="1" Orientation="Vertical" Minimum="0" Maximum="100" Value="45"
                                ViewportSize="20" Thickness="10" HorizontalAlignment="Center" />
        </Grid>

        <Button x:Name="MenuButton" Grid.Row="3" Width="140" Height="32" HorizontalAlignment="Left" Content="Abrir opções">
            <Button.ContextMenu>
                <controls:ContextMenu CornerRadius="8" ItemPadding="10,6">
                    <controls:ToggleMenuItem Header="Exibir detalhes" IsChecked="True" />
                    <controls:ChoiceMenuItem Header="Tema" SelectedIndex="0">
                        <controls:ChoiceMenuOption Content="Claro" Value="Light" />
                        <controls:ChoiceMenuOption Content="Escuro" Value="Dark" />
                    </controls:ChoiceMenuItem>
                    <Separator />
                    <MenuItem Header="Exportar" />
                </controls:ContextMenu>
            </Button.ContextMenu>
        </Button>
    </Grid>
</Window>
'@ | Set-Content (Join-Path $xamlDirectory 'MainWindow.xaml') -Encoding utf8

@'
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DLH.Controls.Wpf;
using PackageContextMenu = DLH.Controls.Wpf.ContextMenu;

namespace PackageConsumer;

public sealed record Row(string Name, int Quantity);

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RowsGrid.ItemsSource = new ObservableCollection<Row>
        {
            new("Produto A", 2),
            new("Produto B", 5),
            new("Produto C", 8)
        };
    }

    public string? ValidateConsumer()
    {
        UpdateLayout();
        if (!IsLoaded || PresentationSource.FromVisual(this) is null) return "A janela XAML não foi conectada a uma fonte de apresentação.";
        if (ActualWidth <= 0 || ActualHeight <= 0) return "A janela XAML não recebeu dimensões reais.";
        Tabs.ApplyTemplate();
        RowsGrid.ApplyTemplate();
        HorizontalBar.ApplyTemplate();
        VerticalBar.ApplyTemplate();
        if (Tabs.Template is null || VisualTreeHelper.GetChildrenCount(Tabs) == 0 || Tabs.Items.Count != 2) return "TabControl não foi carregado pelo XAML.";
        if (RowsGrid.Template is null || VisualTreeHelper.GetChildrenCount(RowsGrid) == 0 || RowsGrid.Columns.Count != 2 || RowsGrid.Items.Count != 3) return "DataGridView não foi carregado pelo XAML.";
        if (HorizontalBar.Template is null || VerticalBar.Template is null || VisualTreeHelper.GetChildrenCount(HorizontalBar) == 0 || VisualTreeHelper.GetChildrenCount(VerticalBar) == 0) return "As barras de rolagem não aplicaram seus templates.";
        if (HorizontalBar.Orientation != System.Windows.Controls.Orientation.Horizontal || VerticalBar.Orientation != System.Windows.Controls.Orientation.Vertical) return "A orientação das barras de rolagem está incorreta.";

        if (MenuButton.ContextMenu is not PackageContextMenu menu || menu.Items.Count != 4) return "ContextMenu não foi carregado pelo XAML.";
        menu.PlacementTarget = MenuButton;
        menu.IsOpen = true;
        Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        if (!menu.IsOpen || PresentationSource.FromVisual(menu) is null) return "ContextMenu não abriu em uma superfície real.";
        menu.IsOpen = false;

        var dpi = VisualTreeHelper.GetDpi(this);
        Console.WriteLine($"PASS: janela XAML renderizada; escala DPI={dpi.DpiScaleX:0.00}x{dpi.DpiScaleY:0.00}.");
        return null;
    }
}

public static class Program
{
    [STAThread]
    public static int Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/DLH.Controls.Wpf;component/Themes/Generic.xaml", UriKind.Relative)
        });

        var window = new MainWindow();
        window.Show();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var error = window.ValidateConsumer();
        window.Close();
        app.Shutdown();
        if (error is null) return 0;
        Console.Error.WriteLine(error);
        return 20;
    }
}
'@ | Set-Content (Join-Path $xamlDirectory 'Program.cs') -Encoding utf8

$results = @()
foreach ($consumer in @(
    @{ Name = 'CodeConsumer'; Directory = $codeDirectory; Kind = 'programmatic' },
    @{ Name = 'XamlConsumer'; Directory = $xamlDirectory; Kind = 'rendered-xaml' }
)) {
    $project = Join-Path $consumer.Directory 'Consumer.csproj'
    $started = Get-Date
    & dotnet restore $project --source $packageDirectory
    if ($LASTEXITCODE -ne 0) { throw "Falha ao restaurar o pacote em $($consumer.Name)." }
    & dotnet build $project -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Falha ao compilar $($consumer.Name)." }
    & dotnet run --project $project -c Release --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "$($consumer.Name) falhou com código $LASTEXITCODE." }
    $results += [pscustomobject]@{
        name = $consumer.Name
        kind = $consumer.Kind
        status = 'passed'
        durationSeconds = [Math]::Round(((Get-Date) - $started).TotalSeconds, 2)
    }
}

$report = [pscustomobject]@{
    package = [IO.Path]::GetFileName($package)
    version = $version
    operatingSystem = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    dotnetSdk = (& dotnet --version)
    executedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    consumers = $results
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding utf8
Write-Host "PASS: pacote $version validado em $($results.Count) aplicações WPF isoladas. Relatório: $reportPath"

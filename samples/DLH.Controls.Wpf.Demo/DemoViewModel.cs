using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DLH.Controls.Wpf.Demo;

public sealed class DemoViewModel : INotifyPropertyChanged
{
    private int nextId = 4;
    private TabDocument? selectedTab;
    private TabDocument? comparisonSelectedTab;
    private bool english;
    private bool longHeaders;
    private double headerFontSize = 14;
    private ObservableCollection<TabDocument> tabs = new();
    public ObservableCollection<TabDocument> Tabs { get => tabs; private set { tabs = value; OnPropertyChanged(); } }
    public TabDocument? SelectedTab
    {
        get => selectedTab;
        set { selectedTab = value; OnPropertyChanged(); }
    }
    public TabDocument? ComparisonSelectedTab
    {
        get => comparisonSelectedTab;
        set { comparisonSelectedTab = value; OnPropertyChanged(); }
    }
    public ICommand AddTabCommand { get; }
    public ICommand RemoveTabCommand { get; }
    public ICommand CloseTabCommand { get; }
    public double HeaderFontSize { get => headerFontSize; set { headerFontSize = value; OnPropertyChanged(); } }
    public DemoViewModel()
    {
        Tabs.Add(new("Visão geral", "◈", "Uma superfície contínua", "As abas compartilham a cor do painel. Experimente o teclado, redimensione a janela e alterne o tema."));
        Tabs.Add(new("Editor", "✎", "Um espaço para experimentar", "O texto abaixo é armazenado no modelo e permanece disponível ao trocar de aba."));
        Tabs.Add(new("Indisponível", "○", "Aba desabilitada", "", false));
        SelectedTab = Tabs[0];
        ComparisonSelectedTab = Tabs[1];
        CloseTabCommand = new ParameterCommand(item => { if (item is TabDocument document) Tabs.Remove(document); }, item => item is TabDocument document && document.IsEnabled && Tabs.Contains(document));
        AddTabCommand = new RelayCommand(() =>
        {
            var item = new TabDocument($"Documento {nextId++}", "◇", "Novo documento", "Adicione mais abas para experimentar a rolagem horizontal.");
            Tabs.Add(item);
            SelectedTab = item;
            ComparisonSelectedTab = item;
        });
        RemoveTabCommand = new RelayCommand(() =>
        {
            if (SelectedTab is not { } item) return;
            var index = Tabs.IndexOf(item);
            Tabs.Remove(item);
            SelectedTab = Tabs.Skip(Math.Max(0, index)).Concat(Tabs.Take(Math.Max(0, index)).Reverse()).FirstOrDefault(tab => tab.IsEnabled);
        });
    }
    public void ToggleLanguage()
    {
        english = !english;
        ApplyHeaderVariants();
    }
    public string MutateCollection(int operation)
    {
        switch (operation % 5)
        {
            case 0:
                Tabs.Add(new TabDocument($"Documento {nextId++}", "◇", "Adicionado externamente", "A coleção notificou os dois controles."));
                return "Documento inserido";
            case 1 when Tabs.Count > 1:
                Tabs.RemoveAt(Tabs.Count - 1);
                return "Último documento removido";
            case 2 when Tabs.Count > 1:
                Tabs.Move(Tabs.Count - 1, 0);
                return "Último documento movido para o início";
            case 3:
                Tabs.Clear(); SelectedTab = null; ComparisonSelectedTab = null;
                return "Coleção limpa";
            default:
                ResetDocuments();
                return "Coleção substituída";
        }
    }
    public void ResetDocuments()
    {
        Tabs = new ObservableCollection<TabDocument>
        {
            new("Visão geral", "◈", "Uma superfície contínua", "As abas compartilham a cor do painel."),
            new("Editor", "✎", "Um espaço para experimentar", "O texto é armazenado no modelo."),
            new("Indisponível", "○", "Aba desabilitada", "", false)
        };
        SelectedTab = Tabs[0]; ComparisonSelectedTab = Tabs[1];
    }
    public void ToggleHeaderLength()
    {
        longHeaders = !longHeaders;
        ApplyHeaderVariants();
    }
    private void ApplyHeaderVariants()
    {
        foreach (var document in Tabs)
        {
            var baseName = document.Id switch
            {
                "Visão geral" => english ? "Overview" : "Visão geral",
                "Editor" => english ? "Editor" : "Editor",
                "Indisponível" => english ? "Unavailable" : "Indisponível",
                _ => english ? document.Id.Replace("Documento", "Document") : document.Id
            };
            document.Header = longHeaders ? (english ? $"{baseName} — runtime header example" : $"{baseName} — exemplo de título em execução") : baseName;
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class TabDocument(string header, string icon, string title, string description, bool isEnabled = true) : INotifyPropertyChanged
{
    public string Id { get; } = header;
    private string currentHeader = header;
    public string Header { get => currentHeader; set { if (currentHeader == value) return; currentHeader = value; PropertyChanged?.Invoke(this, new(nameof(Header))); } }
    public string Icon { get; } = icon;
    public string Title { get; } = title;
    public string Description { get; } = description;
    public bool IsEnabled { get; } = isEnabled;
    private string notes = "";
    private bool isDetailsExpanded;
    public IReadOnlyList<string> ExampleLines { get; } = Enumerable.Range(1, 24).Select(number => $"Linha sintética {number:00}").ToArray();
    public bool IsDetailsExpanded
    {
        get => isDetailsExpanded;
        set { if (isDetailsExpanded == value) return; isDetailsExpanded = value; PropertyChanged?.Invoke(this, new(nameof(IsDetailsExpanded))); }
    }
    public string Notes
    {
        get => notes;
        set { notes = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Notes))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class RelayCommand(Action execute) : ICommand
{
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
    public event EventHandler? CanExecuteChanged { add { } remove { } }
}

public sealed class ParameterCommand(Action<object?> execute, Predicate<object?> canExecute) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute(parameter);
    public void Execute(object? parameter) => execute(parameter);
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

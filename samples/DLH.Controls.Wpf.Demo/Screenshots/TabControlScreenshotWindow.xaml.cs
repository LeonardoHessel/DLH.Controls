using System.Windows;

namespace DLH.Controls.Wpf.Demo.Screenshots;

public partial class TabControlScreenshotWindow : Window
{
    public TabControlScreenshotWindow() => InitializeComponent();
}

public sealed record PageContentModel(string Title, string Description);

public static class Pages
{
    public static readonly PageContentModel Overview = new(
        "Uma superfície contínua",
        "A aba selecionada e o corpo compartilham a mesma superfície, com o raio consistente nas quatro posições.");

    public static readonly PageContentModel Editor = new(
        "Um espaço para experimentar",
        "Conteúdo arbitrário, incluindo controles editáveis, pode ser hospedado em qualquer aba.");

    public static readonly PageContentModel Settings = new(
        "Preferências da aplicação",
        "Abas podem ser criadas, renomeadas e fechadas em tempo de execução, com persistência de ordem e seleção.");
}

# Integração com dados e temas

## Coleção de documentos

Em um projeto WPF, adicione DLH.Controls.Wpf e use o namespace XAML:

```xml
xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf"
```

Exemplo de controle ligado à coleção Documents do DataContext:

```xml
<dlh:TabControl x:Name="Tabs"
    ItemsSource="{Binding Documents}" ItemKeyPath="Id"
    SelectedItem="{Binding SelectedDocument, Mode=TwoWay}"
    CanCloseTabs="True" ShowCloseButtons="True"
    CanAddTabs="True" AddTabCommand="{Binding AddDocumentCommand}"
    CanRenameTabs="True" TabHeaderPath="Title"
    CloseTabCommand="{Binding CloseDocumentCommand}">
    <dlh:TabControl.ItemTemplate>
        <DataTemplate><TextBlock Text="{Binding Title}" /></DataTemplate>
    </dlh:TabControl.ItemTemplate>
    <dlh:TabControl.ContentTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Notes, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" TextWrapping="Wrap" />
        </DataTemplate>
    </dlh:TabControl.ContentTemplate>
</dlh:TabControl>
```

No view model: Documents deve ser uma ObservableCollection de modelos com Id string estável, Title e Notes. SelectedDocument deve notificar alterações com INotifyPropertyChanged. CloseDocumentCommand recebe o documento; CanExecute determina se pode fechar e Execute remove da coleção de forma síncrona. A biblioteca não exige um framework MVVM. Veja a implementação executável em samples/DLH.Controls.Wpf.Demo/DemoViewModel.cs e MainWindow.xaml.

Para veto síncrono, assine TabClosing:

```csharp
Tabs.TabClosing += (_, e) =>
{
    if (e.Item is Document document && document.HasUnsavedChanges)
        e.Cancel = true;
};
```

Document e HasUnsavedChanges representam o modelo da aplicação. Para confirmação assíncrona, vete o primeiro fechamento e coordene a confirmação/remoção na aplicação, evitando chamadas recursivas ao mesmo veto.

## Título e ícone no cabeçalho

`ItemTemplate` define a aparência de cada cabeçalho. O ícone pode ser texto, imagem ou geometria vetorial; o título continua no modelo:

```xml
<dlh:TabControl.ItemTemplate>
    <DataTemplate>
        <StackPanel Orientation="Horizontal">
            <Path Width="16" Height="16" Margin="0,0,8,0"
                  Stretch="Uniform" Fill="DarkOrange"
                  Data="{Binding IconGeometry}" />
            <TextBlock Text="{Binding Title}" />
        </StackPanel>
    </DataTemplate>
</dlh:TabControl.ItemTemplate>
```

O modelo deve notificar alterações de `Title` para que o cabeçalho e o contorno sejam recalculados. Durante a edição, o editor textual substitui temporariamente a apresentação composta; o ícone e o template reaparecem intactos ao confirmar ou cancelar.

## Criar e renomear abas

As duas funcionalidades são independentes e desativadas por padrão. `CanAddTabs="True"` mostra `+` depois do último cabeçalho. O botão não é item, não recebe índice e não participa da seleção ou do drag and drop. A aplicação cria o documento no comando:

```csharp
AddDocumentCommand = new RelayCommand(() =>
{
    var document = new Document { Id = Guid.NewGuid().ToString(), Title = "Nova aba" };
    Documents.Add(document);
    SelectedDocument = document;
});
```

Use `AddTabContent` para trocar o sinal ou `AddTabContentTemplate` para fornecer um ícone visual. Sem comando, trate `AddTabRequested`. Se o comando não puder executar, o botão permanece visível e desabilitado.

Para edição direta, indique a propriedade textual gravável:

```xml
<dlh:TabControl CanRenameTabs="True"
                      TabHeaderPath="Title"
                      RenameActivation="F2AndDoubleClick" />
```

`F2` e duplo clique abrem o editor; `Enter` confirma e `Esc` cancela. Perder o foco tenta confirmar. Use `TabRenameRequested` para vetar texto ou `RenameTabCommand` quando o ViewModel precisar validar/persistir a alteração. O comando recebe `TabRenameRequest`. Com comando definido, o controle não grava a propriedade automaticamente.

## Tema e faixa lateral

Para usar Tabs.ItemStyle como BasedOn, mescle o dicionário antes de declarar seu estilo:

```xml
<Window.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/DLH.Controls.Wpf;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
        <SolidColorBrush x:Key="Tabs.Surface" Color="#5D5D5D" />
        <SolidColorBrush x:Key="Tabs.Hover" Color="#686868" />
        <SolidColorBrush x:Key="Tabs.Text" Color="#E0E0E0" />
        <SolidColorBrush x:Key="Tabs.Muted" Color="#D0D0D0" />
        <SolidColorBrush x:Key="Tabs.Focus" Color="#FF8A00" />
    </ResourceDictionary>
</Window.Resources>
```

Na instância, configure TabStripPlacement="Left" e TabDragCursor="ScrollNS". O template padrão já é carregado do pacote; mesclar Generic.xaml explicitamente só é necessário para referências diretas aos estilos/recursos. O tema pertence à aplicação e não recebe um identificador na API da biblioteca.

## Salvar dados de organização e aparência separadamente

```csharp
using DLH.Controls.Wpf;
using System.Text.Json;

TabControlState state = Tabs.CaptureState();
TabControlConfiguration configuration = Tabs.CaptureConfiguration();
string stateJson = JsonSerializer.Serialize(state);
string configurationJson = JsonSerializer.Serialize(configuration);

// A aplicação armazena esses textos onde preferir.
Tabs.RestoreState(JsonSerializer.Deserialize<TabControlState>(stateJson)!);
Tabs.RestoreConfiguration(
    JsonSerializer.Deserialize<TabControlConfiguration>(configurationJson)!);
```

Ao reabrir, carregue os documentos antes de restaurar a ordem. Trate arquivos ausentes, dados inválidos e erros de leitura. Não armazene senhas como parte dessas configurações. Reaplicar um binding pode substituir um valor restaurado; mantenha as fontes do view model coerentes.


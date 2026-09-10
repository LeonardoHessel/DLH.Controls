# DLH Controls

[![NuGet](https://img.shields.io/nuget/vpre/DLH.Controls.Wpf?label=NuGet)](https://www.nuget.org/packages/DLH.Controls.Wpf)
[![Downloads](https://img.shields.io/nuget/dt/DLH.Controls.Wpf?label=Downloads)](https://www.nuget.org/packages/DLH.Controls.Wpf)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Plataforma](https://img.shields.io/badge/plataforma-Windows-0078D4)

**DLH Controls** é uma biblioteca de controles de interface reutilizáveis e personalizáveis para aplicações .NET. O projeto concentra componentes, comportamentos, temas, acessibilidade, documentação e exemplos em uma base preparada para crescer com novos controles e plataformas.

O primeiro pacote disponível é o **DLH.Controls.Wpf**, destinado a aplicações WPF. Ele oferece aparência moderna, suporte a MVVM, teclado, acessibilidade e propriedades WPF convencionais, sem exigir um framework visual adicional.

O pacote contém atualmente:

| Componente | Finalidade | Principais recursos |
|---|---|---|
| `CustomTabControl` | Organizar páginas e documentos em abas | superfície contínua, quatro posições, drag and drop animado, criação, renomeação, fechamento, persistência, sombra e temas |
| `DataGridView` | Exibir coleções em uma tabela rica | seleção configurável, ordenação visual, colunas reordenáveis/ocultáveis, estados de dados, densidades, badges/templates e barras de rolagem customizadas |

> Requer **Windows** e **.NET 10** com WPF. O pacote atual é uma versão de pré-lançamento.

## Visão dos componentes

### CustomTabControl

O `CustomTabControl` desenha a aba selecionada e o corpo como uma única superfície. O mesmo raio é usado nas bordas externas e nas ligações entre a aba e o conteúdo.

![CustomTabControl com abas superiores, inferiores e laterais](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/custom-tab-control.png)

Ele pode ser usado com abas declaradas diretamente no XAML ou com uma coleção em `ItemsSource`. A criação, renomeação, remoção e reordenação são opcionais e vêm desativadas ou protegidas por configurações próprias.

### DataGridView

O `DataGridView` especializa o `DataGrid` nativo do WPF. Ele preserva virtualização, bindings, templates, ordenação, teclado e tipos nativos de coluna, acrescentando aparência e comportamentos consistentes com a biblioteca.

![DataGridView com ordenação, status, múltiplas colunas e barras customizadas](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/data-grid-view.png)

O exemplo mostra ordenação com indicadores, status renderizado por template, menu de colunas, densidade compacta e rolagem nos dois eixos.

## Instalação

### CLI do .NET

```powershell
dotnet add package DLH.Controls.Wpf --version 0.2.0-preview.4
```

### Package Manager do Visual Studio

```powershell
Install-Package DLH.Controls.Wpf -Version 0.2.0-preview.4
```

### PackageReference

```xml
<ItemGroup>
    <PackageReference Include="DLH.Controls.Wpf" Version="0.2.0-preview.4" />
</ItemGroup>
```

### Paket CLI

Informe o projeto que receberá a referência:

```powershell
paket add DLH.Controls.Wpf --version 0.2.0-preview.4 --project caminho/SeuProjeto.csproj
```

Ou declare o pacote no arquivo `paket.dependencies`:

```text
source https://api.nuget.org/v3/index.json
nuget DLH.Controls.Wpf 0.2.0-preview.4
```

Adicione esta linha ao `paket.references` do projeto WPF:

```text
DLH.Controls.Wpf
```

Depois, restaure normalmente:

```powershell
paket install
```

NuGet, `PackageReference` e Paket consomem o mesmo arquivo `.nupkg`.

## Preparação do XAML

Adicione o namespace da biblioteca à janela ou ao controle que utilizará os componentes:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf">
</Window>
```

Os estilos padrão estão em `Themes/Generic.xaml` e são carregados pelo sistema de temas do WPF. Não é necessário copiar templates para a aplicação consumidora.

# CustomTabControl

## Exemplo mínimo

```xml
<dlh:CustomTabControl CornerRadius="12"
                      HeaderIndent="0"
                      TabSpacing="0">
    <dlh:CustomTabItem Header="Visão geral">
        <TextBlock Margin="24" Text="Conteúdo da visão geral" />
    </dlh:CustomTabItem>

    <dlh:CustomTabItem Header="Editor">
        <TextBox Margin="24" AcceptsReturn="True" />
    </dlh:CustomTabItem>
</dlh:CustomTabControl>
```

`HeaderIndent="0"` alinha a primeira aba à borda do corpo. `TabSpacing="0"` remove espaços entre abas. `CornerRadius` controla todas as curvas do contorno.

## Cabeçalho com texto e ícone

O `Header` aceita qualquer conteúdo WPF:

```xml
<dlh:CustomTabItem>
    <dlh:CustomTabItem.Header>
        <StackPanel Orientation="Horizontal">
            <Image Width="18"
                   Height="18"
                   Margin="0,0,8,0"
                   Source="/MinhaAplicacao;component/Assets/Edit.png" />
            <TextBlock VerticalAlignment="Center" Text="Editor" />
        </StackPanel>
    </dlh:CustomTabItem.Header>

    <TextBlock Margin="24" Text="Página do editor" />
</dlh:CustomTabItem>
```

Para ícones vetoriais, substitua `Image` por `Path`, `Viewbox` ou outro elemento visual.

## Uso com ItemsSource e MVVM

```xml
<dlh:CustomTabControl ItemsSource="{Binding Documents}"
                      SelectedItem="{Binding SelectedDocument}"
                      ItemKeyPath="Id"
                      TabHeaderPath="Title">
    <dlh:CustomTabControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Margin="0,0,8,0" Text="◆" />
                <TextBlock Text="{Binding Title}" />
            </StackPanel>
        </DataTemplate>
    </dlh:CustomTabControl.ItemTemplate>

    <dlh:CustomTabControl.ContentTemplate>
        <DataTemplate>
            <TextBox Margin="24"
                     Text="{Binding Text, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" />
        </DataTemplate>
    </dlh:CustomTabControl.ContentTemplate>
</dlh:CustomTabControl>
```

Um modelo simples pode ser definido assim:

```csharp
public sealed class DocumentViewModel
{
    public required string Id { get; init; }
    public required string Title { get; set; }
    public string Text { get; set; } = string.Empty;
}

public ObservableCollection<DocumentViewModel> Documents { get; } = [];
public DocumentViewModel? SelectedDocument { get; set; }
```

Os dados e o estado editável permanecem no ViewModel. Trocar de aba não recria nem perde o estado do modelo.

## Abas superiores, inferiores e laterais

Use a propriedade WPF `TabStripPlacement`:

```xml
<dlh:CustomTabControl TabStripPlacement="Top" />
<dlh:CustomTabControl TabStripPlacement="Bottom" />
<dlh:CustomTabControl TabStripPlacement="Left" />
<dlh:CustomTabControl TabStripPlacement="Right" />
```

O contorno, a direção da rolagem e o eixo do drag and drop acompanham automaticamente a posição.

## Drag and drop para reorganizar

```xml
<dlh:CustomTabControl CanReorderTabs="True"
                      MinimumDragDistance="5"
                      IsDragPreviewEnabled="True"
                      IsAnimationEnabled="True"
                      DragAnimationDuration="0:0:0.180"
                      DragPreviewOpacity="0.94"
                      TabDragCursor="ScrollWE" />
```

O cursor muda somente depois que o usuário ultrapassa o limiar de arraste. Em abas superiores ou inferiores, o movimento é horizontal; nas laterais, é vertical. Nas extremidades, a faixa rola automaticamente.

A reorganização também está disponível por teclado:

- `Ctrl+Shift+Esquerda/Direita` em abas horizontais.
- `Ctrl+Shift+Cima/Baixo` em abas laterais.
- `Esc` cancela um arraste em andamento.

Por código:

```csharp
bool moved = tabs.MoveTab(oldIndex: 3, newIndex: 1);
```

## Adicionar uma aba

A ação `+` é opcional e não participa da seleção, persistência ou reordenação:

```xml
<dlh:CustomTabControl ItemsSource="{Binding Documents}"
                      CanAddTabs="True"
                      AddTabCommand="{Binding AddDocumentCommand}"
                      AddTabCommandParameter="{Binding SelectedWorkspace}" />
```

Sem comando, a aplicação pode tratar o evento:

```csharp
tabs.AddTabRequested += (_, e) =>
{
    Documents.Add(CreateDocument());
};
```

## Renomear diretamente no cabeçalho

```xml
<dlh:CustomTabControl ItemsSource="{Binding Documents}"
                      CanRenameTabs="True"
                      TabHeaderPath="Title"
                      RenameActivation="F2AndDoubleClick" />
```

- `F2` ou duplo clique inicia a edição.
- `Enter` confirma.
- `Esc` cancela.
- `TabRenameRequested` pode validar ou rejeitar o novo título.
- `RenameTabCommand` permite que o ViewModel assuma a atualização.

## Fechar abas

```xml
<dlh:CustomTabControl ItemsSource="{Binding Documents}"
                      CanCloseTabs="True"
                      ShowCloseButtons="True"
                      CloseTabCommand="{Binding CloseDocumentCommand}" />
```

Uma aba individual pode ser protegida:

```xml
<dlh:CustomTabItem Header="Início"
                   dlh:CustomTabControl.CanCloseTab="False" />
```

A aplicação pode cancelar o fechamento antes da remoção:

```csharp
tabs.TabClosing += (_, e) =>
{
    if (HasUnsavedChanges(e.Item))
        e.Cancel = true;
};
```

A biblioteca não abre diálogos automaticamente; a confirmação pertence à aplicação.

## Sombra e aparência

```xml
<dlh:CustomTabControl Background="#35373C"
                      Foreground="#F2F3F5"
                      BorderBrush="#4C5058"
                      BorderThickness="1"
                      CornerRadius="12"
                      IsShadowEnabled="True"
                      ShadowColor="#494949"
                      ShadowOpacity="0.5"
                      ShadowBlurRadius="10"
                      ShadowDepth="0"
                      ShadowDirection="315" />
```

O fundo, a borda e a sombra utilizam o mesmo contorno unificado. `ShadowOpacity` varia de `0` a `1`; desativar `IsShadowEnabled` preserva os demais valores.

## Salvar ordem e seleção

Cada item precisa de uma chave única e estável. Por padrão, o controle procura a propriedade indicada em `ItemKeyPath="Id"`.

```csharp
using (var output = File.Create("tabs.json"))
    tabs.SaveState(output);

using (var input = File.OpenRead("tabs.json"))
    tabs.LoadState(input);
```

Também é possível trabalhar sem arquivos:

```csharp
TabControlState state = tabs.CaptureState();
tabs.RestoreState(state);
```

O estado contém chaves, ordem e seleção. O conteúdo das páginas permanece sob responsabilidade da aplicação.

# DataGridView

## Exemplo mínimo

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  SelectionBehavior="Row"
                  CornerRadius="10">
    <DataGridTextColumn Header="Nº Embarque"
                        Binding="{Binding Number}"
                        SortMemberPath="Number"
                        MinWidth="160" />

    <DataGridTextColumn Header="Origem"
                        Binding="{Binding Origin}"
                        SortMemberPath="Origin"
                        Width="*" />

    <DataGridTextColumn Header="Quantidade"
                        Binding="{Binding Quantity}"
                        SortMemberPath="Quantity"
                        Width="Auto" />
</dlh:DataGridView>
```

As colunas são os tipos nativos do WPF: `DataGridTextColumn`, `DataGridCheckBoxColumn`, `DataGridComboBoxColumn`, `DataGridHyperlinkColumn` e `DataGridTemplateColumn`. `AutoGenerateColumns` é desativado por padrão.

## Modelo e coleção

```csharp
public sealed record Shipment(
    string Number,
    string Origin,
    string Vehicle,
    int Quantity,
    string Status);

public ObservableCollection<Shipment> Shipments { get; } =
[
    new("EXP-2026-001", "SP", "Caminhão 12", 120, "Concluído"),
    new("EXP-2026-002", "MG", "Caminhão 07", 85, "Em inspeção"),
    new("EXP-2026-003", "PR", "Van 03", 34, "Pendente")
];
```

Alterações em `ObservableCollection<T>` são refletidas automaticamente.

## Seleção

```xml
<dlh:DataGridView SelectionBehavior="None" />
<dlh:DataGridView SelectionBehavior="Row" />
<dlh:DataGridView SelectionBehavior="Column" />
<dlh:DataGridView SelectionBehavior="Cell" />
```

Para receber a seleção no ViewModel:

```xml
<dlh:DataGridView SelectionBehavior="Cell"
                  SelectionChangedCommand="{Binding SelectionChangedCommand}" />
```

O comando recebe `DataGridViewSelection`, contendo `Item` e `Column` de acordo com o modo selecionado.

```csharp
private void OnSelectionChanged(DataGridViewSelection selection)
{
    object? row = selection.Item;
    DataGridColumn? column = selection.Column;
}
```

## Ordenação e indicadores

A ordenação global pode ser ligada ou desligada:

```xml
<dlh:DataGridView CanUserSortColumns="True"
                  ShowSortIndicators="True" />
```

Cada coluna também pode controlar sua própria ordenação:

```xml
<DataGridTextColumn Header="Observação"
                    Binding="{Binding Notes}"
                    CanUserSort="False" />
```

Os indicadores comunicam três estados:

| Estado | Representação |
|---|---|
| Coluna ordenável inativa | setas discretas para cima e para baixo |
| Ordem crescente | seta ascendente destacada |
| Ordem decrescente | seta descendente destacada |

Tamanho, cores e geometrias são configuráveis:

```xml
<dlh:DataGridView SortIconSize="14"
                  SortIconBrush="#9DA3AE"
                  ActiveSortIconBrush="#42A5E8" />
```

## Ordenação padrão e limpeza

```xml
<dlh:DataGridView DefaultSortMemberPath="Number"
                  DefaultSortDirection="Ascending"
                  ShowClearSortMenuItem="True"
                  ShowRestoreDefaultSortMenuItem="True" />
```

Ao clicar com o botão direito em um cabeçalho:

- **Limpar ordenação** recupera a ordem original da coleção.
- **Restaurar ordenação padrão** reaplica a propriedade e direção configuradas.

As mesmas ações estão disponíveis por código:

```csharp
grid.ClearSorting();
bool applied = grid.ApplyDefaultSort();
```

## Reordenar, redimensionar e ocultar colunas

```xml
<dlh:DataGridView CanUserReorderColumns="True"
                  CanUserResizeColumns="True"
                  CanUserToggleColumnVisibility="True" />
```

- Arraste o cabeçalho para mudar sua posição.
- Arraste a divisória para mudar a largura.
- Clique com o botão direito para exibir ou ocultar colunas.
- O controle impede que a última coluna visível seja ocultada.

É possível desativar recursos em uma coluna específica:

```xml
<DataGridTextColumn Header="Código"
                    Binding="{Binding Code}"
                    CanUserReorder="False"
                    CanUserResize="False"
                    CanUserSort="False" />
```

## Células personalizadas

Use `DataGridTemplateColumn` para status, ícones, botões ou qualquer conteúdo WPF:

```xml
<DataGridTemplateColumn Header="Status" SortMemberPath="Status">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border Padding="8,3"
                    CornerRadius="8"
                    Background="#294A4034"
                    BorderBrush="#4B725A"
                    BorderThickness="1">
                <TextBlock Text="{Binding Status}"
                           Foreground="#8DD5A1"
                           FontWeight="SemiBold" />
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

## Estados vazio, carregando e erro

```xml
<dlh:DataGridView EmptyMessage="Nenhum embarque encontrado."
                  LoadingMessage="Carregando embarques..."
                  IsLoading="{Binding IsLoading}"
                  ErrorMessage="{Binding ErrorMessage}" />
```

Prioridade visual:

1. `ErrorMessage` preenchida apresenta a falha.
2. `IsLoading="True"` apresenta o carregamento.
3. Coleção vazia apresenta `EmptyMessage`.
4. Caso contrário, as linhas são exibidas.

## Densidade e separadores

```xml
<dlh:DataGridView Density="Compact"
                  ShowRowSeparators="True"
                  CellPadding="12,6" />
```

Densidades disponíveis:

- `Compact`
- `Default`
- `Comfortable`

Para linhas alternadas, utilize a propriedade nativa:

```xml
<dlh:DataGridView AlternatingRowBackground="#24373A40"
                  AlternationCount="2" />
```

## Barras de rolagem

```xml
<dlh:DataGridView ScrollBarThickness="10"
                  ScrollBarTrackBrush="#3D4046"
                  ScrollBarThumbBrush="#686D77"
                  ScrollBarThumbHoverBrush="#8B919D" />
```

A barra vertical possui espaço reservado, impedindo que o texto da última coluna fique sob ela. A barra horizontal ocupa toda a largura inferior e reserva sua própria faixa, impedindo que o último registro seja encoberto. O raio das extremidades nunca ultrapassa metade da espessura real.

## Desempenho

O controle mantém habilitadas:

```xml
<dlh:DataGridView EnableRowVirtualization="True"
                  EnableColumnVirtualization="True"
                  ScrollViewer.CanContentScroll="True" />
```

Para coleções grandes, evite colocar o grid dentro de outro `ScrollViewer`, pois isso pode impedir a virtualização das linhas.

# Temas e recursos

As propriedades convencionais `Background`, `Foreground`, `BorderBrush`, `BorderThickness`, `Padding`, `FontFamily` e `FontSize` continuam disponíveis.

Recursos principais do `CustomTabControl`:

```xml
<SolidColorBrush x:Key="Tabs.Surface" Color="#35373C" />
<SolidColorBrush x:Key="Tabs.Hover" Color="#454850" />
<SolidColorBrush x:Key="Tabs.Text" Color="#F2F3F5" />
<SolidColorBrush x:Key="Tabs.Muted" Color="#BCC0CA" />
<SolidColorBrush x:Key="Tabs.Edge" Color="#4C5058" />
<SolidColorBrush x:Key="Tabs.Focus" Color="#9CC9FF" />
```

Recursos principais do `DataGridView`:

```xml
<SolidColorBrush x:Key="DataGridView.Surface" Color="#35373C" />
<SolidColorBrush x:Key="DataGridView.Header" Color="#2F3136" />
<SolidColorBrush x:Key="DataGridView.Hover" Color="#454850" />
<SolidColorBrush x:Key="DataGridView.Selection" Color="#334F8AC9" />
<SolidColorBrush x:Key="DataGridView.Text" Color="#F2F3F5" />
<SolidColorBrush x:Key="DataGridView.Edge" Color="#4C5058" />
<SolidColorBrush x:Key="DataGridView.Focus" Color="#9CC9FF" />
```

Como são `DynamicResource`, esses valores podem ser substituídos durante a execução para alternar temas.

# Estrutura da solução

- `src/DLH.Controls.Wpf`: biblioteca reutilizável e projeto empacotável.
- `src/DLH.Controls.Wpf/Controls`: implementação dos componentes.
- `src/DLH.Controls.Wpf/Themes`: templates e recursos visuais.
- `samples/DLH.Controls.Wpf.Demo`: visualizador interativo.
- `tests/DLH.Controls.Wpf.Tests`: testes de integração WPF.
- `docs`: documentação detalhada e planos técnicos.
- `eng`: validação e preparação do pacote.

# Desenvolvimento

```powershell
dotnet restore DLH.Controls.sln
dotnet build DLH.Controls.sln -c Release
dotnet run --project samples/DLH.Controls.Wpf.Demo -c Release
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release
dotnet pack src/DLH.Controls.Wpf -c Release -o artifacts/packages
```

Validação completa antes de publicar:

```powershell
./eng/Validate.ps1
```

A rotina compila a solução, executa as suítes WPF, gera o pacote e verifica DLL, README, licença e instruções do Paket. Somente a biblioteca é empacotada; demonstração e testes não entram no `.nupkg`.

A demonstração preserva preferências em `%LOCALAPPDATA%\CustomTabControl.Demo`.

# Documentação adicional

- [Manual de uso do DLH Controls](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Manual.md)
- [CustomTabControl detalhado](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/TabControl.md)
- [Referência da API](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/ApiReference.md)
- [Integração com dados, MVVM e temas](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Integration.md)
- [Preparação e conteúdo do pacote](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Packaging.md)
- [Publicação automática](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/Publishing.md)
- [Preparação para a versão 1.0.0](https://github.com/LeonardoHessel/DLH.Controls/blob/main/docs/ReleaseReadiness.md)
- [Histórico de versões](https://github.com/LeonardoHessel/DLH.Controls/blob/main/CHANGELOG.md)
- [Como contribuir](https://github.com/LeonardoHessel/DLH.Controls/blob/main/CONTRIBUTING.md)
- [Política de segurança](https://github.com/LeonardoHessel/DLH.Controls/blob/main/SECURITY.md)

# Apoie o projeto

Se o **DLH Controls** estiver ajudando sua aplicação, você pode apoiar a manutenção da biblioteca, a correção de problemas e o desenvolvimento de novos componentes.

## Apoie diretamente pelo Pix

O **Pix é a forma principal de apoiar o DLH Controls no Brasil**. Escaneie o QR Code com o aplicativo do seu banco e escolha o valor da contribuição.

![QR Code para apoiar o DLH Controls por Pix](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/pix-qrcode.png)

**Chave Pix aleatória:** `65e95283-e2bd-46c3-b045-8773e8157df8`

### Apoio internacional — GitHub Sponsors

Para pessoas de outros países, contribuições únicas ou mensais podem ser feitas pelo GitHub Sponsors:

[![Apoie pelo GitHub Sponsors](https://img.shields.io/badge/Apoie-GitHub%20Sponsors-EA4AAA?logo=githubsponsors&logoColor=white)](https://github.com/sponsors/LeonardoHessel)

[Abrir o perfil de patrocínio de LeonardoHessel](https://github.com/sponsors/LeonardoHessel)

# Licença e atribuição

Distribuído sob a **DLH Controls — Licença de Uso com Atribuição Visível, versão 1.0**. Consulte a [licença completa](https://github.com/LeonardoHessel/DLH.Controls/blob/main/LICENSE.txt).

Uso comercial, modificação e redistribuição são permitidos conforme as condições da licença, incluindo crédito acessível aos usuários:

> Este produto utiliza DLH Controls, desenvolvido por Leonardo D. de L. Hessel.

O crédito pode aparecer em **Sobre**, **Créditos** ou **Licenças de terceiros**. Produtos sem interface gráfica devem disponibilizá-lo na documentação ou ajuda.

# Pacote e autoria

- Pacote: [DLH.Controls.Wpf no NuGet.org](https://www.nuget.org/packages/DLH.Controls.Wpf)
- Versão publicada mais recente: `0.2.0-preview.4`
- Autor: **Leonardo D. de L. Hessel**
- Plataforma: Windows
- Framework: .NET 10 / WPF

Mudanças posteriores à versão indicada permanecem em desenvolvimento até uma nova release.

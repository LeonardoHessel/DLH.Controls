# Manual do DLH Controls

Este manual apresenta o uso da biblioteca **DLH Controls** pelo ponto de vista de quem desenvolve uma aplicação. A implementação disponível atualmente está no pacote `DLH.Controls.Wpf`, para Windows com .NET 10 e WPF.

## Sumário

1. [Instalação](#1-instalação)
2. [Preparação do XAML](#2-preparação-do-xaml)
3. [TabControl](#3-tabcontrol)
4. [DataGridView](#4-datagridview)
5. [Temas e personalização](#5-temas-e-personalização)
6. [Acessibilidade e teclado](#6-acessibilidade-e-teclado)
7. [Desempenho](#7-desempenho)
8. [Diagnóstico de problemas](#8-diagnóstico-de-problemas)
9. [Aplicação de demonstração](#9-aplicação-de-demonstração)
10. [Documentos de referência](#10-documentos-de-referência)

## 1. Instalação

Instale o pacote no projeto WPF:

```powershell
dotnet add package DLH.Controls.Wpf --version 0.2.0-preview.4
```

Ou adicione a referência diretamente ao arquivo do projeto:

```xml
<ItemGroup>
    <PackageReference Include="DLH.Controls.Wpf" Version="0.2.0-preview.4" />
</ItemGroup>
```

Como as versões atuais são de pré-lançamento, marque a opção de exibir versões de pré-lançamento ao procurar o pacote pelo gerenciador do Visual Studio.

## 2. Preparação do XAML

Declare o namespace da biblioteca na janela ou no controle que receberá os componentes:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf">
</Window>
```

Os estilos padrão são carregados automaticamente. A aplicação só precisa mesclar `Themes/Generic.xaml` quando quiser referenciar diretamente os estilos ou recursos publicados pela biblioteca.

## 3. TabControl

O `TabControl` organiza conteúdos em abas e desenha a aba selecionada e seu corpo como uma única superfície. O controle aceita tanto `TabControlItem` quanto `System.Windows.Controls.TabItem` nativo.

![TabControl em diferentes posições](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/custom-tab-control.png)

### 3.1 Uso básico

```xml
<dlh:TabControl CornerRadius="12"
                      HeaderIndent="0"
                      TabSpacing="0">
    <dlh:TabControlItem Header="Visão geral">
        <TextBlock Margin="24" Text="Conteúdo da visão geral" />
    </dlh:TabControlItem>

    <dlh:TabControlItem Header="Editor">
        <TextBox Margin="24" AcceptsReturn="True" />
    </dlh:TabControlItem>
</dlh:TabControl>
```

- `CornerRadius` define o mesmo raio para todas as curvas do contorno.
- `HeaderIndent="0"` encosta a primeira aba na borda do corpo.
- `TabSpacing="0"` elimina a distância entre as abas.
- `TabStripPlacement` aceita `Top`, `Bottom`, `Left` e `Right`.

### 3.2 Título e ícone

O cabeçalho aceita qualquer conteúdo WPF:

```xml
<dlh:TabControlItem>
    <dlh:TabControlItem.Header>
        <StackPanel Orientation="Horizontal">
            <Path Width="16"
                  Height="16"
                  Margin="0,0,8,0"
                  Stretch="Uniform"
                  Fill="Orange"
                  Data="M2,2 L14,2 14,14 2,14 Z" />
            <TextBlock VerticalAlignment="Center" Text="Editor" />
        </StackPanel>
    </dlh:TabControlItem.Header>

    <TextBlock Margin="24" Text="Conteúdo da aba" />
</dlh:TabControlItem>
```

Com `ItemsSource`, use `ItemTemplate` para montar o cabeçalho e `ContentTemplate` para apresentar a página.

### 3.3 Uso com MVVM

```xml
<dlh:TabControl ItemsSource="{Binding Documents}"
                      SelectedItem="{Binding SelectedDocument, Mode=TwoWay}"
                      ItemKeyPath="Id"
                      TabHeaderPath="Title">
    <dlh:TabControl.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Title}" />
        </DataTemplate>
    </dlh:TabControl.ItemTemplate>

    <dlh:TabControl.ContentTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Text, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" />
        </DataTemplate>
    </dlh:TabControl.ContentTemplate>
</dlh:TabControl>
```

Use uma `ObservableCollection<T>` para que inclusões, remoções e mudanças de posição sejam refletidas na interface.

### 3.4 Reordenar abas

```xml
<dlh:TabControl CanReorderTabs="True"
                      MinimumDragDistance="5"
                      IsDragPreviewEnabled="True"
                      IsAnimationEnabled="True"
                      DragAnimationDuration="0:0:0.180"
                      DragPreviewOpacity="0.94" />
```

O arraste começa somente após ultrapassar a distância mínima. Abas superiores e inferiores movem-se no eixo horizontal; abas laterais movem-se no eixo vertical. Pressionar `Esc` cancela a operação.

### 3.5 Adicionar abas

A ação de adicionar vem desativada. Quando habilitada, aparece depois da última aba e não participa dos índices nem da seleção:

```xml
<dlh:TabControl CanAddTabs="True"
                      AddTabContent="+"
                      AddTabCommand="{Binding AddDocumentCommand}" />
```

O comando recebe `AddTabCommandParameter`, quando definido. Sem comando, trate o evento `AddTabRequested`. A aplicação continua responsável por criar o objeto e adicioná-lo à coleção.

### 3.6 Renomear abas

```xml
<dlh:TabControl CanRenameTabs="True"
                      TabHeaderPath="Title"
                      RenameActivation="F2AndDoubleClick" />
```

- `F2` ou duplo clique inicia a edição, conforme `RenameActivation`.
- `Enter` confirma.
- `Esc` cancela.
- `TabHeaderPath` deve apontar para uma propriedade `string` gravável.
- `RenameTabCommand` permite delegar a validação e a atualização ao ViewModel.

### 3.7 Fechar abas

```xml
<dlh:TabControl CanCloseTabs="True"
                      ShowCloseButtons="True"
                      CloseTabCommand="{Binding CloseDocumentCommand}" />
```

`CanCloseTabs` concede a permissão global. `ShowCloseButtons` controla apenas a aparência. Use a propriedade anexada `TabControl.CanCloseTab="False"` para proteger uma aba específica. O evento `TabClosing` permite cancelar a operação antes da remoção.

### 3.8 Sombra e contorno

```xml
<dlh:TabControl IsShadowEnabled="True"
                      ShadowColor="#494949"
                      ShadowOpacity="0.5"
                      ShadowBlurRadius="10"
                      ShadowDepth="0"
                      ShadowDirection="315"
                      CornerRadius="12" />
```

A sombra, a borda, a aba selecionada e o corpo usam o mesmo contorno. Desativar a sombra preserva seus parâmetros para uma ativação posterior.

### 3.9 Persistir ordem e seleção

Cada item deve possuir uma chave de texto única e estável, indicada por `ItemKeyPath` ou pela propriedade anexada `TabKey`:

```csharp
using (var output = File.Create("tabs.json"))
    tabs.SaveState(output);

using (var input = File.OpenRead("tabs.json"))
    tabs.LoadState(input);
```

O estado armazena ordem e seleção. Os conteúdos e dados das páginas permanecem sob responsabilidade da aplicação.

## 4. DataGridView

O `DataGridView` mantém a infraestrutura do `DataGrid` nativo e acrescenta aparência, estados, ordenação visual, menu de colunas e barras de rolagem consistentes.

![DataGridView com ordenação e células personalizadas](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/data-grid-view.png)

### 4.1 Uso básico

```xml
<dlh:DataGridView ItemsSource="{Binding Products}"
                  SelectionBehavior="Row"
                  CornerRadius="10">
    <DataGridTextColumn Header="Código"
                        Binding="{Binding Code}"
                        SortMemberPath="Code"
                        MinWidth="120" />
    <DataGridTextColumn Header="Descrição"
                        Binding="{Binding Description}"
                        SortMemberPath="Description"
                        Width="*" />
    <DataGridTextColumn Header="Quantidade"
                        Binding="{Binding Quantity}"
                        SortMemberPath="Quantity"
                        Width="Auto" />
</dlh:DataGridView>
```

`AutoGenerateColumns` é desativado por padrão. Declare colunas nativas como `DataGridTextColumn`, `DataGridCheckBoxColumn`, `DataGridComboBoxColumn`, `DataGridHyperlinkColumn` e `DataGridTemplateColumn`.

### 4.2 Seleção

`SelectionBehavior` aceita:

Defina `AllowMultipleSelection="True"` para selecionar várias linhas ou células com Ctrl/Shift. Use `GetBatchSelection()` para ler a seleção atual e `ExecuteBatchAction()` para encaminhá-la ao `BatchActionCommand`.

| Valor | Comportamento |
|---|---|
| `None` | Não permite seleção pelo controle |
| `Row` | Seleciona a linha completa |
| `Column` | Seleciona a coluna |
| `Cell` | Seleciona uma célula |

Defina `SelectionChangedCommand` para receber um `DataGridViewSelection` no ViewModel.

### 4.3 Ordenação

```xml
<dlh:DataGridView CanUserSortColumns="True"
                  ShowSortIndicators="True"
                  DefaultSortMemberPath="Description"
                  DefaultSortDirection="Ascending"
                  ShowClearSortMenuItem="True"
                  ShowRestoreDefaultSortMenuItem="True" />
```

O menu do cabeçalho pode limpar a ordenação atual ou reaplicar a ordenação padrão. Por código, use `ClearSorting()` e `ApplyDefaultSort()`.

#### Ordenação por múltiplas colunas

Ative `IsMultiColumnSortEnabled`. Um clique inicia a ordenação, `Shift+clique` acrescenta critérios e `Ctrl+clique` remove o critério apontado. Quando existem dois ou mais critérios, o cabeçalho exibe números que indicam sua prioridade.

```csharp
grid.ApplySort(nameColumn, ListSortDirection.Ascending);
grid.ApplySort(dateColumn, ListSortDirection.Descending, append: true);
grid.RemoveSort(nameColumn);
```

### 4.4 Filtros por coluna

Os filtros são combinados com **E** e também respeitam um filtro que já exista na visualização WPF:

```csharp
grid.SetFilter(statusColumn, "Pendente", DataGridViewFilterOperator.Equals);
grid.SetFilter(quantityColumn, "10", DataGridViewFilterOperator.GreaterThan);
grid.ClearFilter(statusColumn);
grid.ClearFilters();
```

Defina `dlh:DataGridView.FilterMemberPath="Customer.Name"` na coluna quando o valor filtrado não for o mesmo usado para ordenação. Use `dlh:DataGridView.CanUserFilter="False"` para impedir filtros naquela coluna. O menu do cabeçalho oferece ações para limpar o filtro da coluna e todos os filtros ativos.

### 4.5 Organização das colunas

```xml
<dlh:DataGridView CanUserReorderColumns="True"
                  CanUserResizeColumns="True"
                  CanUserToggleColumnVisibility="True" />
```

O usuário pode arrastar cabeçalhos, redimensionar divisórias e usar o menu de contexto para mostrar ou ocultar colunas. O controle preserva pelo menos uma coluna visível.

### 4.6 Persistência do layout

Cada coluna precisa de uma chave estável. `SortMemberPath` é usado automaticamente quando for único; para colunas sem ordenação ou com caminhos repetidos, defina `DataGridView.ColumnKey`:

```xml
<DataGridTemplateColumn Header="Ações"
                        dlh:DataGridView.ColumnKey="actions">
    <!-- template da aplicação -->
</DataGridTemplateColumn>
```

```csharp
DataGridViewState state = grid.CaptureState();
grid.RestoreState(state);

using (var output = File.Create("grid-layout.json"))
    grid.SaveState(output);

using (var input = File.OpenRead("grid-layout.json"))
    grid.LoadState(input);

grid.ResetState();
```

O formato versionado registra ordem, largura, unidade de largura, visibilidade e ordenação. A biblioteca não escolhe o caminho do arquivo. Colunas ausentes são ignoradas, colunas novas permanecem no final e dados inválidos não são aplicados parcialmente.

### 4.7 Edição e validação

Ative a edição explicitamente com `IsCellEditingEnabled="True"`. As regras declaradas no `Binding` continuam responsáveis pela validação. Células inválidas recebem borda e dica de erro; personalize com `ShowValidationErrors` e `ValidationErrorBrush`. O `CellEditEndingCommand` pode inspecionar item, coluna e ação e cancelar a confirmação.

### 4.8 Exportação CSV

Use `ExportCsv` para gravar apenas colunas visíveis, na ordem atual, e as linhas produzidas pelos filtros e ordenação. Para `DataGridTemplateColumn`, informe `DataGridViewCsvOptions.ValueSelector` quando `SortMemberPath` não representar o conteúdo exibido.

### 4.9 Células personalizadas

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

O mesmo recurso permite apresentar ícones, botões, imagens, links ou editores próprios.

### 4.10 Agrupamento e detalhes

Configure `GroupMemberPath` e `IsGroupingEnabled="True"` para agrupar a visualização. O cabeçalho de cada grupo continua personalizável pela coleção `GroupStyle` nativa. Defina um `RowDetailsTemplate` e ative `ShowRowDetailsOnSelection` para abrir detalhes com a seleção; `SetRowDetailsVisibility` permite controlar uma linha materializada por código.

### 4.11 Estados da coleção

```xml
<dlh:DataGridView EmptyMessage="Nenhum item encontrado."
                  LoadingMessage="Carregando..."
                  IsLoading="{Binding IsLoading}"
                  ErrorMessage="{Binding ErrorMessage}" />
```

A prioridade é: erro, carregamento, coleção vazia e, por fim, dados.

### 4.12 Densidade e rolagem

```xml
<dlh:DataGridView Density="Compact"
                  ShowRowSeparators="True"
                  CellPadding="12,6"
                  ScrollBarThickness="10"
                  ScrollBarTrackBrush="#3D4046"
                  ScrollBarThumbBrush="#686D77"
                  ScrollBarThumbHoverBrush="#8B919D" />
```

As densidades disponíveis são `Compact`, `Default` e `Comfortable`. As barras reservam espaço próprio para não cobrir linhas, cabeçalhos ou o conteúdo da última coluna.

## 5. Temas e personalização

As propriedades WPF usuais, como `Background`, `Foreground`, `BorderBrush`, `BorderThickness`, `FontFamily` e `FontSize`, continuam disponíveis.

Para substituir recursos globais, declare chaves no dicionário da aplicação:

```xml
<SolidColorBrush x:Key="Tabs.Surface" Color="#35373C" />
<SolidColorBrush x:Key="Tabs.Focus" Color="#9CC9FF" />
<SolidColorBrush x:Key="DataGridView.Surface" Color="#35373C" />
<SolidColorBrush x:Key="DataGridView.Header" Color="#2F3136" />
<SolidColorBrush x:Key="DataGridView.Focus" Color="#9CC9FF" />
```

Os recursos são dinâmicos e podem ser trocados durante a execução. Consulte o README para a lista completa das principais chaves.

## 6. Acessibilidade e teclado

- Preserve contraste suficiente entre texto, fundo, foco e seleção.
- Forneça textos acessíveis para cabeçalhos compostos apenas por ícones.
- Não remova indicadores de foco sem oferecer uma alternativa visível.
- Teste navegação com `Tab`, setas e `Ctrl+Tab`.
- No `TabControl`, `Ctrl+Shift` com as setas reorganiza abas quando permitido.
- No `DataGridView`, os comportamentos de teclado do `DataGrid` continuam disponíveis.

Consulte a [lista de verificação de acessibilidade](AccessibilityChecklist.md) antes de publicar uma aplicação.

## 7. Desempenho

O `DataGridView` mantém virtualização de linhas e colunas habilitada. Para preservar esse comportamento:

- evite colocar o grid dentro de outro `ScrollViewer`;
- prefira coleções observáveis e atualizações incrementais;
- mantenha templates de célula simples em tabelas muito grandes;
- evite medir colunas extensas com `Width="Auto"` quando não for necessário.

## 8. Diagnóstico de problemas

### O estilo não foi aplicado

Confirme a referência ao pacote, o namespace XAML e a compatibilidade com `net10.0-windows`. Limpe e recompile a solução depois de atualizar o pacote.

### A aba não pode ser arrastada

Verifique `CanReorderTabs`, ultrapasse `MinimumDragDistance` e use uma coleção mutável sem ordenação, agrupamento ou filtro ativo.

### O título não pode ser editado

Ative `CanRenameTabs` e confirme que `TabHeaderPath` termina em uma propriedade `string` gravável. Quando houver `RenameTabCommand`, o comando é responsável por atualizar o modelo.

### A aba não fecha

Verifique `CanCloseTabs`, a propriedade individual `CanCloseTab`, o resultado de `CanExecute` do comando e um possível cancelamento em `TabClosing`.

### Uma coluna não ordena

Confirme `CanUserSortColumns`, `CanUserSort` na coluna e um `SortMemberPath` correspondente a uma propriedade do item.

### O grid perdeu virtualização

Remova o `ScrollViewer` externo e confirme que `EnableRowVirtualization`, `EnableColumnVirtualization` e `ScrollViewer.CanContentScroll` continuam habilitados.

## 9. Aplicação de demonstração

Clone o repositório e execute:

```powershell
dotnet run --project samples/DLH.Controls.Wpf.Demo -c Release
```

A demonstração permite experimentar os controles, alterar configurações e observar diferentes posições, temas, densidades, modos de seleção e comportamentos.

## 10. Documentos de referência

- [Referência da API](ApiReference.md)
- [Guia detalhado do TabControl](TabControl.md)
- [Integração com dados, MVVM e temas](Integration.md)
- [Lista de verificação de acessibilidade](AccessibilityChecklist.md)
- [Histórico de versões](../CHANGELOG.md)
- [Como contribuir](../CONTRIBUTING.md)
- [Política de segurança](../SECURITY.md)


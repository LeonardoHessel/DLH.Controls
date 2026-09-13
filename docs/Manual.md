# Manual do DLH Controls

Este manual apresenta o uso da biblioteca **DLH Controls** pelo ponto de vista de quem desenvolve uma aplicação. A implementação disponível atualmente está no pacote `DLH.Controls.Wpf`, para Windows com .NET 10 e WPF.

## Sumário

1. [Instalação](#1-instalação)
2. [Preparação do XAML](#2-preparação-do-xaml)
3. [TabControl](#3-tabcontrol)
4. [DataGridView](#4-datagridview)
5. [ScrollBar e ContextMenu](#5-scrollbar-e-contextmenu)
6. [Temas e personalização](#6-temas-e-personalização)
7. [Acessibilidade e teclado](#7-acessibilidade-e-teclado)
8. [Desempenho](#8-desempenho)
9. [Diagnóstico de problemas](#9-diagnóstico-de-problemas)
10. [Aplicação de demonstração](#10-aplicação-de-demonstração)
11. [Documentos de referência](#11-documentos-de-referência)

## 1. Instalação

Instale o pacote no projeto WPF:

```powershell
dotnet add package DLH.Controls.Wpf --version 0.4.0-preview.1
```

Ou adicione a referência diretamente ao arquivo do projeto:

```xml
<ItemGroup>
    <PackageReference Include="DLH.Controls.Wpf" Version="0.4.0-preview.1" />
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

Configure `GroupMemberPath` e `IsGroupingEnabled="True"` para agrupar a visualização. O cabeçalho de cada grupo continua personalizável pela coleção `GroupStyle` nativa.

O controle oferece duas apresentações de detalhes:

- **embutida:** defina `RowDetailsTemplate` e ative `ShowRowDetailsOnSelection`; a altura da linha aumenta para acomodar o conteúdo;
- **flutuante:** defina `RowDetailsPopupTemplate` e ative `ShowRowDetailsPopupOnClick`; o conteúdo aparece sobre a interface sem deslocar as demais linhas.

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  ShowRowDetailsPopupOnClick="True"
                  RowDetailsPopupPlacement="Bottom"
                  RowDetailsPopupVerticalOffset="4">
    <dlh:DataGridView.RowDetailsPopupTemplate>
        <DataTemplate>
            <Border MaxWidth="520"
                    Padding="16,12"
                    Background="#292B2F"
                    BorderBrush="#50545C"
                    BorderThickness="1"
                    CornerRadius="8">
                <StackPanel>
                    <TextBlock Text="{Binding ShipmentNumber}"
                               FontWeight="SemiBold" />
                    <TextBlock Margin="0,6,0,0"
                               Text="{Binding Notes}"
                               TextWrapping="Wrap" />
                </StackPanel>
            </Border>
        </DataTemplate>
    </dlh:DataGridView.RowDetailsPopupTemplate>
</dlh:DataGridView>
```

No modo flutuante, clicar na linha abre o painel; clicar novamente na mesma linha o fecha; clicar em outro registro troca o conteúdo; e clicar fora fecha o painel. `RowDetailsPopupContentStyle` personaliza o contêiner. `RowDetailsPopupPlacement`, `RowDetailsPopupHorizontalOffset` e `RowDetailsPopupVerticalOffset` controlam a posição. `IsRowDetailsPopupOpen` e `RowDetailsPopupItem` expõem o estado, e `CloseRowDetailsPopup()` fecha o painel por código. Na ausência de `RowDetailsPopupTemplate`, o controle reutiliza `RowDetailsTemplate`.

### 4.11 Fixação aderente

Ative `CanPinRows` e `CanPinColumns` para permitir que o usuário fixe registros pelo menu da linha e colunas pelo menu do cabeçalho. As duas opções são `False` por padrão. O comando de fixação fica apenas no menu de opções; as células e os cabeçalhos não recebem um botão permanente.

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  CanPinRows="True"
                  CanPinColumns="True"
                  MaxPinnedRows="5"
                  MaxPinnedColumns="4"
                  ShowPinnedBoundarySeparator="True"
                  PinnedBoundarySeparatorBrush="#42A5E8"
                  PinnedBoundarySeparatorThickness="2"
                  RowKeyMemberPath="Id" />
```

O item fixado acompanha a rolagem enquanto sua posição natural estiver visível. Ao alcançar uma borda, ele adere a ela e permanece visível. Linhas podem aderir ao topo ou à parte inferior; colunas podem aderir à esquerda ou à direita. Quando há vários itens fixados, eles se acumulam na ordem encontrada, sem se sobrepor. O cruzamento entre linhas e colunas fixadas mantém o mesmo conteúdo, fundo, altura e alinhamento do grid.

`MaxPinnedRows` e `MaxPinnedColumns` limitam quantos itens podem ser fixados e têm padrões 5 e 4. Reduzir qualquer um dos dois em tempo de execução desfixa automaticamente os itens fixados mais recentes até respeitar o novo limite, disparando `RowUnpinned`/`ColumnUnpinned` para cada um. `ShowPinnedBoundarySeparator` controla a linha que separa as regiões fixa e rolável. A cor vem de `PinnedBoundarySeparatorBrush`, e `PinnedBoundarySeparatorThickness` aceita um valor finito maior que zero. O separador é aplicado automaticamente à borda em uso.

As operações `PinRow`, `UnpinRow`, `ToggleRowPin`, `PinColumn`, `UnpinColumn` e `ToggleColumnPin` retornam `True` somente quando alteram o estado. Use `UnpinAllRows()` e `UnpinAllColumns()` para limpar os grupos. `PinnedRows` e `PinnedColumns` são coleções somente leitura; os eventos `RowPinned`, `RowUnpinned`, `ColumnPinned` e `ColumnUnpinned` informam cada alteração. Ocultar uma coluna fixada (`Visibility` diferente de `Visible`) a desfixa automaticamente e libera seu lugar em `MaxPinnedColumns`; ela precisa ser fixada de novo explicitamente quando voltar a ficar visível.

As colunas fixadas participam de `CaptureState`, `RestoreState`, `SaveState` e `LoadState` por suas chaves estáveis. Para persistir linhas fixadas, configure `RowKeyMemberPath` com uma propriedade de texto única e estável. Caminhos aninhados usam segmentos separados por ponto, como `Identity.Key`. Linhas sem chave configurada continuam fixáveis durante a execução, mas não são incluídas no estado salvo.

### 4.12 Estados da coleção

```xml
<dlh:DataGridView EmptyMessage="Nenhum item encontrado."
                  LoadingMessage="Carregando..."
                  IsLoading="{Binding IsLoading}"
                  ErrorMessage="{Binding ErrorMessage}" />
```

A prioridade é: erro, carregamento, coleção vazia e, por fim, dados.

### 4.13 Densidade e rolagem

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

## 5. ScrollBar e ContextMenu

O `ScrollBar` pode ser usado diretamente em qualquer conteúdo rolável. `Thickness` controla sua espessura e `CornerRadius` é limitado visualmente à metade dessa medida. Cores, opacidade, sombra e botões direcionais são opcionais.

```xml
<dlh:ScrollBar Orientation="Vertical"
               Minimum="0" Maximum="100" Value="30"
               Thickness="10" CornerRadius="5"
               ShowButtons="False" />
```

O `ContextMenu` aceita os mesmos `MenuItem`, comandos, bindings, atalhos, itens marcáveis e submenus do WPF:

```xml
<Button Content="Opções">
    <Button.ContextMenu>
        <dlh:ContextMenu CornerRadius="8" ItemPadding="12,8">
            <MenuItem Header="Atualizar" InputGestureText="F5" />
            <dlh:ToggleMenuItem Header="Exibir detalhes"
                                IsChecked="{Binding ShowDetails}"
                                CheckedIcon="●" UncheckedIcon="○" />
            <dlh:ChoiceMenuItem Header="Tema" SelectedValue="{Binding Theme}" SelectedIndex="0">
                <dlh:ChoiceMenuOption Content="Escuro" Value="Dark" Icon="☾" />
                <dlh:ChoiceMenuOption Content="Claro" Value="Light" Icon="☀" />
            </dlh:ChoiceMenuItem>
            <Separator />
            <MenuItem Header="Exportar">
                <MenuItem Header="Arquivo CSV" />
            </MenuItem>
        </dlh:ContextMenu>
    </Button.ContextMenu>
</Button>
```

O `DataGridView` usa os dois componentes internamente. Eles também estão disponíveis para controles próprios e aplicações consumidoras.

Os itens são organizados em cinco colunas: ícone, título, valor, atalho e seta. `MenuItemAssist.Value` permite preencher a coluna de valor de qualquer item, enquanto `MenuItemAssist.ValueTemplate` permite personalizar sua apresentação. Submenus usam a mesma estrutura.

`SubmenuPlacementDirection="Left"` move somente a coluna da seta para o início e faz o submenu abrir para a esquerda. As colunas de ícone, título, valor e atalho não mudam de ordem. O valor padrão é `Right`.

Cada menu possui um caminho ativo. Clicar em outro ramo fecha os submenus que não pertencem ao novo caminho. Um componente externo pode chamar `ContextMenu.ActivatePath(item)`, `CollapseAfter(item)` ou `CollapseAll()`. Em menus fixos, marque a raiz com `MenuInteraction.IsScopeRoot="True"` e chame os métodos equivalentes de `MenuInteraction`, informando a raiz. Isso permite que um clique externo feche toda a árvore sem interferir em outros menus da janela.

No `ToggleMenuItem`, `IsChecked` é bidirecional por padrão e os ícones podem variar entre `CheckedIcon` e `UncheckedIcon`. No `ChoiceMenuItem`, o clique sobre o item percorre as opções e a seta abre o submenu para seleção direta. `SelectedContent` ocupa a coluna de valor. Use `SelectedIndex`, `SelectedItem` ou `SelectedValue` para manter a escolha no view model.

## 6. Temas e personalização

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

## 7. Acessibilidade e teclado

- Preserve contraste suficiente entre texto, fundo, foco e seleção.
- Forneça textos acessíveis para cabeçalhos compostos apenas por ícones.
- Não remova indicadores de foco sem oferecer uma alternativa visível.
- Teste navegação com `Tab`, setas e `Ctrl+Tab`.
- No `TabControl`, `Ctrl+Shift` com as setas reorganiza abas quando permitido.
- No `DataGridView`, os comportamentos de teclado do `DataGrid` continuam disponíveis.

Consulte a [lista de verificação de acessibilidade](AccessibilityChecklist.md) antes de publicar uma aplicação.

## 8. Desempenho

O `DataGridView` mantém virtualização de linhas e colunas habilitada. Para preservar esse comportamento:

- evite colocar o grid dentro de outro `ScrollViewer`;
- prefira coleções observáveis e atualizações incrementais;
- mantenha templates de célula simples em tabelas muito grandes;
- evite medir colunas extensas com `Width="Auto"` quando não for necessário.

## 9. Diagnóstico de problemas

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

## 10. Aplicação de demonstração

Clone o repositório e execute:

```powershell
dotnet run --project samples/DLH.Controls.Wpf.Demo -c Release
```

A demonstração permite experimentar os controles, alterar configurações e observar diferentes posições, temas, densidades, modos de seleção e comportamentos.

## 11. Documentos de referência

- [Referência da API](ApiReference.md)
- [Guia detalhado do TabControl](TabControl.md)
- [Integração com dados, MVVM e temas](Integration.md)
- [Lista de verificação de acessibilidade](AccessibilityChecklist.md)
- [Histórico de versões](../CHANGELOG.md)
- [Como contribuir](../CONTRIBUTING.md)
- [Política de segurança](../SECURITY.md)

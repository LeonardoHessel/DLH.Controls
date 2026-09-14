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

Para permitir seleção múltipla, defina `AllowMultipleSelection="True"`. `GetBatchSelection()` retorna uma fotografia dos itens e células selecionados; `BatchSelectionChangedCommand` acompanha as mudanças e `BatchActionCommand` pode ser acionado por `ExecuteBatchAction()`.

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

## Ordenação por múltiplas colunas

```xml
<dlh:DataGridView IsMultiColumnSortEnabled="True" />
```

- Um clique inicia ou alterna a ordenação de uma coluna.
- `Shift+clique` acrescenta a coluna como novo critério.
- `Ctrl+clique` remove a coluna da ordenação.
- O número ao lado da seta informa a prioridade de cada critério.

Por código, use `ApplySort(column, direction, append)` e `RemoveSort(column)`. A ordem completa também é preservada por `CaptureState()`.

## Filtros por coluna

Use `SetFilter` para combinar filtros em diferentes colunas. O caminho padrão vem de `SortMemberPath` e pode ser substituído por `DataGridView.FilterMemberPath`.

```csharp
grid.SetFilter(grid.Columns[0], "EXP-2026", DataGridViewFilterOperator.StartsWith);
grid.SetFilter(grid.Columns[3], "50", DataGridViewFilterOperator.GreaterThan);
grid.ClearFilter(grid.Columns[3]);
grid.ClearFilters();
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

## Edição e validação

As células permanecem somente leitura por padrão. Ative `IsCellEditingEnabled="True"` e use as regras de validação normais dos bindings WPF. `ShowValidationErrors` controla o destaque, `ValidationErrorBrush` define sua cor e `CellEditEndingCommand` recebe um `DataGridViewCellEditContext` que pode cancelar a confirmação.

## Exportação CSV

`ExportCsv` grava as linhas da visualização atual e somente as colunas visíveis, na ordem apresentada. Assim, filtros e ordenações ativos são respeitados. `DataGridViewCsvOptions` configura cabeçalhos, delimitador, cultura e um seletor para colunas com templates.

```csharp
using var file = File.Create("dados.csv");
grid.ExportCsv(file, new DataGridViewCsvOptions { Delimiter = ";" });
```

## Agrupamento e detalhes de linha

Defina `GroupMemberPath` e ative `IsGroupingEnabled` para criar grupos sem substituir agrupamentos externos. O modo embutido continua disponível: `ShowRowDetailsOnSelection` apresenta o `RowDetailsTemplate` dentro do grid ao selecionar uma linha, e `SetRowDetailsVisibility(item, visible)` controla uma linha materializada por código.

```xml
<dlh:DataGridView IsGroupingEnabled="True"
                  GroupMemberPath="Status"
                  ShowRowDetailsOnSelection="True">
    <dlh:DataGridView.RowDetailsTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Notes}" />
        </DataTemplate>
    </dlh:DataGridView.RowDetailsTemplate>
</dlh:DataGridView>
```

Para apresentar os detalhes sem alterar a altura das linhas, ative o painel flutuante. Um clique abre os detalhes da linha, um segundo clique na mesma linha fecha o painel e um clique em outra linha troca o conteúdo. Clicar fora do painel também o fecha.

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

`RowDetailsPopupContentStyle` personaliza o contêiner, e as propriedades `RowDetailsPopupPlacement`, `RowDetailsPopupHorizontalOffset` e `RowDetailsPopupVerticalOffset` controlam a posição. Consulte `IsRowDetailsPopupOpen` e `RowDetailsPopupItem` para conhecer o estado atual ou chame `CloseRowDetailsPopup()` para fechar o painel por código. Quando `RowDetailsPopupTemplate` não é informado, o controle reutiliza `RowDetailsTemplate`.

## Fixação aderente de linhas e colunas

A fixação é opcional e permanece desativada por padrão. Ative `CanPinRows` e `CanPinColumns` para disponibilizar a ação de fixar nos menus de contexto das linhas e dos cabeçalhos. O ícone de fixação aparece no menu de opções, sem ocupar espaço permanente nas células ou nos cabeçalhos.

```xml
<dlh:DataGridView ItemsSource="{Binding Shipments}"
                  CanPinRows="True"
                  CanPinColumns="True"
                  MaxPinnedRows="5"
                  MaxPinnedColumns="4"
                  ShowPinnedBoundarySeparator="True"
                  PinnedBoundarySeparatorBrush="#42A5E8"
                  PinnedBoundarySeparatorThickness="2" />
```

Uma linha ou coluna fixada permanece em sua posição natural enquanto estiver visível. Durante a rolagem, ela adere à borda somente quando sair da área visível. Vários itens fixados se acumulam sem sobreposição: linhas aderem ao topo ou à parte inferior e colunas aderem à esquerda ou à direita, conforme o sentido da rolagem. As interseções entre linhas e colunas fixadas preservam o conteúdo, a altura e o alinhamento originais.

Ao clicar em uma célula aderente para selecioná-la ou editá-la, o grid rola até a célula original, preservando os comportamentos de seleção, edição e validação. O menu de contexto da célula aderente atua sobre a coluna original. Ocultar uma coluna fixada a desfixa automaticamente e libera seu lugar em `MaxPinnedColumns`; para fixá-la de novo depois de reexibida, chame `PinColumn` explicitamente. Substituir `ItemsSource` também atualiza o conteúdo das colunas aderentes.

Reduzir `MaxPinnedRows` ou `MaxPinnedColumns` em tempo de execução desfixa automaticamente os itens fixados mais recentes até respeitar o novo limite, disparando `RowUnpinned`/`ColumnUnpinned` para cada um.

`ShowPinnedBoundarySeparator` exibe a divisão entre a área fixa e a área rolável. `PinnedBoundarySeparatorBrush` define a cor e `PinnedBoundarySeparatorThickness` aceita valores finitos maiores que zero. A divisão acompanha qualquer uma das quatro bordas em que exista conteúdo aderente.

Por código, use `PinRow`, `UnpinRow`, `ToggleRowPin`, `PinColumn`, `UnpinColumn` e `ToggleColumnPin`. `UnpinAllRows()` e `UnpinAllColumns()` limpam cada grupo. As coleções somente leitura `PinnedRows` e `PinnedColumns` representam o estado atual, e os eventos `RowPinned`, `RowUnpinned`, `ColumnPinned` e `ColumnUnpinned` notificam as alterações.

O estado das colunas fixadas é salvo com as demais configurações do grid. Para também persistir linhas fixadas, defina `RowKeyMemberPath` com uma propriedade textual, única e estável de cada registro. Caminhos aninhados são aceitos com segmentos separados por ponto, como `Identity.Key`.

## Persistir o layout das colunas

Defina uma chave estável com `SortMemberPath` ou com a propriedade anexada `ColumnKey`. O estado inclui ordem, largura, visibilidade e ordenação:

```xml
<DataGridTextColumn Header="Descrição"
                    dlh:DataGridView.ColumnKey="description"
                    Binding="{Binding Description}"
                    SortMemberPath="Description" />
```

```csharp
using (var output = File.Create("grid-layout.json"))
    grid.SaveState(output);

using (var input = File.OpenRead("grid-layout.json"))
    grid.LoadState(input);

grid.ResetState(); // retorna ao layout capturado quando o controle foi carregado
```

Colunas removidas são ignoradas e colunas novas são colocadas depois das conhecidas. Um estado inválido é rejeitado antes de modificar o controle.

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

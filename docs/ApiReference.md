# Referência da API — TabControl

Revisão para estabilização, baseada na implementação atual. Namespace: `DLH.Controls.Wpf`. Usar no thread de interface WPF. Os membros herdados de TabControl mantêm seus contratos WPF.

## Propriedades próprias

| Propriedade | Padrão | Contrato |
|---|---|---|
| CornerRadius | 12 | Raio uniforme, finito e não negativo para todas as curvas |
| HeaderIndent | NaN | Recuo automático; 0 encosta na borda; valores finitos não negativos |
| TabSpacing | 0 | Espaço no eixo da faixa, finito e não negativo |
| IsShadowEnabled | true | Liga/desliga a sombra sem apagar parâmetros |
| ShadowColor | #494949 | Cor da sombra |
| ShadowOpacity | 0.5 | Número finito entre 0 e 1 |
| ShadowBlurRadius | 10 | Finito e não negativo |
| ShadowDepth | 0 | Finito e não negativo |
| ShadowDirection | 315 | Ângulo finito |
| CanReorderTabs | true | Controla arraste, teclado e MoveTab; não bloqueia RestoreState |
| TabDragCursor | ScrollWE | Cursor não nulo; não troca automaticamente para ScrollNS nas laterais |
| MinimumDragDistance | 5 | Pixels físicos no eixo; início somente acima do limiar |
| IsDragPreviewEnabled | true | Prévia flutuante; desativar não impede reorganizar |
| IsAnimationEnabled | true | Animações de arraste |
| DragAnimationDuration | 180 ms | De zero a dez segundos |
| DragPreviewOpacity | 0.94 | Finito entre 0 e 1 |
| CanCloseTabs | true | Permissão global, inclusive RequestCloseTab e comando roteado |
| ShowCloseButtons | false | Preferência visual; não é uma permissão de remoção |
| CloseTabCommand | null | ICommand síncrono; parâmetro é o item, não necessariamente TabItem |
| CanAddTabs | false | Exibe a ação de adicionar; ainda depende de comando ou evento capaz de atender a solicitação |
| AddTabCommand | null | ICommand que cria o item; recebe AddTabCommandParameter |
| AddTabCommandParameter | null | Contexto opcional enviado ao comando ou evento de criação |
| AddTabContent | + | Conteúdo do botão independente da coleção |
| AddTabContentTemplate | null | DataTemplate opcional para o conteúdo do botão |
| CanRenameTabs | false | Autoriza edição direta; não torna um destino de dados gravável |
| TabHeaderPath | null | Caminho textual editável em itens de dados, como Header ou Document.Title |
| RenameTabCommand | null | ICommand que recebe TabRenameRequest e atualiza o modelo |
| RenameActivation | F2AndDoubleClick | None, F2, DoubleClick ou combinação dos dois gestos |
| ItemKeyPath | Id | Caminho de uma chave string; aceita membros separados por ponto |

Propriedades anexadas: `CanCloseTab` (true) protege um contêiner individual; `TabKey` (null) fornece a chave de itens explícitos. As chaves devem ser não vazias, únicas e estáveis.

A propriedade herdada `TabStripPlacement` aceita Top, Bottom, Left e Right. `BorderThickness` usa o maior lado como traço uniforme. `Padding`, cores e fontes são propriedades herdadas. Valores padrão acima são os da biblioteca, não os ajustes do visualizador.

## Operações e eventos

| Operação | Resultado e limites |
|---|---|
| MoveTab(oldIndex, newIndex) | bool; índices base zero; exige fonte mutável sem ordenação/filtro/grupo e CanReorderTabs; selecionado é preservado |
| RequestCloseTab(item) | bool; respeita permissões, CanExecute e cancelamento; true somente se removido |
| RequestAddTab(parameter?) | bool; usa AddTabCommand ou, na ausência dele, AddTabRequested |
| BeginRenameTab(item) | bool; abre o editor quando a funcionalidade e o destino de dados permitem |
| CommitTabRename() | bool; confirma texto diferente após veto/CanExecute e encerra somente quando aceito |
| CancelTabRename() | Encerra o editor sem modificar o título |
| CaptureState(keySelector?) | TabControlState; captura ordem e seleção por chaves |
| RestoreState(state, keySelector?) | Valida estrutura/chaves, ignora ausentes e anexa novos itens; exige fonte mutável |
| SaveState(stream, keySelector?) / LoadState(stream, keySelector?) | JSON; não fecham o stream do chamador |
| CaptureConfiguration() | TabControlConfiguration com valores textuais em cultura invariável |
| ValidateConfiguration(configuration) | Estático; valida sem alterar controle |
| RestoreConfiguration(configuration) | Valida todos os valores antes de aplicar; preserva bindings com SetCurrentValue |
| ResetConfiguration() | Aplica padrões registrados; não remove documentos; não significa restaurar o tema ou ajustes iniciais da aplicação |

`TabReordered` é emitido após mudança efetiva, com Item, OldIndex, NewIndex e Reason (Programmatic, Drag, Keyboard). Não ocorre em cancelamento, no-op ou RestoreState. `TabClosing` permite veto por Cancel; `TabClosed` ocorre após remoção confirmada. `AddTabRequested` atende criação sem comando. `TabRenaming` e `TabRenameRequested` permitem veto; `TabRenamed` sinaliza aceitação. `StateRestored` sinaliza a conclusão da restauração da ordem/seleção. São eventos CLR, não eventos roteados. `CloseTab` e `AddTab` são RoutedUICommand.

Quando `AddTabCommand` existe, ele tem precedência sobre `AddTabRequested`; nunca são executados os dois para uma solicitação. A ação `+` não integra Items, seleção, índices, estado ou reordenação. Quando `RenameTabCommand` existe, recebe `TabRenameRequest` com Item, OldHeader e NewHeader. Sem comando, `TabHeaderPath` precisa terminar em propriedade string gravável; itens TabItem explícitos aceitam Header textual. O controle coordena uma edição síncrona e não persiste o editor em andamento.

Quando CloseTabCommand está definido, ele é responsável pela remoção síncrona. A biblioteca não aguarda Tasks nem oferece confirmação assíncrona. Alterar diretamente a coleção é responsabilidade da aplicação e não passa pelas permissões/eventos do controle.

## Persistência e erros

Os dois formatos têm Version=1, independentemente da versão NuGet. Estado não salva conteúdo das páginas. Configuração inclui CanAddTabs, CanRenameTabs, TabHeaderPath e RenameActivation, mas não salva comandos, parâmetros, conteúdo/templates, itens, seleção ou o tema da aplicação. Propriedades ausentes mantêm o valor atual; nomes desconhecidos e versões incompatíveis são rejeitados. Cores são valores, não chaves DynamicResource. Cursores/brushes personalizados precisam ser convertíveis pelos conversores WPF; CaptureConfiguration pode rejeitá-los.

Chaves inválidas ou fontes incompatíveis geram InvalidOperationException; valores/versões inválidos geram ArgumentException; JSON inválido gera JsonException e conteúdo JSON nulo em LoadState gera InvalidDataException. Falhas de streams pertencem ao chamador. A validação prévia não fornece rollback de efeitos de comandos, eventos ou coleções personalizados que lancem exceções.

## Tipos auxiliares e compatibilidade

`TabControlItem` é opcional: o `System.Windows.Controls.TabItem` nativo também é aceito. `TabControlState`, `TabControlConfiguration` e os argumentos de eventos são públicos. `TabCursors.ClosedHand` e `TabSpacingConverter` também são públicos.

Os nomes foram padronizados durante a fase de pré-lançamento: `TabControl` substitui `CustomTabControl` e `TabControlItem` substitui `CustomTabItem`.

# Referência da API — ScrollBar

`ScrollBar` deriva de `System.Windows.Controls.Primitives.ScrollBar`. `Thickness` controla o eixo transversal conforme `Orientation`; `CornerRadius` deve ser uniforme e é limitado visualmente à metade da espessura. `TrackBrush`, `ThumbBrush`, `ThumbHoverBrush`, `ThumbPressedBrush`, `TrackPadding` e `ThumbOpacity` controlam o visual. `ShowButtons` exibe comandos direcionais. A sombra é opt-in por `IsShadowEnabled` e usa `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` e `ShadowDepth`.

# Referência da API — ContextMenu

`ContextMenu` deriva de `System.Windows.Controls.ContextMenu` e preserva `Items`, `ItemContainerStyle`, comandos, bindings, atalhos, teclado, itens marcáveis e submenus nativos. `CornerRadius`, `Padding`, `ItemPadding`, `IconSize` e `IconColumnWidth` definem a geometria. `Background`, `Foreground`, `BorderBrush`, `HoverBrush`, `CheckedBrush` e `SeparatorBrush` definem as cores. `DisabledOpacity` controla itens indisponíveis. A sombra usa `IsShadowEnabled`, `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` e `ShadowDepth`.

`ToggleMenuItem` especializa o item marcável com `IsChecked` bidirecional por padrão, `CheckedIcon` e `UncheckedIcon`. `ChoiceMenuItem` representa uma escolha entre vários valores. `SelectedIndex`, `SelectedItem` e `SelectedValue` são bidirecionais por padrão; `DisplayMemberPath`, `SelectedValuePath` e `IconMemberPath` adaptam modelos externos. `CycleDirection`, `IsCycleWrappingEnabled` e `DropDownButtonWidth` controlam o clique cíclico e a área que abre o submenu. `ChoiceMenuOption` é o modelo declarativo simples com `Content`, `Value`, `Icon` e `IsEnabled`.

Em C#, quando `System.Windows.Controls` e `DLH.Controls.Wpf` estiverem importados simultaneamente, use um alias ou o nome qualificado para distinguir os dois tipos: `using ControlsContextMenu = DLH.Controls.Wpf.ContextMenu;`. Em XAML, o prefixo `dlh:` já elimina a ambiguidade.

# Referência da API — DataGridView

`AllowMultipleSelection` alterna entre seleção simples e estendida. `GetBatchSelection()` retorna `DataGridViewBatchSelection`; `BatchSelectionChangedCommand` recebe mudanças e `BatchActionCommand` é executado por `ExecuteBatchAction()`.

`IsCellEditingEnabled` libera a edição nativa. `ShowValidationErrors` e `ValidationErrorBrush` configuram o retorno visual das regras WPF. `CellEditEndingCommand` recebe `DataGridViewCellEditContext`; defina `Cancel=true` para impedir a confirmação.

`ExportCsv(TextWriter, options)` e `ExportCsv(Stream, options, encoding)` exportam a visualização corrente. `DataGridViewCsvOptions` oferece `Delimiter`, `IncludeHeaders`, `Culture` e `ValueSelector`.

`IsGroupingEnabled` aplica um `PropertyGroupDescription` para `GroupMemberPath`. `ShowRowDetailsOnSelection` controla os detalhes na seleção e `SetRowDetailsVisibility(item, visible)` atua em uma linha materializada.

## Persistência do layout

`DataGridView.ColumnKey` define uma chave estável por coluna. Na ausência dela, `SortMemberPath` é utilizado. Todas as colunas precisam de chaves não vazias e únicas para usar a persistência.

| Tipo ou operação | Contrato |
|---|---|
| `DataGridViewState` | Formato versionado com colunas e ordenações |
| `DataGridViewColumnState` | Chave, índice visual, largura, unidade e visibilidade |
| `DataGridViewSortState` | Chave da coluna e direção da ordenação |
| `CaptureState()` | Captura a configuração atual sem manter referências às colunas |
| `RestoreState(state)` | Valida integralmente e restaura; colunas desconhecidas são ignoradas e novas são anexadas |
| `SaveState(stream)` / `LoadState(stream)` | Serializa ou lê JSON sem fechar o stream do chamador |
| `ResetState()` | Restaura o estado inicial capturado no carregamento e informa se estava disponível |
| `StateRestored` | Emitido uma vez depois de uma restauração concluída |

O formato atual usa `Version=1`. Estados com versão desconhecida, chaves duplicadas, índices repetidos, larguras inválidas, enumerações inválidas ou nenhuma coluna visível são rejeitados antes da aplicação.

## Ordenação múltipla

## Filtros por coluna

`Filters` expõe os filtros ativos. Use `SetFilter(column, value, operador)`, `ClearFilter(column)` e `ClearFilters()`. Os operadores disponíveis são `Contains`, `Equals`, `StartsWith`, `EndsWith`, `GreaterThan` e `LessThan`. `DataGridView.FilterMemberPath` escolhe a propriedade consultada e `DataGridView.CanUserFilter` desativa filtros em uma coluna. O filtro existente na `ICollectionView` é preservado e combinado com os filtros do controle.

`IsMultiColumnSortEnabled` é `false` por padrão. Quando habilitada, clique simples substitui a ordenação, `Shift+clique` acrescenta um critério e `Ctrl+clique` remove o critério da coluna. `ApplySort(column, direction, append)` e `RemoveSort(column)` oferecem o mesmo controle por código. A propriedade anexada somente leitura `SortPriority` informa a posição, iniciando em 1 quando existem múltiplos critérios.


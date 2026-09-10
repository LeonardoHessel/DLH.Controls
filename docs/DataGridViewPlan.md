# Plano — DataGridView para WPF

## Referência multiplataforma

Controles de tabela multiplataforma oferecem colunas declarativas, leitura e formatação de propriedades, ordenação, seleção de linha ou célula, rolagem horizontal sincronizada, cabeçalho fixo, mensagem vazia, carregamento, separadores e limites de altura.

No MAUI, cabeçalho e linhas são reconstruídos em code-behind porque o cenário Android evita `CollectionView` dentro de `ScrollView`. Essa estratégia não deve ser copiada no WPF: `System.Windows.Controls.DataGrid` já possui virtualização, colunas, ordenação, cabeçalho fixo, rolagem sincronizada, edição, teclado e automação. O novo controle será uma especialização do DataGrid nativo.

## Referência web

O componente Angular em `shared/components/data-table` fornece a casca visual arredondada, cabeçalho fixo, densidades compacta/padrão/confortável, linhas alternadas, hover e estilos de seleção. A camada `list-table` acrescenta colunas dirigidas por metadados, ordenação, alinhamento, seleção múltipla, templates de célula, ações, navegação por teclado e estados de vazio, carregamento e erro.

No WPF, esses recursos serão expostos sobre o `DataGrid` nativo. Isso mantém colunas declarativas em XAML, `DataGridTemplateColumn`, ordenação, navegação por teclado, automação e virtualização.

## Nome e pacote

- Tipo público: `DLH.Controls.Wpf.DataGridView`.
- Pacote: `DLH.Controls.Wpf`, junto dos demais componentes WPF.
- Diretório: `src/DLH.Controls.Wpf/Controls/DataGridView`.
- As colunas continuam sendo `DataGridTextColumn`, `DataGridTemplateColumn` e demais tipos nativos do WPF.

## Etapa A — base visual e seleção

- Tema integrado aos recursos `DataGridView.*`.
- Propriedade `SelectionBehavior`: `None` por padrão, `Row`, `Column` ou `Cell`.
- `SelectionChangedCommand` recebe item e coluna selecionados.
- `SelectedItem` e `CurrentCell` continuam disponíveis para binding nativo.
- Separadores e espaçamento de célula configuráveis.
- Ordenação nativa, controlada por `CanUserSortColumns` e `SortMemberPath`.
- `CanUserSortColumns="False"` desativa a ordenação de toda a tabela e oculta todos os seus indicadores.
- `DefaultSortMemberPath` e `DefaultSortDirection` definem a ordenação inicial e permitem restaurá-la pelo menu do cabeçalho.
- O menu do cabeçalho oferece `Limpar ordenação` e, quando configurada, `Restaurar ordenação padrão`.
- `ShowClearSortMenuItem` e `ShowRestoreDefaultSortMenuItem` controlam independentemente a presença dessas ações no menu.
- As barras de rolagem seguem o tema do controle; `ScrollBarThickness`, `ScrollBarTrackBrush`, `ScrollBarThumbBrush` e `ScrollBarThumbHoverBrush` permitem personalização.
- As barras são sobrepostas ao corpo com margens internas e transparência, sem criar uma faixa separada nem um bloco quadrado entre os eixos.
- O raio das extremidades é calculado pela dimensão real e nunca ultrapassa metade da largura vertical ou metade da altura horizontal.
- Quando visível, a barra horizontal reserva sua própria faixa para que nenhuma linha fique encoberta.
- O polegar usa o comprimento proporcional calculado pelo `Track`, mantendo as duas extremidades dentro da área disponível; o trilho visível comunica toda a área de rolagem.
- O template remove os mínimos de aproximadamente 16 px impostos pelo tema nativo, permitindo que `ScrollBarThickness` seja respeitado até o limite validado pelo componente.
- A barra vertical possui uma coluna interna reservada; cabeçalhos e células nunca ficam sob ela, enquanto a barra horizontal ocupa a largura total.
- O fundo e o apresentador do cabeçalho atravessam também a área reservada, criando um acabamento contínuo até a borda direita; o espaço final acompanha a espessura efetiva da barra vertical.
- Indicador neutro em toda coluna ordenável e indicadores destacados para ordem crescente ou decrescente.
- `ShowSortIndicators`, `SortIconSize`, cores e geometrias permitem personalizar completamente os símbolos de ordenação.
- Reordenação nativa, controlada por `CanUserReorderColumns` e ativada por padrão.
- Menu no botão direito de qualquer cabeçalho para mostrar ou ocultar colunas, controlado por `CanUserToggleColumnVisibility`.
- Virtualização de linhas e colunas mantida.

## Etapa B — estados vazio e carregando

- `EmptyMessage` e `EmptyContentTemplate`.
- `IsLoading`, `LoadingMessage` e `LoadingContentTemplate`.
- Sobreposição de carregamento bloqueia interação com linhas antigas sem esconder cabeçalhos.
- Estado vazio aparece somente quando não há itens e não há carregamento.
- `ErrorMessage` substitui o corpo por uma mensagem de falha até ser limpo.
- `Density` oferece `Compact`, `Default` e `Comfortable`.
- `CornerRadius` controla o arredondamento da superfície completa.

## Etapa C — visualizador e cenários

- Nova área no visualizador com dados de embarques inspirados no exemplo MAUI.
- Colunas de texto, número, estado formatado e uma coluna com template/ícone.
- Alternância entre seleção desativada, linha e célula.
- Ordenação crescente/decrescente, coleção dinâmica, vazio e carregamento.
- Tema claro, escuro e cinza/laranja.

## Etapa D — testes e documentação

- Padrões, propriedades inválidas e bindings.
- Seleção por linha/célula e execução única do comando.
- Ordenação e preservação da coleção de origem.
- Filtros por coluna, combinação de critérios e preservação do filtro externo da visualização.
- Seleção múltipla opcional com comandos para ações em lote.
- Edição opt-in com integração às regras de validação do WPF e retorno visual configurável.
- Exportação CSV da visualização corrente, com escape de campos e personalização de valores.
- Atualizações de `ObservableCollection` e virtualização com volume maior.
- Estado vazio/carregando e retorno aos dados.
- Teclado, nomes acessíveis, alto contraste e DPI.
- README, referência da API, integração, changelog e validação do pacote.

## Decisões iniciais

O componente não repetirá `PropertyName`, `ValueSelector` e `ValueFormatter` do MAUI na primeira versão. No WPF, `Binding`, `SortMemberPath`, conversores e `DataGridTemplateColumn` atendem esses casos de maneira nativa e com suporte das ferramentas XAML. Uma camada de colunas própria só será adicionada se surgir uma necessidade que os tipos nativos não resolvam.

`EnableHorizontalScroll` e `EnableFixedHeader` também não serão duplicados. O WPF já expõe `HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility`, `MaxHeight` e mantém o cabeçalho fora da rolagem vertical.

## Evolução — persistência do layout

- Estado versionado para ordem, largura, unidade, visibilidade e ordenação.
- Chave estável por `DataGridView.ColumnKey`, com fallback para `SortMemberPath` único.
- Captura, restauração, JSON e retorno ao estado inicial sem definir armazenamento na biblioteca.
- Compatibilidade com colunas adicionadas ou removidas entre versões da aplicação.
- Validação completa antes da alteração e tentativa de rollback se uma aplicação personalizada lançar uma exceção.

## Evolução — ordenação múltipla

- Ativação opcional por `IsMultiColumnSortEnabled`, preservando o comportamento nativo como padrão.
- Clique simples para substituir, `Shift+clique` para acrescentar e `Ctrl+clique` para remover critérios.
- Métodos públicos para aplicação e remoção programática.
- Indicador numérico de prioridade no cabeçalho e persistência da lista ordenada de critérios.

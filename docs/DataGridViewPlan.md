# Plano — DataGridView para WPF

## Referência MAUI

O controle analisado em `controle de tabela MAUI usado como referÃªncia` oferece colunas declarativas, leitura e formatação de propriedades, ordenação, seleção de linha ou célula, rolagem horizontal sincronizada, cabeçalho fixo, mensagem vazia, carregamento, separadores e limites de altura.

No MAUI, cabeçalho e linhas são reconstruídos em code-behind porque o cenário Android evita `CollectionView` dentro de `ScrollView`. Essa estratégia não deve ser copiada no WPF: `System.Windows.Controls.DataGrid` já possui virtualização, colunas, ordenação, cabeçalho fixo, rolagem sincronizada, edição, teclado e automação. O novo controle será uma especialização do DataGrid nativo.

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
- Reordenação nativa, controlada por `CanUserReorderColumns` e ativada por padrão.
- Menu no botão direito de qualquer cabeçalho para mostrar ou ocultar colunas, controlado por `CanUserToggleColumnVisibility`.
- Virtualização de linhas e colunas mantida.

## Etapa B — estados vazio e carregando

- `EmptyMessage` e `EmptyContentTemplate`.
- `IsLoading`, `LoadingMessage` e `LoadingContentTemplate`.
- Sobreposição de carregamento bloqueia interação com linhas antigas sem esconder cabeçalhos.
- Estado vazio aparece somente quando não há itens e não há carregamento.

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
- Atualizações de `ObservableCollection` e virtualização com volume maior.
- Estado vazio/carregando e retorno aos dados.
- Teclado, nomes acessíveis, alto contraste e DPI.
- README, referência da API, integração, changelog e validação do pacote.

## Decisões iniciais

O componente não repetirá `PropertyName`, `ValueSelector` e `ValueFormatter` do MAUI na primeira versão. No WPF, `Binding`, `SortMemberPath`, conversores e `DataGridTemplateColumn` atendem esses casos de maneira nativa e com suporte das ferramentas XAML. Uma camada de colunas própria só será adicionada se surgir uma necessidade que os tipos nativos não resolvam.

`EnableHorizontalScroll` e `EnableFixedHeader` também não serão duplicados. O WPF já expõe `HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility`, `MaxHeight` e mantém o cabeçalho fora da rolagem vertical.

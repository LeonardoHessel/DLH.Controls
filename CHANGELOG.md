# Histórico de versões

## Não publicado

## 0.4.0-preview.2 — 12 de setembro de 2026

- Correção da redução reentrante de `MaxPinnedRows` e `MaxPinnedColumns`, garantindo que alterações feitas durante os eventos de desfixação respeitem os limites finais.

## 0.4.0-preview.1 — 12 de setembro de 2026 (não publicada no NuGet)

> A versão foi substituída por `0.4.0-preview.2` antes da publicação após a identificação de uma regressão de reentrada nos limites de fixação. A tag original permanece como registro histórico.

> Inclui também o conteúdo abaixo já publicado em `0.3.0-preview.2` sem uma seção própria no histórico.

- **Alteração incompatível:** `CustomTabControl` foi renomeado para `TabControl` (e seus itens para `TabControlItem`); atualize referências de tipo e recursos de estilo existentes.
- Padronização dos nomes públicos para `TabControl`, `TabControlItem` e `ScrollBar`.
- Novo `ScrollBar` reutilizável com orientação, espessura, cores, estados, raio limitado, sombra e botões opcionais.
- `DataGridView` migrado para a barra compartilhada, preservando suas propriedades de compatibilidade.
- Novo `ContextMenu` reutilizável com superfície, espaçamento, ícones, itens marcáveis, submenus, separadores, estados e sombra configuráveis.
- Menu de cabeçalho do `DataGridView` migrado para o `ContextMenu` compartilhado, preservando ordenação, filtros e visibilidade das colunas.
- Laboratório visual para configurar todas as opções próprias de `ScrollBar` e `ContextMenu` durante a execução.
- Snapshot visual dos controles compartilhados e testes da tela de configuração, incluindo tratamento de valores inválidos.
- Correção da composição entre `MenuItem` e `Separator` e da propagação visual para itens e submenus.
- Botões direcionais do `ScrollBar` agora seguem as cores e os estados do componente.
- Seletor visual de cores em todas as propriedades cromáticas do laboratório, preservando a entrada hexadecimal manual.
- Correção da atualização de cores do `ContextMenu` após a primeira abertura, incluindo separadores, itens, estados e submenus.
- Correção do template do separador para renderizar `SeparatorBrush` no lugar da linha nativa do WPF.
- Detalhes de registro em painel flutuante opcional e completamente personalizável, com alternância por clique e fechamento externo ou programático.
- Fixação aderente opcional de linhas e colunas nas quatro bordas, acionada pelos menus de opções e com persistência por chaves estáveis.
- Separador configurável entre as regiões fixa e rolável, com cor, espessura e visibilidade próprias.
- Correções de composição, fundo, alinhamento e continuidade visual quando múltiplas linhas e colunas fixadas se cruzam.
- Menus de contexto do `DataGridView` reorganizados por escopo de ação (coluna, linha, fixação).
- Correção de vazamento de memória nos overlays de coluna fixada ao reconstruir a visualização.
- Correção de métricas obsoletas em linhas fixadas filtradas ou reordenadas.
- Overlays de fixação passam a ser estritamente visuais: seleção e edição permanecem sob controle do grid original, incluindo colunas de caixa de seleção.
- Reduzir `MaxPinnedRows`/`MaxPinnedColumns` em tempo de execução agora desfixa automaticamente os itens mais recentes em excesso.
- Ocultar uma coluna fixada agora a desfixa e libera seu lugar em `MaxPinnedColumns`.
- `RestoreState` não republica mais eventos de fixação para itens cujo estado não mudou, incluindo no rollback de uma restauração com falha.
- `RowKeyMemberPath` passa a aceitar caminhos aninhados (ex.: `Identity.Key`).
- Indicadores de ordenação fixados só são atualizados quando a ordenação de fato é aceita pela coluna.

- Persistência versionada do layout das colunas e da ordenação.
- Ordenação por múltiplas colunas com prioridade visual.
- Filtros combináveis por coluna, com operadores textuais e comparativos.
- Seleção múltipla opcional e comandos para seleção e ações em lote.
- Edição opcional de células, comando de confirmação/cancelamento e destaque configurável de erros de validação.
- Exportação CSV das linhas e colunas visíveis, respeitando filtros, ordenação e posição das colunas.
- Agrupamento opcional por propriedade e detalhes de linha expansíveis ou exibidos na seleção.
- Suítes WPF integradas ao `dotnet test`, com categorias, relatório TRX e isolamento por processo.
- Validação do pacote em uma aplicação WPF temporária que instala somente o arquivo NuGet gerado.
- Comparação visual automatizada com imagem de referência, tolerância a antialiasing e artefato de diferenças.
- Primeiros testes STA independentes do `DataGridView`, executáveis individualmente pelo Visual Studio e `dotnet test`.
- CI com compilação separada em Debug e Release e anexos das comparações visuais no relatório de testes.
- Coleta de cobertura de código integrada à validação completa e publicada com os artefatos de teste.
- Contrato versionado da API pública para detectar alterações incompatíveis antes do empacotamento.
- Snapshot visual independente do `DataGridView`, com imagens atual e de diferenças anexadas ao resultado.
- Cenário integrado de filtro, ordenação, layout e exportação, mais matriz cultural `pt-BR` e `en-US`.
- Testes STA individuais para seleção múltipla, comandos em lote, edição opt-in e aparência da validação.

## 0.2.0-preview.4 — 10 de setembro de 2026

- Corrige a exibição do QR Code do Pix no README publicado pelo NuGet.
- Substitui o bloco HTML por uma imagem em Markdown compatível com a galeria.

## 0.2.0-preview.3 — 10 de setembro de 2026

- Novo manual de uso em português-BR, com instalação, exemplos, configuração, acessibilidade, desempenho e diagnóstico de problemas.
- README reorganizado para apresentar a biblioteca DLH Controls e seus componentes antes da implementação WPF atual.
- Exemplos de instalação atualizados para a versão mais recente.
- Links e imagens da documentação revisados para exibição no GitHub e no NuGet.

## 0.2.0-preview.2 — 10 de setembro de 2026

- Adição do QR Code e da chave Pix ao README.
- GitHub Sponsors oferecido como alternativa para contribuições internacionais.
- Configuração do botão Sponsor por meio de `.github/FUNDING.yml`.
- README publicado dentro do pacote NuGet.

## 0.2.0-preview.1 — 10 de setembro de 2026

- Novo `DataGridView` com seleção configurável, ordenação visual e colunas reordenáveis, redimensionáveis e ocultáveis.
- Estados de carregamento, vazio e erro.
- Barras de rolagem customizáveis, densidades e separadores.
- Menu de cabeçalho para gerenciar colunas e ordenação.
- Instruções de uso com Paket.

## 0.1.0-preview.4 — 10 de setembro de 2026

- Ação opcional `+` para solicitar novas abas por comando ou evento, sem criar um item artificial na coleção.
- Edição opcional do título por `F2`, duplo clique e API, com confirmação, cancelamento, comandos e eventos de validação.
- Configuração e visualizador atualizados para criação e renomeação nas quatro posições.
- Suíte comportamental ampliada de 108 para 123 cenários.

## 0.1.0-preview.3 — 5 de setembro de 2026

- Suporte robusto a cabeçalhos alterados em execução, incluindo texto, idioma, fonte, overflow e cancelamento seguro de uma prévia desatualizada.
- Tratamento de coleções dinâmicas, troca de `ItemsSource`, mutações durante arraste e seleção independente entre controles que compartilham documentos.
- Nomes acessíveis contextuais para fechamento, como “Fechar Editor”, com fallback para cabeçalhos simples.
- Novos cenários do visualizador para idiomas, títulos variáveis, coleção compartilhada, mutações agendadas e distinção entre estado do modelo e estado visual.
- Linha de base reproduzível para o custo de atualização do contorno.
- 108 cenários automatizados de integração, além das suítes de configuração e inspeção manual documentada.

Validações ainda assistidas: leitor de tela real, alto contraste e escalas reais de DPI.

## 0.1.0-preview.2 — 3 de setembro de 2026

- Primeira prévia publicada automaticamente pelo GitHub Releases com Trusted Publishing do NuGet.
- TabControl com superfície contínua, quatro posições, raio e sombra configuráveis.
- Reordenação por arraste e teclado, prévia animada, fechamento controlado, persistência de organização e configurações.

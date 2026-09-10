# Histórico de versões

## Não publicado

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

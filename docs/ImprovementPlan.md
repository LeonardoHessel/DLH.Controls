# Plano de melhorias — DLH.Controls.Wpf

Data: 4 de setembro de 2026
Status: proposta para planejamento; não representa funcionalidades implementadas ou compromissos da versão 1.0.0.

## 1. Objetivo e escopo

Evoluir a robustez, a acessibilidade e a capacidade de demonstrar o TabControl da biblioteca DLH.Controls.Wpf, aproveitando os cenários observados no implementaÃ§Ã£o MAUI de referÃªncia. A implementação continua específica de WPF. Não se propõe transportar os problemas de layout do Android nem dividir o fundo e o cabeçalho do componente WPF: a superfície unificada existente deve ser preservada.

O plano contempla cinco melhorias no componente e quatro cenários no visualizador. A aplicação aplicaÃ§Ã£o de demonstraÃ§Ã£o e a implementação MAUI estão fora do escopo de alterações.

Princípios:

- Preservar as APIs já publicadas sempre que possível.
- Distinguir dados do documento, estado visual e estado de organização das abas.
- Medir antes de otimizar.
- Não considerar testes simulados equivalentes à validação com mouse, leitor de tela ou DPI real.
- Implementar cache de páginas somente após demonstrar necessidade e definir limites de memória.

## 2. Situação de referência

O componente oferece quatro posições, contorno contínuo, raio uniforme, sombra configurável, reordenação por mouse/teclado, fechamento controlado e captura/restauração de estado e configurações.

A cobertura anteriormente executada inclui 86 cenários de integração, testes complementares de configuração e consumo do pacote público em projeto independente. Essa cobertura não substitui os novos cenários descritos abaixo.

O desenho da superfície é atualizado a partir de LayoutUpdated e usa lastShape para evitar reconstruções de geometria quando as medidas relevantes não mudam. Portanto, avaliar o custo desse evento é uma investigação de desempenho, não uma afirmação de que há um problema comprovado.

O conteúdo baseado em modelos não tem um cache dedicado de árvores visuais por aba. Dados editáveis devem permanecer no modelo da aplicação.

## 3. Melhoria A — Cabeçalhos e títulos dinâmicos

### Objetivo

Garantir que alterações do cabeçalho em tempo de execução preservem seleção, geometria, foco e posição de arraste. O caso MAUI de tradução sem recriação dos itens serve como referência de comportamento.

### Cenários

- Alterar o título da aba selecionada e de uma aba anterior a ela.
- Substituir texto curto por texto longo e depois reduzir novamente.
- Trocar idioma sem recriar documentos.
- Alterar fonte, tamanho, peso, ícone ou visibilidade de elementos do cabeçalho.
- Atualizar títulos enquanto a faixa tem rolagem, durante uma animação e durante arraste.
- Repetir nas quatro posições e com a primeira aba rente à borda.

### Proposta técnica

Criar modelos de teste com INotifyPropertyChanged e cabeçalhos definidos por ItemTemplate. Medir o contorno em relação ao cabeçalho real após o ciclo de layout, sem estimar largura pelo número de caracteres.

Verificar o comportamento atual antes de alterar o algoritmo. Caso haja falha, corrigir a invalidação das medidas ou a atualização do destino de arraste. Para mudanças durante arraste, definir um comportamento consistente: atualizar a sessão com novas medidas ou cancelá-la de forma limpa; não efetuar reordenação usando medidas antigas.

Não adicionar propriedade pública somente para resolver invalidação interna.

### Critérios de aceitação

1. SelectedItem mantém a identidade do documento após a alteração do título.
2. A aba ativa permanece ligada ao corpo sem costura ou borda interna.
3. O contorno acompanha o novo tamanho ao término do layout.
4. Nenhum item é duplicado, removido ou reordenado apenas por mudar o cabeçalho.
5. Não permanecem cursor, transformações ou prévia de um arraste interrompido.
6. A aba selecionada pode ser localizada na faixa após expansão do título, com comportamento de rolagem documentado.

### Testes e riscos

Automatizar identidade, geometria, ordem e limpeza do arraste. Usar tolerâncias geométricas compatíveis com o renderizador; evitar testes baseados exclusivamente em pixels de uma captura.

Validar manualmente texto longo, fontes e DPI real. O principal risco é criar ciclos extras de layout ou deslocamentos visuais ao tentar corrigir o alinhamento.

## 4. Melhoria B — Coleções dinâmicas e seleções independentes

### Objetivo

Tornar explícitos e testados os comportamentos em mudanças estruturais da fonte de dados, inclusive quando dois controles apresentam os mesmos documentos.

### Cenários

- Adicionar, remover, mover, limpar e substituir ItemsSource.
- Remover a selecionada, a última aba ou uma aba anterior à selecionada.
- Remover ou substituir a fonte durante arraste/animação.
- Substituir objetos por novas instâncias com a mesma chave persistente.
- Manter dois controles sobre a mesma ObservableCollection de modelos, cada um com sua própria seleção.
- Exercitar fontes somente leitura, filtradas, agrupadas ou ordenadas.

### Proposta técnica

Separar o contrato de seleção por identidade de objeto do contrato de persistência por chave. Uma nova instância com a mesma chave não deve ser considerada a mesma seleção automaticamente, salvo por restauração explícita ou política documentada.

No cenário compartilhado, usar modelos de dados, não as mesmas instâncias visuais de TabItem: elementos WPF não podem ser hospedados simultaneamente em dois pais visuais. Cada controle deve gerar seus próprios contêineres.

Não usar um único SelectedDocument compartilhado no view model quando o objetivo for seleção independente. Compartilhar a coleção significa que uma remoção ou reordenação aparece nos dois controles; não significa compartilhar a aba selecionada.

Registrar notificações, eventos e identidade dos objetos antes/depois. Conferir desligamento de observadores e limpeza de sessões ao substituir a fonte ou descarregar o controle.

### Critérios de aceitação

1. A seleção de um controle não altera a do outro, quando os bindings de seleção são independentes.
2. Alterações na coleção compartilhada aparecem em ambos, sem divergência ou duplicação.
3. Reordenação mantém a seleção de cada controle enquanto seu item ainda existe.
4. Não há acesso a contêiner removido nem prévia presa após substituição de fonte.
5. Fontes incompatíveis recusam operações sem mutação parcial causada pelo controle.
6. O comportamento quando a seleção deixa de existir fica documentado e testado para cada caminho: fechamento, remoção externa e troca de fonte.

### Testes e riscos

Criar fixtures com dois controles e uma coleção compartilhada. Testar eventos e seleção separadamente. Não pressupor que remoção externa dispara TabClosing/TabClosed: esses eventos pertencem ao fluxo de fechamento do componente.

Mudanças de seleção herdadas de WPF podem diferir das políticas específicas de RequestCloseTab; evitar igualar esses fluxos sem avaliar compatibilidade.

## 5. Melhoria C — Acessibilidade e navegação por teclado

### Objetivo

Permitir reconhecer, selecionar, reorganizar e fechar abas sem depender de mouse ou de interpretação visual dos ícones.

### Proposta técnica

Inspecionar os AutomationPeers herdados e as informações expostas antes de criar peers próprios. Verificar nome, papel, seleção, habilitação e foco. Usar nomes fornecidos pelo consumidor para cabeçalhos com ícones.

Avaliar se o botão de fechamento deve expor um nome contextual, como “Fechar Editor”, com opção de localização. Não introduzir um novo padrão de teclado que entre em conflito com os controles das páginas.

Revisar contraste, foco visível, ordem de tabulação e botões desabilitados. Avaliar temas padrão e cenários de alto contraste sem afirmar suporte antes da verificação.

### Critérios de aceitação

1. Um leitor de tela identifica o controle, as abas e qual está selecionada.
2. Abas compostas apenas por ícones têm nomes úteis.
3. Tab/Shift+Tab não prendem o foco na faixa.
4. Setas, Ctrl+Tab e atalhos de reordenação têm comportamento definido e consistente com a orientação.
5. Campos de edição não perdem seus atalhos para o TabControl.
6. Ao fechar uma aba, o foco resultante é previsível e continua dentro de um elemento válido.
7. Fechamento indisponível não aparece como ação executável para automação.

### Testes e riscos

Automatizar informações dos peers, seleção, comandos e regras de habilitação. Executar roteiro manual com teclado e pelo menos um leitor de tela no Windows. Registrar versão do Windows, leitor e tema.

Um nome acessível presente não comprova uma experiência completa de acessibilidade. Alterações nos peers podem modificar o comportamento observado por ferramentas externas e exigem regressão.

## 6. Melhoria D — Custo de atualização do contorno

### Objetivo

Medir o custo de LayoutUpdated e verificar se há ganho relevante em reduzir trabalho sem perder a sincronização entre aba e corpo.

### Plano de medição

Comparar cenários com 2, 10 e 50 abas: janela ociosa, redimensionamento, rolagem, digitação na página, mudança de título e arraste. Medir quantidade de chamadas, reconstruções de geometria, tempo no trecho de atualização e alocações.

Separar chamadas ao método de reconstruções efetivas: lastShape já elimina parte do trabalho. Coletar dados em Release, registrar máquina e escala e repetir cada cenário para reduzir ruído. Não usar duração total da suíte como benchmark do controle.

### Alternativas a avaliar

- Invalidação por mudanças relevantes de tamanho, seleção e rolagem.
- Agrupamento de atualizações para o próximo ciclo de renderização.
- Redução de transformações e consultas repetidas quando nada mudou.
- Manutenção de LayoutUpdated como mecanismo de correção, caso necessário.

Nenhuma alternativa será escolhida antes da medição.

### Critérios de aceitação

1. Existe uma linha de base reproduzível e uma comparação antes/depois.
2. A otimização reduz o custo medido no cenário-alvo sem regressão relevante nos demais.
3. Contorno, hover, rolagem, quatro posições e arraste continuam corretos.
4. Não há dependência de espera fixa arbitrária para corrigir alinhamento.
5. Eventos associados a objetos de vida longa são desligados no ciclo de vida apropriado.

### Riscos

Trocar um mecanismo abrangente por eventos específicos pode deixar lacunas em atualização de templates, DPI, fontes e medidas. Se o custo atual não for relevante, a conclusão válida é manter a implementação existente e registrar os resultados.

## 7. Melhoria E — Preservação opcional do conteúdo visual

### Objetivo

Investigar a necessidade de manter árvores visuais de páginas para preservar estados como posição de rolagem e expansão de elementos ao trocar abas.

### Distinções necessárias

- Dados de negócio e textos editáveis pertencem ao modelo.
- Ordem e seleção pertencem ao estado de organização.
- Posição de rolagem e estado de controles podem pertencer à apresentação.
- Guardar uma árvore visual não equivale a salvar dados entre execuções.

### Proposta técnica em duas etapas

Primeiro, demonstrar perda de estado em um exemplo com ContentTemplate e verificar alternativas leves: vincular estado ao modelo, restaurar rolagem ou manter a página na aplicação.

Somente se necessário, prototipar uma política opt-in de cache. Nome, tipo e padrão da eventual propriedade pública ainda não estão definidos. O comportamento atual deve permanecer como padrão para preservar compatibilidade e memória.

Definir identidade dos itens, capacidade/evicção, tratamento de fechamento, troca de ItemsSource e descarregamento. Itens visuais explícitos precisam de tratamento distinto dos conteúdos gerados a partir de modelos. Nunca reutilizar um elemento visual simultaneamente entre dois controles.

Também definir o significado de Loaded/Unloaded e a atividade de páginas ocultas: timers, subscriptions e trabalho em segundo plano não podem continuar inadvertidamente apenas porque a página foi retida.

### Critérios de aceitação

1. O cenário de perda de estado é reproduzido antes da implementação.
2. Com a opção desativada, o comportamento atual permanece.
3. Com a opção ativada, o estado visual escolhido é preservado ao alternar abas.
4. Remover documentos e trocar a fonte libera referências segundo uma política documentada.
5. Dois controles não compartilham a mesma árvore visual por acidente.
6. Há medição de memória e testes de liberação; não basta contar itens do cache.
7. Comportamentos de templates, DataContext, foco, temas e recursos são cobertos.

### Riscos e decisão de versão

É a melhoria de maior impacto arquitetural. Pode reter documentos, imagens, serviços e eventos e alterar o ciclo de vida das páginas. Não é pré-requisito automático para 1.0.0; pode ser adiada para uma versão posterior.

## 8. Evolução do visualizador

### V1 — Idiomas e cabeçalhos variáveis

Adicionar seleção de idioma de demonstração, botão para alternar títulos curtos/longos e ajustes de fonte. Atualizar propriedades dos modelos existentes. Mostrar a identidade da aba selecionada para tornar verificável que a seleção não foi recriada.

Aceitação: trocar idioma em quatro posições, com rolagem e durante interação, sem perda dos dados de exemplo. Strings da demonstração não impõem um mecanismo de localização à biblioteca.

### V2 — Páginas com estado visual

Adicionar formulário, lista longa e conteúdo expansível. Permitir observar posição de rolagem e valores antes/depois de trocar abas. Distinguir visualmente estado guardado no modelo de estado exclusivamente visual.

Aceitação: o exemplo explica e reproduz o comportamento atual antes de servir de vitrine para qualquer cache futuro. Usar dados sintéticos e não depender de rede ou banco.

### V3 — Coleção compartilhada

Exibir dois TabControls sobre a mesma coleção de modelos, com seleções separadas. Permitir escolher orientações diferentes.

Aceitação: selecionar uma aba em um controle não muda a seleção do outro; mover ou remover um documento atualiza a coleção nos dois. A UI deve comunicar essa diferença ao usuário.

### V4 — Mutações durante interação

Adicionar ações controladas para inserir, remover, limpar e substituir a coleção. Um cenário temporizado, iniciado explicitamente pelo usuário, permite testar a mutação enquanto arrasta.

Aceitação: mostrar o resultado da operação e permitir reconstruir os dados da demonstração. Não executar mutações inesperadas ao abrir a janela. Garantir encerramento dos timers ao fechar o exemplo.

## 9. Ordem sugerida e dependências

| Etapa | Entrega | Impacto esperado |
|---|---|---|
| 1 | Testes A e B + exemplos V1 e V3 | Robustez sem nova API pública |
| 2 | Exemplo V4 e correções de falhas reproduzidas | Tratamento de mudanças durante interação |
| 3 | Revisão C e roteiro manual | Qualidade de uso por teclado/leitor de tela |
| 4 | Medição D e decisão fundamentada | Desempenho sem otimização prematura |
| 5 | Exemplo V2 e investigação E | Base para decidir sobre cache opcional |

A e B devem orientar qualquer mudança na atualização da geometria. V2 deve preceder a definição de uma API de cache. Não há estimativa de prazo: ela depende das falhas encontradas e dos resultados das medições.

## 10. Estratégia de entrega e validação

Para cada etapa:

1. Documentar o comportamento esperado e o cenário reproduzível.
2. Adicionar os testes necessários sem duplicar verificações triviais da implementação.
3. Implementar a correção mínima e preservar APIs publicadas.
4. Executar as suítes existentes e os cenários novos relacionados ao risco.
5. Registrar limitações de testes simulados e resultado dos testes manuais.
6. Atualizar documentação e notas de versão.
7. Publicar uma prévia apenas após aprovação do lançamento.

Manter evidências separadas: testes automatizados, observações visuais, avaliação de acessibilidade e medições de desempenho/memória. Um resultado positivo em uma categoria não substitui as demais.

## 11. Fora do escopo

- Conexão com banco, licenciamento e serviço da aplicação aplicaÃ§Ã£o de demonstraÃ§Ã£o.
- Alterações no implementaÃ§Ã£o MAUI de referÃªncia ou criação imediata de um pacote MAUI.
- Transferência de abas entre janelas, docking e fechamento assíncrono.
- Expansão de frameworks suportados sem projeto de compatibilidade próprio.
- Garantia de que todas as melhorias farão parte da versão 1.0.0.

## 12. Decisões a registrar ao iniciar a implementação

- Comportamento durante mudança de título em arraste: recalcular ou cancelar, conforme viabilidade e consistência.
- Política de seleção em remoções externas e substituição da fonte, respeitando o comportamento herdado do WPF.
- Leitor de tela e ambientes reais disponíveis para validação.
- Métricas e cenário-alvo de desempenho após coleta da linha de base.
- Necessidade real, limite e ciclo de vida do cache visual antes de expor nova API.

Recomendação inicial: iniciar pela etapa 1, sem introduzir novas propriedades públicas até que os testes mostrem uma necessidade concreta.

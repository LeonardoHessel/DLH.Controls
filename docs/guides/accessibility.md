# Validação manual de acessibilidade

Os testes automatizados confirmam o papel de TabControl/TabItem, o estado selecionado, nomes fornecidos pelo consumidor, nome contextual da ação de fechar e sua indisponibilidade quando bloqueada. Eles não substituem o uso real das tecnologias assistivas.

Antes de declarar suporte de acessibilidade, registrar data, versão do Windows, escala, tema e leitor de tela e executar:

1. Percorrer a janela com Tab e Shift+Tab e confirmar que o foco entra e sai da faixa.
2. Selecionar abas com as setas e Ctrl+Tab nas quatro orientações.
3. Reordenar com Ctrl+Shift+setas e conferir anúncio, foco e ordem.
4. Conferir nomes de abas de texto, somente ícone e títulos alterados em execução.
5. Fechar uma aba selecionada e outra não selecionada; conferir o próximo foco.
6. Desabilitar fechamento e confirmar que a ação não é anunciada como disponível.
7. Repetir nos três temas, em alto contraste e com 100%, 150% e 200% de escala.

## Execução de 5 de setembro de 2026

Validação realizada no visualizador compilado em Release, em uma janela real do Windows:

- Árvore de automação reconheceu os três controles e seus itens como guias selecionáveis.
- Abas laterais compostas por imagens expuseram os nomes Licença, Banco de dados, Procedimentos e Informações.
- `Ctrl+Tab` mudou a página mantendo o foco no campo de edição.
- Os controles superior e inferior mantiveram seleções independentes.
- Títulos PT/EN e curtos/longos atualizaram a árvore de automação e o contorno; o overflow exibiu rolagem.
- A alteração de fonte preservou o contorno contínuo.
- A mutação agendada inseriu o mesmo documento nos dois controles e atualizou o status.
- Ao desabilitar fechamento, os botões desapareceram da árvore de automação e o comando da barra ficou indisponível.
- Tema escuro e tema claro foram inspecionados visualmente.
- O tema cinza e laranja foi inspecionado visualmente; contorno, sombra, foco e contraste funcional permaneceram legíveis.
- Tab e Shift+Tab permitiram sair e retornar ao conteúdo, passando pelos elementos seguintes sem aprisionar o foco na faixa.
- O arraste físico horizontal moveu “Visão geral” da posição 1 para 2 e refletiu a mesma ordem no segundo controle ligado à coleção.
- O arraste físico vertical moveu “Licença” da posição 1 para 3; a prévia permaneceu no eixo vertical e o contorno foi recomposto após a animação.

Foi encontrada e corrigida uma falha: o botão anunciava o tipo do modelo (`DLH.Controls.Wpf.Demo.TabDocument`). O template agora prioriza `AutomationProperties.Name` da aba e usa o cabeçalho como fallback, produzindo nomes como “Fechar Visão geral” e “Fechar Banco de dados”.

Permanecem pendentes: leitor de tela real, modo de alto contraste, escalas reais de 100%, 150% e 200%, verificação auditiva dos anúncios de reordenação e cancelamento físico de um gesto em andamento com `Esc`. O cancelamento, as quatro orientações e as escalas simuladas continuam cobertos pela suíte automatizada, mas não são apresentados como equivalentes ao ambiente real.

O pacote não afirma conformidade integral com um padrão de acessibilidade apenas com base nos testes automatizados e nesta inspeção parcial.

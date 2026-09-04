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

Resultado atual: **pendente de execução manual**. O pacote não afirma conformidade integral com um padrão de acessibilidade apenas com base nos testes automatizados.

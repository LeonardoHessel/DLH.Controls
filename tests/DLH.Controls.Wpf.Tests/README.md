# Testes de drag and drop

A suíte independente está em `DragDropTests.cs`; `Program.cs` mantém as verificações visuais de geometria, ícones, temas e sombra. Cada cenário de arraste usa um controle e uma coleção novos. Uma falha é registrada sem impedir a execução dos demais cenários.

## Executar

Na raiz da solução:

```powershell
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --drag-only
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --drag-only --report resultado.json
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release
```

O primeiro comando executa somente os 86 cenários de drag and drop. O segundo salva um relatório JSON com resultado e duração por cenário. O terceiro executa também a suíte visual. O processo retorna código 1 se qualquer cenário falhar.

## Cobertura automatizada

São nove cenários por posição (Top, Bottom, Left, Right), totalizando 36:

- Soltar nos dois sentidos, com atualização MVVM, seleção e uma notificação Move por operação.
- Limiar estritamente maior que 5 pixels físicos, nos dois sentidos do eixo; movimento perpendicular não inicia o arraste.
- Destino estável enquanto os vizinhos animam, movimento perpendicular ignorado e cancelamento sem alterar a origem.
- Soltar fora dos limites do eixo sem reorganizar.
- Rolagem automática nos dois sentidos.
- Remover a aba arrastada sem afetar outros itens.
- Cancelar e reiniciar rapidamente, antes de terminar a animação anterior.

Sete cenários adicionais verificam fontes explícitas, mutáveis e incompatíveis; índices e abas desabilitadas; cursor e timer; Esc; perda de captura e descarregamento; esvaziamento da coleção; preservação de transformações e opacidade existentes.

A conclusão usa o mesmo método chamado pelo evento de soltar o botão. A reflexão necessária para preparar o estado e fornecer coordenadas simuladas fica concentrada nos auxiliares da suíte, sem expor uma API de testes na biblioteca.

## Limites e revisão interativa

Os testes executam layout, templates, bindings, eventos roteados e animações reais do WPF em STA. O estado inicial de arraste e as coordenadas do ponteiro são simulados. Não é uma validação ponta a ponta de mouse físico, captura nativa, Alt+Tab real ou monitores com DPIs diferentes.

Antes de uma versão de distribuição, conferir na demonstração:

1. Clique sem arrastar e movimentos até 5 px: cursor normal, sem prévia.
2. Arraste nos dois sentidos em cada posição, inclusive com títulos de tamanhos diferentes.
3. Afaste o mouse perpendicularmente: a posição no eixo continua sendo considerada.
4. Mantenha o mouse na extremidade: rolagem contínua; solte após rolar.
5. Pressione Esc, use Alt+Tab e solte fora da janela: sem cursor, indicador ou prévia presos.
6. Repita arrastes rápidos e teste escalas de 100%, 150% e 200% em monitores reais.


## Regressões de redimensionamento e posição

Há dois cenários adicionais para cada posição: redimensionar três vezes durante o arraste sem perder o alinhamento da prévia; e trocar a posição da faixa durante a animação. Trocar a posição cancela o arraste, preserva a coleção e permite iniciar uma nova operação imediatamente.

Esses testes revelaram e corrigiram duas falhas: a coordenada transversal da prévia ficava presa ao layout antigo, e a alteração de TabStripPlacement não encerrava a sessão de arraste. A prévia agora usa o viewport atual, e a mudança de posição cancela a sessão e invalida o layout imediatamente.

## Recursos adicionais

A suíte também cobre evento de reordenação (origem e índices), ausência de evento em no-op, propriedades da prévia/animação, configuração inválida, botão e comando de fechar, cancelamento, proteção de abas, seleção após fechamento, roundtrip JSON, chaves inválidas/ausentes/novas e reordenação por teclado nos quatro lados. Total atual: 86 cenários.

O modo `--drag-only` foi preservado por compatibilidade e agora inclui esses testes comportamentais relacionados às abas.

## Ampliação de cobertura
25 cenários adicionais: 50 abas com reordenações repetidas e arraste/rolagem nas quatro posições; LayoutTransform de 125%, 150% e 200%; alteração de permissões/animação/prévia durante arraste; limites de reordenação por teclado e metadados de foco/fechamento; JSON inválido e referências removidas.

`--settings-only` também verifica os nomes acessíveis dos ícones laterais e a resposta da demonstração a arquivo ausente ou corrompido usando um caminho temporário isolado. O método de leitura aceita um caminho opcional para esse teste; o caminho normal de persistência continua igual.

Escala de layout não equivale a DPI real do monitor. Permanecem manuais: troca entre monitores, Alt+Tab físico, captura real do mouse, navegação completa com Tab/Shift+Tab e avaliação por leitor de tela. Não foram alteradas as configurações de escala do Windows.

### Persistência das configurações
`--configuration-only` executa 19 verificações: serialização independente de cultura, validação antes de alterar propriedades, bindings/documentos preservados, padrões da biblioteca, arquivo por modelo, restauração em outra janela, troca de tema após restaurar, botão de padrões com escopo individual/coletivo, JSON corrompido e arquivo ausente. Usa arquivos temporários isolados; não altera as preferências salvas pelo usuário. A abertura de outra janela é testada pela chamada ao mesmo método de leitura usado no carregamento, sem simular um reinício completo do processo.

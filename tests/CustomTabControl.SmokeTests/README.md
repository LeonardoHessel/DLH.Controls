# Testes de drag and drop

A suíte independente está em `DragDropTests.cs`; `Program.cs` mantém as verificações visuais de geometria, ícones, temas e sombra. Cada cenário de arraste usa um controle e uma coleção novos. Uma falha é registrada sem impedir a execução dos demais cenários.

## Executar

Na raiz da solução:

```powershell
dotnet run --project tests/CustomTabControl.SmokeTests -c Release -- --drag-only
dotnet run --project tests/CustomTabControl.SmokeTests -c Release -- --drag-only --report resultado.json
dotnet run --project tests/CustomTabControl.SmokeTests -c Release
```

O primeiro comando executa somente os 43 cenários de drag and drop. O segundo salva um relatório JSON com resultado e duração por cenário. O terceiro executa também a suíte visual. O processo retorna código 1 se qualquer cenário falhar.

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

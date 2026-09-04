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
| ItemKeyPath | Id | Caminho de uma chave string; aceita membros separados por ponto |

Propriedades anexadas: `CanCloseTab` (true) protege um contêiner individual; `TabKey` (null) fornece a chave de itens explícitos. As chaves devem ser não vazias, únicas e estáveis.

A propriedade herdada `TabStripPlacement` aceita Top, Bottom, Left e Right. `BorderThickness` usa o maior lado como traço uniforme. `Padding`, cores e fontes são propriedades herdadas. Valores padrão acima são os da biblioteca, não os ajustes do visualizador.

## Operações e eventos

| Operação | Resultado e limites |
|---|---|
| MoveTab(oldIndex, newIndex) | bool; índices base zero; exige fonte mutável sem ordenação/filtro/grupo e CanReorderTabs; selecionado é preservado |
| RequestCloseTab(item) | bool; respeita permissões, CanExecute e cancelamento; true somente se removido |
| CaptureState(keySelector?) | TabControlState; captura ordem e seleção por chaves |
| RestoreState(state, keySelector?) | Valida estrutura/chaves, ignora ausentes e anexa novos itens; exige fonte mutável |
| SaveState(stream, keySelector?) / LoadState(stream, keySelector?) | JSON; não fecham o stream do chamador |
| CaptureConfiguration() | TabControlConfiguration com valores textuais em cultura invariável |
| ValidateConfiguration(configuration) | Estático; valida sem alterar controle |
| RestoreConfiguration(configuration) | Valida todos os valores antes de aplicar; preserva bindings com SetCurrentValue |
| ResetConfiguration() | Aplica padrões registrados; não remove documentos; não significa restaurar o tema ou ajustes iniciais da aplicação |

`TabReordered` é emitido após mudança efetiva, com Item, OldIndex, NewIndex e Reason (Programmatic, Drag, Keyboard). Não ocorre em cancelamento, no-op ou RestoreState. `TabClosing` permite veto por Cancel; `TabClosed` ocorre após remoção confirmada. `StateRestored` sinaliza a conclusão da restauração da ordem/seleção. São eventos CLR, não eventos roteados. `CloseTab` é um RoutedUICommand.

Quando CloseTabCommand está definido, ele é responsável pela remoção síncrona. A biblioteca não aguarda Tasks nem oferece confirmação assíncrona. Alterar diretamente a coleção é responsabilidade da aplicação e não passa pelas permissões/eventos do controle.

## Persistência e erros

Os dois formatos têm Version=1, independentemente da versão NuGet. Estado não salva conteúdo das páginas. Configuração não salva comandos, itens, templates, seleção ou o tema da aplicação. Propriedades ausentes mantêm o valor atual; nomes desconhecidos e versões incompatíveis são rejeitados. Cores são valores, não chaves DynamicResource. Cursores/brushes personalizados precisam ser convertíveis pelos conversores WPF; CaptureConfiguration pode rejeitá-los.

Chaves inválidas ou fontes incompatíveis geram InvalidOperationException; valores/versões inválidos geram ArgumentException; JSON inválido gera JsonException e conteúdo JSON nulo em LoadState gera InvalidDataException. Falhas de streams pertencem ao chamador. A validação prévia não fornece rollback de efeitos de comandos, eventos ou coleções personalizados que lancem exceções.

## Tipos auxiliares e compatibilidade

CustomTabItem é opcional: TabItem nativo também é aceito. TabControlState, TabControlConfiguration e os argumentos de eventos são públicos. TabCursors.ClosedHand e TabSpacingConverter também foram publicados: não serão removidos ou tornados internos nesta revisão. O primeiro é um cursor compartilhado, não deve ser descartado pelo consumidor; o segundo é infraestrutura do template, sem necessidade de uso direto na maioria das aplicações.

Mantidos os nomes CustomTabControl, CanCloseTabs (global) e CanCloseTab (individual). Renomeá-los agora quebraria consumidores sem benefício suficiente. Nenhuma assinatura pública foi alterada nesta revisão.

# Padronização de controles compartilhados

## ScrollBar

`ScrollBar` deriva da barra nativa e mantém seus comandos, automação, teclado e integração com `ScrollViewer`. O contrato compartilhado inclui orientação, espessura, pincéis do trilho e polegar, estados de hover e pressionado, opacidade, preenchimento, raio, sombra e botões direcionais opcionais.

O raio informado é uniforme e seu valor renderizado nunca supera metade do eixo transversal. O padrão visual reproduz a barra já aprovada no `DataGridView`; o grid conserva suas propriedades atuais por compatibilidade e as encaminha às instâncias compartilhadas.

## ContextMenu

O menu compartilhado mantém `ContextMenu`, `MenuItem`, comandos e MVVM nativos, padronizando superfície, borda, raio, sombra, espaçamento, ícones, itens marcáveis, submenus, separadores e estados de foco/seleção. O menu de cabeçalho do `DataGridView` usa o componente sem alterar suas ações.

## Validação

Cada controle terá testes próprios de padrões, validação, templates, orientação e consumo externo. A demonstração e os snapshots visuais deverão comprovar que a extração não altera os componentes existentes.


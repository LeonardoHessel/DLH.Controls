# Plano — criação e renomeação de abas

## 1. Objetivo e limites

Adicionar duas funcionalidades independentes ao `CustomTabControl`:

1. uma ação visual opcional, representada inicialmente por `+`, para solicitar a criação de uma aba;
2. edição opcional do título de uma aba diretamente no cabeçalho.

As duas funcionalidades permanecem desativadas por padrão. O controle coordena a interação e expõe comandos e eventos; a aplicação continua responsável por criar o item, alterar seu modelo e persistir os dados.

A ação de adicionar não será um item da coleção. Portanto, não poderá ser selecionada, fechada, renomeada, reordenada ou incluída em `SelectedIndex`, `Items`, captura de estado e cálculos de destino do drag and drop.

## 2. Etapa A — ação opcional para adicionar abas

**Resultado:** concluída. A ação é um botão independente dos itens, funciona nas quatro posições e permanece oculta por padrão. A integração aceita `AddTabCommand` com parâmetro ou, na ausência de comando, `AddTabRequested`. O estado habilitado acompanha `CanExecute`; conteúdo e template são substituíveis. A cobertura automatizada confirma posicionamento, execução única, precedência do comando, isolamento da coleção e metadados de acessibilidade.

### API proposta

| Membro | Tipo | Padrão | Finalidade |
|---|---|---:|---|
| `CanAddTabs` | `bool` | `false` | Exibe e habilita a ação de adicionar. |
| `AddTabCommand` | `ICommand?` | `null` | Comando da aplicação que cria e adiciona o novo item. |
| `AddTabCommandParameter` | `object?` | `null` | Parâmetro opcional enviado ao comando. |
| `AddTabContent` | `object?` | `"+"` | Conteúdo visual da ação. |
| `AddTabContentTemplate` | `DataTemplate?` | `null` | Permite substituir o `+` por ícone ou conteúdo próprio. |

`CanAddTabs` controla a disponibilidade geral. Se `AddTabCommand.CanExecute` retornar `false`, o botão permanece visível e desabilitado. Sem comando, a ação pode ser atendida pelo evento `AddTabRequested`; se não houver comando nem assinante, ela fica desabilitada.

### Evento proposto

`AddTabRequested` permite integração sem framework MVVM. O evento não adiciona automaticamente um objeto desconhecido à coleção. A aplicação cria o modelo, inclui-o em sua `ObservableCollection` e escolhe se ele será selecionado.

### Apresentação e posições

Adicionar `PART_AddTabButton` ao template, ao lado do painel de cabeçalhos e dentro da região rolável adequada. A ação aparece depois da última aba nas posições superior e inferior, e abaixo da última aba nas posições esquerda e direita.

O botão usa recursos de tema e respeita `CornerRadius`, `TabStripPlacement`, fonte, foco por teclado, estados de hover/pressionado/desabilitado e escalas de DPI. Sua forma não se une ao corpo porque nunca representa a aba selecionada.

### Interação

- Clique, toque equivalente do WPF, `Enter` e `Space` solicitam uma nova aba.
- A ação participa da navegação por `Tab`, mas não da seleção por setas ou `Ctrl+Tab` entre documentos.
- O drag and drop ignora completamente a ação.
- Quando as abas transbordarem, a ação deve continuar alcançável pela faixa sem encobrir os botões de rolagem.

### Critérios de aceitação

1. O controle permanece visual e funcionalmente idêntico quando `CanAddTabs` é `false`.
2. Uma solicitação executa exatamente uma vez.
3. `CanExecute=false` impede mouse e teclado.
4. A ação não altera índices, seleção, ordem capturada nem contorno ativo.
5. Funciona nas quatro posições e com overflow.
6. O nome acessível padrão é `Adicionar aba` e pode acompanhar conteúdo acessível personalizado.

## 3. Etapa B — edição opcional do título

**Resultado:** concluída. A edição permanece desativada por padrão e pode ser iniciada por `F2`, duplo clique ou chamada programática. `Enter` confirma, `Esc` cancela e a perda de foco tenta confirmar. A integração aceita propriedade indicada por `TabHeaderPath`, `TabItem.Header` textual ou `RenameTabCommand`; eventos permitem veto e notificação. Mudanças estruturais cancelam o editor e o drag and drop ignora sua área interativa.

### API proposta

| Membro | Tipo | Padrão | Finalidade |
|---|---|---:|---|
| `CanRenameTabs` | `bool` | `false` | Autoriza edição direta dos títulos. |
| `TabHeaderPath` | `string?` | `null` | Caminho da propriedade textual do item, como `Header` ou `Title`. |
| `RenameTabCommand` | `ICommand?` | `null` | Recebe a solicitação validada para atualizar o modelo. |
| `RenameActivation` | `TabRenameActivation` | `F2AndDoubleClick` | Define os gestos que iniciam a edição. |

O nome `TabHeaderPath` evita conflito com o `HeaderTemplate` nativo e explicita que o valor pertence ao item de dados. A enumeração de ativação deve permitir pelo menos `F2`, `DoubleClick` e `F2AndDoubleClick`.

### Eventos e dados

- `TabRenaming`: disparado antes de entrar no modo de edição e cancelável.
- `TabRenameRequested`: contém item, título original e novo título; é cancelável.
- `TabRenamed`: informa que a aplicação aceitou a alteração.

O comando recebe um `TabRenameRequest` com `Item`, `OldHeader` e `NewHeader`. O componente não deve presumir que todos os modelos são mutáveis.

Ordem de integração:

1. se houver `RenameTabCommand`, consultar `CanExecute` e executar o comando;
2. sem comando, tentar atualizar por binding de duas vias associado a `TabHeaderPath`;
3. para `TabItem` declarado diretamente, atualizar `Header` quando ele for textual;
4. se nenhum mecanismo puder gravar, não iniciar a edição.

### Editor visual

O cabeçalho alterna entre sua apresentação normal e um `TextBox` no próprio `CustomTabItem`. O editor herda fonte, cores e alinhamento do cabeçalho, seleciona o texto ao abrir e mantém a largura inicial mínima para reduzir saltos de layout.

- `F2` ou duplo clique inicia a edição da aba apontada/selecionada.
- `Enter` solicita a confirmação.
- `Esc` restaura o texto original.
- Perda de foco confirma, exceto quando causada pelo cancelamento ou descarregamento.
- Texto sem alteração encerra a edição sem comando ou evento final.
- Título vazio é permitido pelo componente; regras de negócio pertencem ao comando/evento da aplicação.
- Apenas uma aba pode estar em edição por controle.

Durante a edição, o início do drag and drop sobre o editor é bloqueado. Se um arraste, troca de `ItemsSource`, remoção, `Reset`, mudança de posição ou descarregamento ocorrer, a edição é cancelada de maneira limpa. A confirmação que alterar a largura invalida o layout e atualiza o contorno normalmente.

### Cabeçalhos compostos

O `ItemTemplate` atual pode conter ícone, texto e outros elementos. Ao editar, somente o valor resolvido por `TabHeaderPath` é apresentado no `TextBox`; o ícone permanece visível. Isso exige separar, no template padrão do item, a área do conteúdo visual da área sobreposta pelo editor.

Para templates totalmente personalizados, a biblioteca deve fornecer uma parte nomeada ou um estilo de editor substituível. A primeira versão pode expor `TabHeaderEditTemplate`, desde que a API permaneça pequena e haja um caso real no visualizador.

### Critérios de aceitação

1. Nada muda quando `CanRenameTabs` é `false`.
2. Mouse e teclado iniciam a mesma sessão de edição.
3. `Enter`, `Esc` e perda de foco têm resultados determinísticos.
4. O título só muda quando o destino de dados aceita a solicitação.
5. Ícone, seleção, conteúdo, identidade e ordem são preservados.
6. Renomeação funciona nas quatro posições, com overflow e títulos longos.
7. Remoção ou troca da coleção não deixa editor, foco ou captura presos.
8. Tecnologias assistivas recebem `Editar nome da aba <título>` e anunciam o campo editável.

## 4. Etapa C — configuração, estado e compatibilidade

Incluir as opções visuais e comportamentais simples em `CaptureConfiguration`, `RestoreConfiguration` e `ResetConfiguration`. Comandos, parâmetros, templates e assinantes de eventos não são serializados.

`CanAddTabs=false` e `CanRenameTabs=false` devem integrar os padrões restauráveis. `TabHeaderPath` pode ser persistido por ser texto declarativo; revisar riscos de renomeação de propriedades antes de incluí-lo no formato atual.

`CaptureState` continua contendo apenas documentos reais. A edição em andamento nunca é persistida. Uma restauração cancela qualquer interação transitória antes de aplicar ordem e seleção.

## 5. Etapa D — visualizador e documentação

Atualizar o visualizador com controles separados para ativar criação e renomeação. O exemplo principal deverá:

- adicionar uma aba com identificador estável e título inicial `Nova aba`;
- selecionar a aba recém-criada;
- permitir renomeá-la por duplo clique e `F2`;
- mostrar um exemplo de `AddTabContentTemplate` com ícone vetorial;
- manter exemplos com ambas as funcionalidades desativadas para provar compatibilidade.

Documentar em `README.md`, `docs/TabControl.md`, `docs/Integration.md` e `docs/ApiReference.md` os exemplos com `ItemsSource`, títulos e ícones, comandos, eventos e comportamento de teclado.

## 6. Etapa E — testes

### Automatizados

- padrões desativados e round-trip de configuração;
- execução única de comando/evento de criação e respeito a `CanExecute`;
- ausência da ação `+` em itens, índices, seleção, estado e reordenação;
- criação nas quatro posições, com coleção vazia e overflow;
- ativação da edição, confirmação, cancelamento e perda de foco;
- `INotifyPropertyChanged`, binding de duas vias, `TabItem.Header` e comando externo;
- veto, texto igual, título vazio e item removido durante edição;
- bloqueio de drag no editor e limpeza diante de mudanças estruturais;
- nomes de automação e navegação por teclado;
- regressão completa dos 108 cenários atuais.

### Manuais

- aparência e foco nos três temas;
- mouse físico, `F2`, `Enter`, `Esc`, `Tab` e `Shift+Tab`;
- posições superior, inferior, esquerda e direita;
- DPI 100%, 125%, 150% e 200%;
- leitor de tela e alto contraste;
- títulos curtos, longos, acentuados e em idiomas diferentes.

## 7. Sequência de implementação e commits

1. **Ação de adicionar:** API, template, quatro posições, acessibilidade e testes. Commit sugerido: `feat: add optional new tab action`.
2. **Renomeação:** modelo de interação, editor, comandos/eventos, cancelamentos e testes. Commit sugerido: `feat: add optional inline tab renaming`.
3. **Configuração e visualizador:** opções persistíveis, exemplos e painel de configuração. Commit sugerido: `feat: expose tab creation and renaming settings`.
4. **Documentação e validação final:** guias, referência de API, checklist manual e pacote local. Commit sugerido: `docs: document tab creation and inline renaming`.

Cada etapa deve encerrar com build da solução e apenas os testes relevantes; a última executa `eng/Validate.ps1` e confirma que o pacote contém os recursos e a documentação esperados. A versão NuGet só deve ser alterada quando houver decisão de publicar uma nova prévia.

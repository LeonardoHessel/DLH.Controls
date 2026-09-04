# CustomTabControl · WPF / .NET 10

Controle reutilizável de abas com contorno unificado, cantos uniformes, sombra configurável, temas e reordenação animada. A solução separa a biblioteca, a demonstração e os testes.

## Executar

Abra `DLH.Controls.sln` e defina `DLH.Controls.Wpf.Demo` como projeto de inicialização, ou execute:

```powershell
dotnet run --project samples/DLH.Controls.Wpf.Demo
```

A demonstração possui exemplos horizontais e laterais (esquerda/direita), ícones fornecidos no ZIP, três temas e botões para salvar/restaurar a organização. Os dados são ilustrativos; nenhuma conexão externa é realizada.

## Uso básico

Adicione uma referência à biblioteca e declare:

```xml
xmlns:controls="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf"
```

```xml
<controls:CustomTabControl
    TabStripPlacement="Top"
    HeaderIndent="0"
    CornerRadius="12"
    TabSpacing="0">
    <controls:CustomTabItem Header="Primeira" controls:CustomTabControl.TabKey="first">
        <TextBlock Text="Conteúdo livre" />
    </controls:CustomTabItem>
    <controls:CustomTabItem Header="Segunda" controls:CustomTabControl.TabKey="second" />
</controls:CustomTabControl>
```

O estilo é carregado automaticamente de `Themes/Generic.xaml`. Itens `TabItem` nativos são aceitos; com `ItemsSource`, o controle gera `CustomTabItem`. Use `ItemTemplate` para os cabeçalhos, `ContentTemplate` para as páginas e `SelectedItem` com binding de duas vias.

Dados editáveis devem permanecer no modelo. O controle não mantém um cache de árvores visuais para cada item de dados.

## Contorno e aparência

`CornerRadius="12"` define um único raio para o corpo, a aba ativa e as ligações côncavas. Valores não uniformes, negativos ou não finitos são rejeitados. Se não houver espaço, o contorno reduz suas curvas para um mesmo raio efetivo. `0` remove as curvas. Quando a aba fica rente à borda, a lateral segue reta.

`HeaderIndent` define o recuo inicial; `0` alinha a primeira aba à borda e o padrão `NaN` calcula o espaço pelo raio. `TabSpacing` é zero por padrão e acompanha o eixo da faixa.

O fundo, a borda e a sombra são desenhados em um único contorno. `BorderThickness` usa seu maior componente como espessura do traço; prefira valores uniformes. `Padding`, fontes, `Background` e `BorderBrush` seguem as propriedades WPF.

Os recursos `Tabs.Surface`, `Tabs.Hover`, `Tabs.Text`, `Tabs.Muted`, `Tabs.Edge`, `Tabs.Focus` e `Tabs.HeaderPadding` podem ser substituídos. O hover usa uma silhueta contínua atrás da superfície ativa. Para derivar um estilo de `Tabs.ItemStyle`, mescle explicitamente `/DLH.Controls.Wpf;component/Themes/Generic.xaml`.

## Sombra

```xml
<controls:CustomTabControl
    IsShadowEnabled="True"
    ShadowColor="#494949"
    ShadowOpacity="0.5"
    ShadowBlurRadius="10"
    ShadowDepth="0"
    ShadowDirection="315" />
```

Esses são os padrões. Opacidade varia de 0 a 1; desfoque e distância são não negativos. Desativar remove o efeito sem perder a configuração. Distância zero deixa a sombra centralizada. Spread permanece zero.

## Reordenação e animação

```xml
<controls:CustomTabControl
    CanReorderTabs="True"
    MinimumDragDistance="5"
    IsDragPreviewEnabled="True"
    IsAnimationEnabled="True"
    DragAnimationDuration="0:0:0.180"
    DragPreviewOpacity="0.94"
    TabDragCursor="ScrollWE" />
```

O limiar é medido em pixels físicos, somente no eixo da faixa, e precisa ser ultrapassado. O cursor aparece apenas durante o arraste. Para abas laterais, a demonstração usa `ScrollNS`.

A prévia acompanha o eixo das abas, os vizinhos abrem espaço e a coleção só muda ao soltar. O movimento perpendicular é ignorado. Nas extremidades, há rolagem automática. Esc, perda de captura, desativação da janela ou soltura além dos limites do eixo cancelam a operação. Mudar a posição da faixa durante o arraste também cancela; redimensionar atualiza o alinhamento da prévia.

`IsDragPreviewEnabled="False"` mantém o destino e a reordenação sem prévia flutuante. `IsAnimationEnabled="False"` ou duração zero faz os deslocamentos instantaneamente. Alterar as opções de prévia/animação/limiar durante um arraste cancela a sessão de forma limpa. A duração aceita de zero a dez segundos.

Use `MoveTab(indiceAtual, indiceFinal)` para reordenar por código. Retorna `false` quando a operação não é suportada. `ObservableCollection<T>` recebe uma única notificação `Move`. Listas mutáveis e itens explícitos são aceitos. Arrays, fontes somente leitura e visões filtradas, ordenadas ou agrupadas não são reorganizados. Não há transferência entre controles ou janelas.

### Evento de reordenação

```csharp
tabs.TabReordered += (_, e) =>
{
    // e.Item, e.OldIndex, e.NewIndex e e.Reason
    // Reason: Programmatic, Drag ou Keyboard.
};
```

O evento ocorre após uma mudança efetiva, uma vez por operação; cancelamentos e movimentos para o mesmo índice não o disparam. A restauração em lote emite somente `StateRestored` ao final.

## Teclado

Com o cabeçalho focado, use **Ctrl+Shift+Esquerda/Direita** para reorganizar abas horizontais ou **Ctrl+Shift+Cima/Baixo** nas laterais. O foco acompanha o item movido. As teclas não são interceptadas em editores ou controles interativos dentro da página/cabeçalho. O comportamento padrão de Tab, setas e Ctrl+Tab permanece disponível.

## Fechar abas

`ShowCloseButtons` é `false` por padrão. Ative para mostrar os botões de fechamento:

```xml
<controls:CustomTabControl ShowCloseButtons="True"
                          CloseTabCommand="{Binding CloseDocumentCommand}"
                          TabClosing="OnTabClosing" />
```

`CloseTabCommand` recebe o item da coleção como parâmetro. Seu `CanExecute` pode impedir o fechamento. Quando fornecido, o comando é responsável por remover o item de forma síncrona. Sem comando, o controle remove da lista mutável. O método `RequestCloseTab(item)` utiliza exatamente o mesmo fluxo e retorna se o item foi removido.

`TabClosing` ocorre antes de executar o comando ou remover o item; defina `e.Cancel = true` para impedir o fechamento. A demonstração usa esse evento para pedir confirmação quando há anotações. A biblioteca não exibe diálogos. `TabClosed` só ocorre depois de confirmar que o item saiu da coleção. Comandos assíncronos devem coordenar sua própria confirmação/remoção; não fazem parte desse contrato síncrono.

Para proteger uma aba, use `controls:CustomTabControl.CanCloseTab="False"` no contêiner ou configure essa propriedade no `ItemContainerStyle`. O botão é ocultado e `RequestCloseTab` também respeita a proteção. Fechar a selecionada escolhe a próxima aba habilitada, ou a anterior quando necessário; fechar outra preserva a seleção. Fechar a última limpa a seleção.

## Persistir ordem e seleção

O estado contém somente chaves, ordem, seleção e versão do formato, sem serializar páginas ou seus dados. Cada item precisa de uma chave de texto única e estável:

- `ItemKeyPath="Id"` (padrão) para modelos; aceita caminho como `Document.Id`.
- `controls:CustomTabControl.TabKey="first"` para itens explícitos.
- Um `Func<object, string>` opcional nos métodos para extrair a chave.

```csharp
using (var output = File.Create(path)) tabs.SaveState(output);
using (var input = File.OpenRead(path)) tabs.LoadState(input);

// Alternativa sem arquivo:
TabControlState state = tabs.CaptureState();
tabs.RestoreState(state);
```

O chamador é responsável por abrir/fechar os streams e escolher onde armazená-los. Também pode serializar `TabControlState` por conta própria. `RestoreState` ignora chaves de itens ausentes e coloca itens novos no final, preservando sua ordem relativa. Chaves duplicadas/vazias e versões desconhecidas são rejeitadas antes de reorganizar. A restauração requer fonte mutável sem filtro, agrupamento ou ordenação, mas funciona com arraste desabilitado.

A demonstração salva explicitamente em `%LOCALAPPDATA%\CustomTabControl.Demo\layout.json`, restaura pelo botão e tenta restaurar ao abrir. Apenas as abas existentes são reorganizadas: documentos fechados/ausentes não são recriados e anotações não são gravadas nesse arquivo.

## Validação

```powershell
dotnet build DLH.Controls.sln -c Release
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --drag-only --report resultado.json
```

A suíte comportamental cobre 104 cenários independentes, incluindo os recursos de fechamento, eventos, persistência, parâmetros e teclado. A execução sem `--drag-only` inclui também a suíte visual. Os testes usam WPF real com coordenadas/estado de entrada simulados; não substituem a revisão com mouse físico, leitor de tela e monitores em diferentes escalas.

### Configurações no visualizador
O botão **Configurações das abas** abre os ajustes de raio uniforme, espaços, borda, cores, fonte, posição, sombra, cursor, limiar de arraste, prévia, animação e fechamento. Por padrão, aplicar altera os três modelos; o seletor permite escolher somente um. A posição de cada modelo é preservada até selecionar outra posição explicitamente. Os valores são validados antes de aplicar. Os valores iniciais do formulário vêm do modelo Documentos.

Os botões Animar e Prévia alteram os três modelos. Salvar/restaurar organização inclui Documentos, Lateral e Simples, com leitura compatível com o arquivo anterior. Salvar organização persiste ordem/seleção. Salvar configurações grava aparência e comportamento em arquivo separado; alterações não salvas permanecem na sessão. Chaves e comandos MVVM continuam definidos conforme a fonte de cada modelo.

Verificação do painel: `dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --settings-only`.

O checkbox **Permitir exclusão de abas** controla CanCloseTabs nos três modelos. Desmarcar bloqueia RequestCloseTab e o comando de fechamento, oculta os botões de fechar e desabilita Remover selecionada. ShowCloseButtons continua sendo apenas uma preferência visual independente.

## Configurações reutilizáveis em qualquer aplicação

A biblioteca não escolhe caminhos, não grava arquivos e não conhece os temas do visualizador.

```csharp
var configuration = tabs.CaptureConfiguration();
var json = JsonSerializer.Serialize(configuration); // a aplicação escolhe o armazenamento
var restored = JsonSerializer.Deserialize<TabControlConfiguration>(json)!;
CustomTabControl.ValidateConfiguration(restored); // opcional; RestoreConfiguration também valida
 tabs.RestoreConfiguration(restored);
 tabs.ResetConfiguration();
```

O objeto versionado usa valores textuais com cultura invariável. Inclui as propriedades visuais e comportamentais expostas no painel, posição das abas, permissões e cursor. Valores ausentes mantêm a configuração atual; propriedades desconhecidas, versões incompatíveis e valores inválidos são rejeitados antes da aplicação. Comandos, itens, templates, chaves de documentos e seleção não são serializados. Ordem/seleção continuam atendidas pela API de estado.

A restauração preserva bindings com SetCurrentValue. Alterações futuras na fonte do binding podem prevalecer. ResetConfiguration usa os valores padrão registrados na biblioteca (inclusive abas superiores e botões de fechar ocultos), sem remover os documentos. Valores como brushes/cursor precisam ser representáveis pelo conversor WPF; recursos personalizados sem representação textual não são portáveis por esta API. Cores são capturadas como valores, não como chaves DynamicResource.

No visualizador, **Salvar configurações** grava `%LOCALAPPDATA%\CustomTabControl.Demo\configuration.json`, com um registro por modelo e o tema da aplicação. A leitura é automática ao abrir ou pelo botão **Restaurar configurações**. O arquivo de organização anterior continua separado. No painel, **Restaurar padrões** respeita o modelo escolhido; para persistir o resultado, use Salvar configurações. Alternar tema reaplica suas cores aos três modelos. Alterações não salvas permanecem somente na sessão.

Teste da API e integração de arquivo: `dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --configuration-only`.

# CustomTabControl

Controle de abas reutilizável para WPF / .NET 10. O visual conecta a aba selecionada ao painel, com cantos arredondados e sombra discreta.

## Executar

Abra `CustomTabControl.sln` no Visual Studio e defina `CustomTabControl.Demo` como projeto de inicialização, ou execute:

```powershell
dotnet run --project src/CustomTabControl.Demo
```

## Estrutura

- `src/CustomTabControl.Controls`: controles, conversor de espaçamento e templates em `Themes/Generic.xaml`.
- `src/CustomTabControl.Demo`: exemplos em XAML e MVVM, documentos dinâmicos e alternância de tema.
- `tests/CustomTabControl.SmokeTests`: verificações executáveis de carregamento e comportamento, sem pacotes externos.

## Usar em outro projeto WPF

Adicione uma referência ao projeto Controls e declare:

```xml
xmlns:controls="clr-namespace:CustomTabControl.Controls;assembly=CustomTabControl.Controls"
```

```xml
<controls:CustomTabControl CornerRadius="12" TabSpacing="4" Padding="24">
    <controls:CustomTabItem Header="Primeira">
        <TextBlock Text="Conteúdo livre" />
    </controls:CustomTabItem>
    <controls:CustomTabItem Header="Segunda" />
</controls:CustomTabControl>
```

O dicionário padrão é carregado automaticamente pelo WPF. `TabItem` nativo também é aceito. Com `ItemsSource`, são gerados contêineres `CustomTabItem`.

## MVVM

Use `ItemsSource`, `SelectedItem` com binding de duas vias, `ItemTemplate` para os cabeçalhos e `ContentTemplate` para o conteúdo. `HeaderTemplate` também funciona em itens explícitos. Veja a demonstração para um exemplo completo com `ObservableCollection` e comandos.

O conteúdo visual segue o ciclo de vida padrão do TabControl: não há cache de uma árvore visual por aba. Armazene os dados editáveis no modelo, como no exemplo de anotações, para preservá-los ao alternar abas.

## Aparência

Propriedades: `CornerRadius` controla o painel; `TabSpacing` define o espaço entre cabeçalhos, em unidades independentes de dispositivo. Valores negativos ou não finitos são rejeitados. `Padding`, `Background`, `Foreground`, `BorderBrush`, `BorderThickness` e fontes usam as propriedades WPF existentes.

Recursos substituíveis no escopo do controle, janela ou aplicativo:

| Chave | Uso |
| --- | --- |
| `Tabs.Surface` | Fundo do painel e aba ativa |
| `Tabs.Hover` | Silhueta contínua da aba sob o mouse, atrás da superfície selecionada |

| `Tabs.Text` | Texto ativo |
| `Tabs.Muted` | Texto inativo |
| `Tabs.Edge` | Borda |
| `Tabs.Focus` | Indicador de foco |
| `Tabs.HeaderPadding` | Espaçamento interno do cabeçalho (`Thickness`) |

Os pincéis usam `DynamicResource`, permitindo trocar o tema durante a execução. Para derivar um estilo com `BasedOn="{StaticResource Tabs.ItemStyle}"`, mescle explicitamente `/CustomTabControl.Controls;component/Themes/Generic.xaml` nos recursos, como faz a demonstração.

## Comportamento e limites

- O template orienta a faixa conforme `TabStripPlacement`. O modelo lateral demonstra Left e Right.
- Cabeçalhos em uma linha com barra de rolagem horizontal automática quando necessário.
- A mudança de seleção traz o cabeçalho selecionado para a área visível.
- Estados normal, mouse sobre, selecionado, foco pelo teclado e desabilitado.
- Navegação herdada do TabControl: Tab, setas e Ctrl+Tab. A validação interativa com teclado e leitor de tela ainda deve ser feita no ambiente de uso.
- A remoção da demonstração é um comando externo; não há botão de fechar dentro das abas ou janelas destacáveis.
- Escala e medidas seguem as unidades independentes de dispositivo do WPF; a revisão em monitores com diferentes DPIs ainda deve ser feita no ambiente de uso.

## Validar

```powershell
dotnet build CustomTabControl.sln -c Release
dotnet run --project tests/CustomTabControl.SmokeTests -c Release
```

Os testes verificam criação de contêineres, templates, itens desabilitados, adição/remoção, seleção vinculada, rolagem, dados preservados no modelo, troca de tema, layout compacto e coleção vazia. Um caminho PNG opcional após `--` salva uma prévia renderizada do tema escuro.

## Superfície unificada

A superfície é desenhada por um único `Path` (`PART_Surface`). Um único contorno fechado percorre o corpo e a parte visível da aba selecionada, incluindo arcos côncavos na ligação entre eles. O valor uniforme de `CornerRadius` define os cantos e as curvas de ligação. Se faltar espaço, todas as curvas do contorno usam o mesmo raio efetivo reduzido. Preenchimento, contorno e sombra são aplicados somente à geometria resultante; a aba selecionada tem fundo transparente e o corpo funciona apenas como contêiner de layout. A forma acompanha seleção, redimensionamento e rolagem.

A borda percorre o contorno externo inteiro. Como esse contorno é contínuo, a espessura do traço usa o maior componente de `BorderThickness`; prefira valores uniformes, como `1` ou `2`. A barra horizontal aparece acima dos cabeçalhos para manter o contato da aba ativa com o corpo.

Os testes geométricos verificam que existe um único contorno fechado e nenhuma borda interna na junção, inclusive após trocar a seleção. Também verificam os arcos de ligação com diferentes valores de CornerRadius, incluindo zero.


## Primeira aba sem recuo

Defina `HeaderIndent="0"` no controle para alinhar a primeira aba à borda esquerda do corpo. Valores positivos definem a distância em unidades WPF. O padrão `NaN` calcula o espaço automaticamente a partir do raio.

Com a aba ativa rente à esquerda, a lateral segue contínua, sem curva côncava esquerda; o canto superior arredondado pertence à aba. A curva direita permanece. Recuos pequenos reduzem uniformemente o raio efetivo do contorno para caber no espaço disponível. A demonstração inferior usa recuo zero.


O espaçamento entre abas agora é zero por padrão (TabSpacing). O destaque do mouse usa o mesmo construtor de contorno com curvas da superfície ativa e é desenhado atrás dela. Os cabeçalhos não desenham mais um retângulo de fundo próprio. A demonstração mantém HeaderIndent em zero.


## Reorganizar abas

Arraste um cabeçalho com o botão esquerdo e solte na posição indicada pela linha de destaque. O movimento só começa após deslocar o ponteiro mais de 5 pixels físicos a partir do clique, considerando apenas o eixo da faixa de abas. Nas extremidades da faixa, a rolagem horizontal continua enquanto o ponteiro permanece ali. Esc, perda de captura ou soltar além dos limites do eixo de reordenação cancela sem alterar a ordem. Durante o arraste, o movimento perpendicular é ignorado: nas abas superiores, mover o mouse para cima ou para baixo mantém o destino correspondente à posição horizontal.

`CanReorderTabs` é `true` por padrão; configure `false` para desativar. Também é possível chamar `MoveTab(indiceAtual, indiceFinal)`, que retorna se a operação foi aceita. A reordenação fica restrita ao mesmo controle e não parte de abas desabilitadas. A seleção e os objetos de conteúdo são preservados; clicar em outra aba antes de arrastar mantém o comportamento normal de seleção.

Com MVVM, use `ObservableCollection<T>`: a operação chama `Move`, atualizando a origem com uma única notificação. Itens explícitos e listas mutáveis também são aceitos. Fontes somente leitura, arrays e visões ordenadas, filtradas ou agrupadas não permitem reordenação. Não há transferência entre controles ou janelas.

Os testes cobrem movimentos nos dois sentidos, atualização da origem, seleção, dados editados, itens explícitos, listas comuns, fontes incompatíveis, indicador de destino, cancelamento e rolagem nas extremidades. A interação física de arrastar com o mouse deve ser conferida na demonstração.

### Cursor ao pressionar a aba

`TabDragCursor` define o cursor mostrado somente durante o arraste, após deslocar o ponteiro mais de 5 pixels físicos com o botão pressionado. Cliques e movimentos até 5 pixels mantêm o cursor normal. O padrão é `ScrollWE` (rolagem horizontal nativa). Ao soltar ou cancelar, o cursor normal volta sem alterar a propriedade `Cursor` do controle.

```xml
<controls:CustomTabControl TabDragCursor="ScrollWE" />
```

Também aceita outros cursores WPF, como `Arrow`, `Cross` ou `SizeAll`, e instâncias de `Cursor` fornecidas via código ou binding. Apenas passar o mouse não ativa esse cursor.




## Sombra configurável

Todas as propriedades aceitam XAML, estilos e bindings e atualizam o efeito em tempo de execução. O efeito continua restrito à superfície unificada, sem afetar os textos e campos.

| Propriedade | Padrão | Significado |
| --- | --- | --- |
| `IsShadowEnabled` | `True` | Liga/desliga a sombra |
| `ShadowColor` | `#494949` | Cor |
| `ShadowOpacity` | `0.5` | Opacidade entre 0 e 1 |
| `ShadowBlurRadius` | `10` | Raio de desfoque, não negativo |
| `ShadowDepth` | `0` | Distância, não negativa |
| `ShadowDirection` | `315` | Direção em graus; não altera o resultado quando a distância é zero |

```xml
<controls:CustomTabControl
    IsShadowEnabled="True"
    ShadowColor="#494949"
    ShadowOpacity="0.5"
    ShadowBlurRadius="10"
    ShadowDepth="0"
    ShadowDirection="315" />
```

`IsShadowEnabled="False"` remove o efeito, preservando a configuração para quando for reativado. Spread permanece zero; não há expansão adicional do contorno.

O botão Alternar tema percorre Escuro → Claro → Cinza e laranja → Escuro. O terceiro tema usa cinzas neutros e detalhes laranja inspirados na referência. A troca mantém a aba selecionada e os dados dos documentos.



## Prévia animada de reordenação

Após ultrapassar 5 pixels, uma cópia visual do cabeçalho acompanha o ponteiro e os demais cabeçalhos deslizam em 180 ms para abrir espaço. O cabeçalho original fica oculto durante a prévia, evitando texto duplicado. O painel de conteúdo permanece parado. A prévia é limitada à faixa de cabeçalhos e move-se somente no eixo das abas. Top/Bottom fixam a coordenada vertical; Left/Right fixam a horizontal. O template e o cálculo de destino agora acompanham a orientação da faixa.

O cálculo do destino usa posições de layout, não as posições animadas, para evitar oscilações. Nenhum item da coleção é movido até soltar. Cancelar remove a prévia e anima os cabeçalhos de volta; concluir aplica a ordem e remove os deslocamentos temporários.



## Modelo lateral com ícones

A demonstração inclui um terceiro controle com quatro páginas: Licença, Banco de dados, Procedimentos e Informações. Os PNGs em Assets foram fornecidos no arquivo de referÃªncia e estão incorporados como recursos da aplicação. O seletor Esquerda/Direita alterna a posição da faixa. Os dados exibidos são exemplos locais.

Nesse modelo, a prévia e os deslocamentos animados usam o eixo vertical, o cursor é ScrollNS, o indicador é horizontal e a rolagem nas extremidades é vertical. A posição horizontal do ponteiro é ignorada ao calcular o destino. O tema Cinza e laranja aproxima o visual da referência.

## Arredondamento uniforme

Use `CornerRadius="12"` para configurar um único raio para o corpo, as abas, as ligações côncavas, o destaque do mouse e a prévia de arraste. `0` deixa o contorno reto. Valores negativos, não finitos ou quatro raios diferentes são rejeitados para preservar a uniformidade.

```xml
<controls:CustomTabControl CornerRadius="16" />
```

Nas dimensões normais, o raio aplicado é o informado. Se não houver espaço, o contorno reduz todas as suas curvas para um mesmo raio efetivo. Quando a aba está rente à borda, a ligação desse lado é reta, mantendo a lateral contínua.

## Suíte consolidada de drag and drop

Os testes de arraste estão separados dos testes visuais. Execute `dotnet run --project tests/CustomTabControl.SmokeTests -c Release -- --drag-only` para rodar os 43 cenários independentes. Acrescente `--report resultado.json` para gerar o relatório. O comando sem argumentos executa a validação completa. Consulte `tests/CustomTabControl.SmokeTests/README.md` para cobertura e limites da simulação.


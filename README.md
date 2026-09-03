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

- Primeira versão com abas no topo; outras posições de `TabStripPlacement` não são implementadas pelo template.
- Cabeçalhos em uma linha com barra de rolagem horizontal automática quando necessário.
- A mudança de seleção traz o cabeçalho selecionado para a área visível.
- Estados normal, mouse sobre, selecionado, foco pelo teclado e desabilitado.
- Navegação herdada do TabControl: Tab, setas e Ctrl+Tab. A validação interativa com teclado e leitor de tela ainda deve ser feita no ambiente de uso.
- A remoção da demonstração é um comando externo; não há botão de fechar dentro das abas, reordenação ou janelas destacáveis.
- Escala e medidas seguem as unidades independentes de dispositivo do WPF; a revisão em monitores com diferentes DPIs ainda deve ser feita no ambiente de uso.

## Validar

```powershell
dotnet build CustomTabControl.sln -c Release
dotnet run --project tests/CustomTabControl.SmokeTests -c Release
```

Os testes verificam criação de contêineres, templates, itens desabilitados, adição/remoção, seleção vinculada, rolagem, dados preservados no modelo, troca de tema, layout compacto e coleção vazia. Um caminho PNG opcional após `--` salva uma prévia renderizada do tema escuro.

## Superfície unificada

A superfície é desenhada por um único `Path` (`PART_Surface`). Um único contorno fechado percorre o corpo e a parte visível da aba selecionada, incluindo arcos côncavos na ligação entre eles. Os raios superiores de `CornerRadius` definem também os cantos da aba ativa e as curvas de ligação correspondentes; são limitados apenas quando não há espaço disponível. Preenchimento, contorno e sombra são aplicados somente à geometria resultante; a aba selecionada tem fundo transparente e o corpo funciona apenas como contêiner de layout. A forma acompanha seleção, redimensionamento e rolagem.

A borda percorre o contorno externo inteiro. Como esse contorno é contínuo, a espessura do traço usa o maior componente de `BorderThickness`; prefira valores uniformes, como `1` ou `2`. A barra horizontal aparece acima dos cabeçalhos para manter o contato da aba ativa com o corpo.

Os testes geométricos verificam que existe um único contorno fechado e nenhuma borda interna na junção, inclusive após trocar a seleção. Também verificam os arcos de ligação com diferentes valores de CornerRadius, incluindo zero.


## Primeira aba sem recuo

Defina `HeaderIndent="0"` no controle para alinhar a primeira aba à borda esquerda do corpo. Valores positivos definem a distância em unidades WPF. O padrão `NaN` calcula o espaço automaticamente a partir do raio.

Com a aba ativa rente à esquerda, a lateral segue contínua, sem curva côncava esquerda; o canto superior arredondado pertence à aba. A curva direita permanece. Recuos pequenos reduzem os raios locais apenas para caber no espaço disponível. A demonstração inferior usa recuo zero.


O espaçamento entre abas agora é zero por padrão (TabSpacing). O destaque do mouse usa o mesmo construtor de contorno com curvas da superfície ativa e é desenhado atrás dela. Os cabeçalhos não desenham mais um retângulo de fundo próprio. A demonstração mantém HeaderIndent em zero.


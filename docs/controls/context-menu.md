# ContextMenu

O `ContextMenu` mantém os comandos, bindings, teclado, itens marcáveis e submenus do controle nativo do WPF, com o visual da biblioteca.

```xml
<Button Content="Opções">
    <Button.ContextMenu>
        <dlh:ContextMenu CornerRadius="8"
                         ItemPadding="12,8"
                         IconColumnWidth="28"
                         IsShadowEnabled="True">
            <MenuItem Header="Atualizar" InputGestureText="F5"
                      dlh:MenuItemAssist.Value="Disponível">
                <MenuItem.Icon>
                    <TextBlock Text="↻" />
                </MenuItem.Icon>
            </MenuItem>
            <dlh:ToggleMenuItem Header="Exibir detalhes"
                                IsChecked="{Binding ShowDetails, Mode=TwoWay}"
                                CheckedIcon="●"
                                UncheckedIcon="○" />
            <dlh:ChoiceMenuItem Header="Tema"
                                SelectedValue="{Binding Theme, Mode=TwoWay}"
                                SelectedIndex="0"
                                CycleDirection="Forward"
                                IsCycleWrappingEnabled="True">
                <dlh:ChoiceMenuOption Content="Escuro" Value="Dark" Icon="☾" />
                <dlh:ChoiceMenuOption Content="Claro" Value="Light" Icon="☀" />
                <dlh:ChoiceMenuOption Content="Sistema" Value="System" Icon="◐" />
            </dlh:ChoiceMenuItem>
            <Separator />
            <MenuItem Header="Exportar">
                <MenuItem Header="Arquivo CSV" />
                <MenuItem Header="Planilha" IsEnabled="False" />
            </MenuItem>
        </dlh:ContextMenu>
    </Button.ContextMenu>
</Button>
```

Use `Background`, `Foreground`, `BorderBrush`, `BorderThickness` e `Padding` para a superfície. `HoverBrush`, `CheckedBrush`, `SeparatorBrush`, `DisabledOpacity`, `ItemPadding`, `IconSize` e `IconColumnWidth` controlam os itens. A sombra utiliza `IsShadowEnabled`, `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` e `ShadowDepth`.

Cada linha usa cinco colunas alinhadas: ícone, título, valor, atalho e seta. Use `MenuItemAssist.Value` e `MenuItemAssist.ValueTemplate` para preencher a coluna de valor em qualquer `MenuItem`; colunas sem conteúdo permanecem vazias. A mesma estrutura é aplicada recursivamente aos submenus.

Defina `SubmenuPlacementDirection="Left"` para usar o menu junto à borda direita de uma janela. Somente a coluna da seta passa para o lado esquerdo, o indicador aponta para a esquerda e o submenu abre desse lado. Ícone, título, valor e atalho preservam sua ordem e orientação. O padrão é `Right`.

O menu mantém somente um caminho ativo da árvore. Ao interagir com outro ramo, os submenus incompatíveis são fechados automaticamente. Outros componentes também podem controlar esse estado:

```csharp
menu.ActivatePath(item);  // mantém o item, seus pais e fecha os outros ramos
menu.CollapseAfter(item); // mantém os pais e fecha o item e os níveis posteriores
menu.CollapseAll();       // fecha toda a árvore
```

Para uma composição fixa que não use `ContextMenu`, marque o contêiner com `dlh:MenuInteraction.IsScopeRoot="True"` e use `MenuInteraction.ActivatePath`, `CollapseAfter` ou `CollapseAll`, passando esse contêiner como escopo. Escopos diferentes não interferem entre si.

`ToggleMenuItem` fornece um estado booleano bidirecional e aceita `CheckedIcon` e `UncheckedIcon`. `ChoiceMenuItem` preenche automaticamente a coluna de valor com `SelectedContent`, percorre as opções pelo clique principal e abre a lista completa pela seta. A seleção pode ser ligada por `SelectedIndex`, `SelectedItem` ou `SelectedValue`; as três propriedades usam binding bidirecional por padrão. Para coleções de modelos, use `DisplayMemberPath`, `SelectedValuePath` e `IconMemberPath`. `CycleDirection`, `IsCycleWrappingEnabled` e `DropDownButtonWidth` personalizam a interação.

O menu aberto com o botão direito nos cabeçalhos do `DataGridView` já é uma instância desse componente.

No `DataGridView`, o menu do cabeçalho agrupa primeiro as ações da coluna, depois os submenus `Ordenação` e `Colunas visíveis`, e por último as ações gerais. O menu das linhas mostra a fixação da linha e da coluna clicada, identificada pelo nome. Ações de limpeza aparecem quando aplicáveis, e `Desafixar todas...` aparece a partir de duas fixações. Ao atingir um limite de fixação, a ação permanece desabilitada com uma explicação no tooltip. Os mesmos menus são usados nas áreas fixas e móveis.

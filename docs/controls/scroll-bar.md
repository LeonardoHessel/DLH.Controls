# ScrollBar

Use `controls:ScrollBar` diretamente ou dentro do template de qualquer `ScrollViewer`, lista ou controle próprio. Os comandos, teclado, automação e propriedades nativas continuam disponíveis.

```xml
<controls:ScrollBar Orientation="Vertical"
                    Minimum="0"
                    Maximum="100"
                    Value="35"
                    Thickness="10"
                    CornerRadius="5"
                    TrackBrush="#3D4046"
                    ThumbBrush="#686D77"
                    ThumbHoverBrush="#8B919D"
                    ShowButtons="False" />
```

`CornerRadius` aceita um valor uniforme; na renderização ele é limitado automaticamente à metade da espessura. A sombra é opcional por `IsShadowEnabled`, e seus parâmetros podem ser alterados com `ShadowColor`, `ShadowOpacity`, `ShadowBlurRadius` e `ShadowDepth`.

O `DataGridView` já utiliza esse componente internamente. Suas propriedades `ScrollBarThickness`, `ScrollBarTrackBrush`, `ScrollBarThumbBrush` e `ScrollBarThumbHoverBrush` foram preservadas.

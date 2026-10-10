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

## Propriedades e padrões

| Propriedade | Padrão | Contrato |
|---|---|---|
| `Thickness` | 10 | Espessura no eixo transversal; finita e maior que zero |
| `CornerRadius` | 5 | Raio uniforme e não negativo; limitado a metade da espessura ao renderizar |
| `TrackBrush` | `#3D4046` | Pincel da trilha |
| `ThumbBrush` | `#686D77` | Pincel do polegar |
| `ThumbHoverBrush` | `#8B919D` | Pincel do polegar sob o mouse |
| `ThumbPressedBrush` | `#A6ACB8` | Pincel do polegar pressionado |
| `TrackPadding` | 2 | Espaço entre trilha e polegar; valores finitos e não negativos |
| `ThumbOpacity` | 0,72 | Número finito entre 0 e 1 |
| `ShowButtons` | `false` | Exibe os botões de rolagem nas extremidades |
| `IsShadowEnabled` | `false` | Liga a sombra sem apagar seus parâmetros |
| `ShadowColor` | `#494949` | Cor da sombra |
| `ShadowOpacity` | 0,5 | Número finito entre 0 e 1 |
| `ShadowBlurRadius` | 10 | Finito e não negativo |
| `ShadowDepth` | 0 | Finito |

O `DataGridView` já utiliza esse componente internamente. Suas propriedades `ScrollBarThickness`, `ScrollBarTrackBrush`, `ScrollBarThumbBrush` e `ScrollBarThumbHoverBrush` foram preservadas.

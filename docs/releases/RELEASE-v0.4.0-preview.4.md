# Release v0.4.0-preview.4 — DLH.Controls

- **Data:** 14 de setembro de 2026
- **Tipo:** pré-lançamento
- **Homologação visual:** concluída

## Repositório e publicação

| Item | Valor |
|---|---|
| Linha oficial | `main` |
| Tag | [`v0.4.0-preview.4`](https://github.com/LeonardoHessel/DLH.Controls/releases/tag/v0.4.0-preview.4) |
| NuGet | [`DLH.Controls.Wpf` 0.4.0-preview.4](https://www.nuget.org/packages/DLH.Controls.Wpf/0.4.0-preview.4) |
| Publicação | GitHub Release com Trusted Publishing do NuGet |

## Alterações incorporadas

- Repositório reorganizado com documentação separada por controles, guias, desenvolvimento, releases e planos históricos.
- Modelos de issue e pull request, código de conduta, responsáveis, Dependabot e guia operacional do GitHub adicionados.
- Versão do pacote centralizada e conferida automaticamente contra a tag de publicação.
- Validação ampliada para consumir o pacote em duas aplicações WPF isoladas: uma programática e outra construída em XAML e renderizada em uma janela real.
- Correção do alinhamento do cabeçalho, da barra vertical e das regiões fixadas do `DataGridView`, com divisórias de 1 px.
- Rolagem do `DataGridView` otimizada quando existem colunas fixadas, com controles demonstrativos para suavização, passo e duração.
- Bordas dos itens do `ContextMenu` configuráveis nos estados normal, destacado e marcado.
- Larguras independentes para as cinco colunas do menu principal e dos submenus.
- Direção e deslocamentos horizontal e vertical dos submenus configuráveis.
- Capturas do `TabControl`, `DataGridView`, `ScrollBar` e `ContextMenu` atualizadas após a homologação visual.

## Validação

- Compilação Release: concluída sem avisos nem erros.
- Testes automatizados locais: **73/73** aprovados.
- Comparações visuais: aprovadas.
- Pacote gerado: `DLH.Controls.Wpf.0.4.0-preview.4.nupkg`.
- Consumo programático isolado: aprovado.
- Consumo em XAML com renderização de janela: aprovado em escala DPI 1,00.
- Publicação automatizada pelo workflow `Publish NuGet release` após a criação deste pré-lançamento.

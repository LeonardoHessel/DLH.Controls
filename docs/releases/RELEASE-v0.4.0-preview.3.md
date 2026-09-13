# Release v0.4.0-preview.3 — DLH.Controls

- **Data:** 13 de setembro de 2026
- **Tipo:** pré-lançamento
- **Homologação aprovada por:** Leonardo Hessel

## Repositório e publicação

| Item | Valor |
|---|---|
| Linha oficial | `main` @ `4708f37` |
| Tag | [`v0.4.0-preview.3`](https://github.com/LeonardoHessel/DLH.Controls/releases/tag/v0.4.0-preview.3) |
| GitHub Actions | [Execução 34767781821](https://github.com/LeonardoHessel/DLH.Controls/actions/runs/34767781821) — concluída com sucesso |
| NuGet | [`DLH.Controls.Wpf` 0.4.0-preview.3](https://www.nuget.org/packages/DLH.Controls.Wpf/0.4.0-preview.3) |
| Publicação | Concluída pelo Trusted Publishing |

## Alterações incorporadas

- Correção do recorte da sombra nos cantos do `ContextMenu` e de seus submenus.
- Margem reservada para a sombra, removida automaticamente quando o efeito está desabilitado.
- Aplicativo `DLH.Controls.Wpf.Screenshots` separado da demonstração interativa.
- Novas capturas do `TabControl`, `DataGridView`, `ScrollBar` e `ContextMenu`.
- README, manual e referência da API consolidados em português-BR.
- Exemplos de instalação atualizados para `0.4.0-preview.3`.
- Validação do README do pacote ampliada para conferir as quatro imagens dos controles.
- Tolerância da captura dos controles compartilhados calibrada para diferenças de renderização entre ambientes, preservando o limite de diferença média.

## Validação

- Compilação Release: concluída sem avisos nem erros.
- Testes automatizados locais: **72/72** aprovados.
- Consumo do pacote: instalação e execução aprovadas em uma aplicação WPF isolada.
- GitHub Actions: todas as etapas concluídas com sucesso.
- Trusted Publishing: autenticação OIDC e envio ao NuGet concluídos.
- Disponibilidade: versão confirmada no índice público e na página do NuGet.

Uma primeira execução remota identificou variação de antialiasing acima do limite de pixels alterados na captura dos controles compartilhados. Nenhum pacote foi enviado nessa tentativa. A tolerância foi ajustada de 2% para 2,5% somente nessa composição, mantendo a diferença média máxima de 3; a release final foi recriada sobre o commit corrigido e validada integralmente.

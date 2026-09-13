# Release v0.4.0-preview.2 — DLH.Controls

- **Data:** 13 de setembro de 2026
- **Tipo:** pré-lançamento
- **Homologação aprovada por:** Leonardo Hessel

## Repositório e publicação

| Item | Valor |
|---|---|
| Linha oficial | `main` @ `a5c7524` |
| Tag | [`v0.4.0-preview.2`](https://github.com/LeonardoHessel/DLH.Controls/releases/tag/v0.4.0-preview.2) |
| GitHub Actions | [Execução 34734128298](https://github.com/LeonardoHessel/DLH.Controls/actions/runs/34734128298) — concluída com sucesso |
| NuGet | [`DLH.Controls.Wpf` 0.4.0-preview.2](https://www.nuget.org/packages/DLH.Controls.Wpf/0.4.0-preview.2) |
| Publicação | Concluída pelo Trusted Publishing |

## Correção incorporada

- Os limites `MaxPinnedRows` e `MaxPinnedColumns` agora são reavaliados durante a redução das coleções fixadas.
- Linhas e colunas usam proteções de reentrada independentes, permitindo que eventos de desfixação alterem o limite da outra dimensão.
- Um teste automatizado reproduz a alteração dos dois limites durante `RowUnpinned` e confirma o estado final das coleções.

Essa correção substitui a versão `0.4.0-preview.1`, cuja tag foi preservada como registro histórico e que não foi publicada no NuGet.

## Validação

- Compilação: concluída sem avisos nem erros.
- Testes automatizados: **72/72** aprovados.
- Suíte legada: **123/123** aprovada.
- Consumo do pacote: instalação e execução aprovadas em uma aplicação WPF isolada.
- GitHub Actions: todas as 12 etapas do trabalho `publish` concluídas com sucesso, incluindo autenticação confiável e envio ao NuGet.
- Disponibilidade: página pública da versão e arquivo `.nupkg` confirmados após a publicação.


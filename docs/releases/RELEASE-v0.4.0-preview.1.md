# Release v0.4.0-preview.1 — DLH.Controls

- **Data/hora:** 2026-09-12
- **Executada por:** Leonardo Hessel (com Claude Sonnet 5)
- **Homologação aprovada por:** Leonardo Hessel
- **Tipo:** Release (preview)

## Repositório

| Item | Valor |
|---|---|
| VCS | git (GitHub) |
| Linha oficial | `main` @ `a88be09` |
| Tag | [`v0.4.0-preview.1`](https://github.com/LeonardoHessel/DLH.Controls/releases/tag/v0.4.0-preview.1) |
| Merge | Não aplicável — repositório usa branch única `main`, sem `develop`; a release é uma tag direta sobre o `main` atual. |
| Publicação | Pendente: criar GitHub Release marcada **Pre-release** apontando para a tag, para disparar `publish.yml` (NuGet.org via Trusted Publishing) |

## Commits incorporados

55 commits desde `v0.3.0-preview.2` (30 fix, 18 feat — incluindo 1 `feat!` breaking, 2 docs, 1 refactor, 1 test, 1 demo). Destaques:

| SHA | Descrição |
|---|---|
| `b3cde1d` | **feat!:** padroniza nomes dos controles e extrai `ScrollBar` (breaking: `CustomTabControl`→`TabControl`) |
| `c0bcc41` | feat: adiciona `ContextMenu` compartilhado |
| `e00d4d9`…`219db6c` | feat/fix: fixação aderente de linhas e colunas do `DataGridView` (contratos, posicionamento, interseções, separadores, popup de detalhes) |
| `fb728dc` | feat: organiza menus de contexto do `DataGridView` por escopo de ação |
| `9495945` | fix: corrige vazamento de overlays, métricas obsoletas e lacunas de eventos na fixação (1ª rodada de revisão) |
| `a0fe94a` | docs: corrige afirmação obsoleta sobre preservação de fixação e documenta novo comportamento de limites |
| `51dff6d` | fix: corrige regressões encontradas na 2ª rodada de revisão da fixação |
| `a88be09` | docs: prepara notas de release da 0.4.0-preview.1 |

Histórico completo: `git log v0.3.0-preview.2..v0.4.0-preview.1 --oneline`.

## Conflitos

| Arquivo | Conflito | Resolução |
|---|---|---|
| (nenhum) | — | Sem `develop`, não há merge a simular; a tag foi criada direto sobre `main`, já sincronizado com `origin/main`. |

## Alterações

- Arquivos modificados: 35
- Arquivos adicionados: 25
- Arquivos renomeados: 11 (série `CustomTabControl.*` → `TabControl.*`)
- Arquivos removidos: 0
- Linhas: +6931 / -458 (`git diff --stat v0.3.0-preview.2..v0.4.0-preview.1`)

## Testes antes da tag

- `dotnet test tests/DLH.Controls.Wpf.AutomatedTests`: **71/71** aprovados
- `dotnet run --project tests/DLH.Controls.Wpf.Tests`: **123/123** aprovados (suíte legada, inclui `DRAG-DROP: 123/123 passed; 0 failed.`)

## Pendências pós-release

- [ ] **Criar a GitHub Release** apontando para a tag `v0.4.0-preview.1`, marcada **Pre-release**, e publicá-la — é isso que dispara `publish.yml` e o envio real ao NuGet.org. `gh` CLI não estava disponível neste ambiente; feito pela interface web em https://github.com/LeonardoHessel/DLH.Controls/releases/new?tag=v0.4.0-preview.1.
- [ ] Confirmar que o workflow `publish.yml` rodou com sucesso (aba Actions do repositório) e que o pacote apareceu em https://www.nuget.org/packages/DLH.Controls.Wpf/0.4.0-preview.1.
- [ ] Confirmar que a política de Trusted Publishing no NuGet.org ainda está ativa (validade de 7 dias em repositório privado — não se aplica aqui pois o repositório é público, mas vale checar).

## Observações

- O `CHANGELOG.md` continha uma lacuna: a release anterior (`v0.3.0-preview.2`) nunca ganhou uma seção própria — seu conteúdo ficava em "Não publicado" junto com trabalho ainda não lançado. A seção `0.4.0-preview.1` criada nesta release consolida os dois (marcado explicitamente no changelog).
- `b3cde1d` é uma alteração incompatível (`feat!`): consumidores do pacote precisam trocar `CustomTabControl`/`CustomTabControlItem` por `TabControl`/`TabControlItem` ao atualizar.
- A versão do `.csproj` (`<Version>0.2.0-preview.4</Version>`) foi deliberadamente **não** alterada, conforme `docs/Publishing.md`: a versão publicada vem da tag e substitui a versão base durante o pack, sem tocar no arquivo do projeto.

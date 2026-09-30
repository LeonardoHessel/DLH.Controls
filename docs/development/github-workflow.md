# Operação do repositório no GitHub

Este guia descreve o fluxo usado para manter o DLH Controls no GitHub. Ele complementa os guias de [testes e empacotamento](packaging.md) e de [publicação no NuGet](publishing.md).

## Branch principal

`main` é a linha oficial e deve permanecer sempre compilável. A proteção configurada no GitHub exige:

- branch atualizada antes da integração;
- aprovação dos checks `Compile (Debug)`, `Compile (Release)` e `validate`;
- resolução das conversas abertas em pull requests;
- histórico linear;
- bloqueio de exclusão e de force push.

A exigência de aprovação por outra pessoa não está ativa porque o projeto possui um único mantenedor. O proprietário conserva a exceção administrativa para correções urgentes, mas deve executar a validação completa antes de usá-la.

As regras de proteção são configurações externas do GitHub e não são recriadas ao clonar o repositório. Se for necessário configurá-las novamente, acesse **Settings > Branches > Branch protection rules**, selecione `main` e reproduza as regras acima.

## Alteração comum

1. Atualize o repositório local e crie uma branch curta e descritiva.
2. Faça commits pequenos, relacionados ao mesmo objetivo e escritos no imperativo.
3. Execute `./eng/Validate.ps1` no Windows.
4. Envie a branch e abra um pull request para `main`.
5. Preencha o problema, o resultado, a validação e os efeitos de compatibilidade no modelo do pull request.
6. Corrija eventuais falhas dos checks e resolva as conversas da revisão.
7. Integre usando **Squash and merge** ou **Rebase and merge**, preservando o histórico linear.
8. Exclua a branch de trabalho após a integração.

Branches de trabalho podem usar nomes como `feature/nome-curto`, `fix/nome-curto` ou `docs/nome-curto`. Evite misturar correções, mudanças visuais e reorganizações sem relação no mesmo pull request.

## Checks obrigatórios

| Check | Responsabilidade |
|---|---|
| `Compile (Debug)` | restaurar e compilar toda a solução em Debug |
| `Compile (Release)` | restaurar e compilar toda a solução em Release |
| `validate` | executar testes, cobertura, comparação visual, empacotamento e consumo do `.nupkg` |

O workflow está em `.github/workflows/ci.yml` e também pode ser executado manualmente em **Actions > Build, test and package > Run workflow**.

Antes de promover uma versão, o workflow `.github/workflows/release-readiness.yml` pode ser iniciado em **Actions > Release readiness > Run workflow**. Ele executa a validação completa, pode baixar a versão pública informada e publica os relatórios técnicos como artefatos por 30 dias. As verificações físicas permanecem no modo assistido local de `eng/Invoke-ReleaseReadiness.ps1`.

Quando um check falhar:

1. abra o trabalho que falhou e identifique a primeira etapa com erro;
2. baixe o artefato `test-results-*` quando a falha envolver testes ou comparação visual;
3. reproduza localmente com `./eng/Validate.ps1`;
4. corrija a causa e envie um novo commit;
5. não reinicie repetidamente um check determinístico sem corrigir o problema.

Variações visuais devem ser examinadas pela imagem atual e pela imagem de diferenças. Atualize uma referência somente quando a mudança visual for intencional e tiver sido revisada.

## Issues e propostas

Use os formulários em `.github/ISSUE_TEMPLATE`:

- **Relatar um problema** para defeitos reproduzíveis;
- **Sugerir uma melhoria** para novos comportamentos ou alterações de API.

Pesquise relatos existentes antes de criar outro. Vulnerabilidades não devem ser descritas publicamente; siga [SECURITY.md](../../SECURITY.md). O responsável padrão por revisão está declarado em `.github/CODEOWNERS`.

## Dependabot

O Dependabot verifica mensalmente dependências NuGet e GitHub Actions, agrupa atualizações relacionadas e aplica a etiqueta `dependencies`.

Para revisar uma atualização:

1. confira as notas oficiais e possíveis alterações incompatíveis;
2. examine os arquivos modificados e a versão resolvida;
3. aguarde os três checks obrigatórios;
4. faça teste manual quando a atualização afetar renderização, entrada, build ou publicação;
5. integre somente depois da validação completa.

Atualizações principais não devem ser integradas apenas porque o CI passou; verifique migração e compatibilidade pública.

## Preparação de uma release

1. Escolha uma versão SemVer ainda não publicada.
2. Atualize `DlhControlsVersion` em `eng/Version.props`.
3. Mova as alterações relevantes de **Não publicado** para a nova seção do `CHANGELOG.md`.
4. Atualize exemplos que exibem explicitamente a versão do pacote.
5. Execute `./eng/Validate.ps1 -PackageVersion <versão>`.
6. Integre as alterações em `main` e confirme os três checks obrigatórios.
7. Crie uma GitHub Release com uma tag `v<versão>` apontando para esse commit.
8. Marque **Pre-release** quando a versão tiver sufixo de prévia e publique a release.
9. Acompanhe o workflow **Publish NuGet release** até a conclusão.
10. Confirme a versão no GitHub Releases e no NuGet.org.

A versão da tag precisa ser igual à de `eng/Version.props`; a validação interrompe a publicação quando houver divergência. Uma versão já enviada ao NuGet não pode ser sobrescrita.

## Cancelamento ou falha de publicação

Se a release ainda estiver como rascunho, corrija o commit ou a tag antes de publicá-la. Se o workflow falhar antes do envio ao NuGet, corrija a causa e prepare uma execução rastreável; não altere uma tag pública sem avaliar se ela já foi consumida.

Se o pacote já tiver sido publicado, prepare uma nova versão. Não tente reutilizar o mesmo número. Quando uma versão apresentar um defeito grave, marque-a como não listada no NuGet somente após avaliar o impacto nos consumidores e documente a substituição no changelog e nas notas da nova release.

## Configurações externas

Estas configurações vivem nas plataformas e precisam ser verificadas separadamente:

| Configuração | Local | Verificação |
|---|---|---|
| Proteção da `main` | GitHub, **Settings > Branches** | checks, histórico linear, conversas, exclusão e force push |
| Trusted Publishing | NuGet.org, **Trusted Publishing** | proprietário, repositório, `publish.yml` e pacote `DLH.Controls.Wpf` |
| GitHub Sponsors | perfil do mantenedor e `.github/FUNDING.yml` | botão de apoio e destino correto |
| Visibilidade e permissões | GitHub, **Settings > General/Collaborators** | repositório público e acessos mínimos necessários |

Nunca registre tokens, senhas ou chaves no repositório. O workflow de publicação usa uma credencial temporária emitida pelo Trusted Publishing.

## Verificação periódica

Antes de uma release e após mudanças administrativas importantes, confirme:

- `main` protegida e sincronizada;
- checks obrigatórios ativos e aprovados;
- issues e pull requests sem credenciais ou dados pessoais;
- Dependabot sem atualizações críticas ignoradas;
- Trusted Publishing associado ao workflow correto;
- changelog, versão central e tag consistentes;
- links da documentação e imagens do README acessíveis.

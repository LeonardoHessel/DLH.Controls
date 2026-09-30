# Preparação NuGet

ID: `DLH.Controls.Wpf`. Versão atual: `0.4.0-preview.4`. Alterações posteriores permanecem em desenvolvimento até a definição de uma nova versão.
A solução tem um único projeto empacotável; novos componentes entram no mesmo projeto em `Controls/NomeDoComponente`.

O pacote inclui DLL, recursos WPF compilados , README e LICENSE.txt. Não inclui demonstração, ícones de exemplo ou testes.

## Compatibilidade com Paket

Paket utiliza o mesmo formato `.nupkg` e não exige um artefato ou metadado exclusivo. O README incluído no pacote documenta `paket add`, `paket.dependencies`, `paket.references` e `paket install`. A página do NuGet.org apresenta a opção Paket CLI automaticamente para pacotes NuGet compatíveis.

A validação de empacotamento confirma que o README efetivamente incluído no `.nupkg` contém as instruções do Paket. Isso evita publicar uma versão cuja página não explique como adicionar a biblioteca por esse gerenciador.

Autor: Leonardo D. de L. Hessel. Licença personalizada de atribuição visível, versão 1.0, incluída via PackageLicenseFile. O texto completo está em LICENSE.txt.

Antes de publicar: verificar disponibilidade/permissão do ID e configurar autenticação de publicação. Atualmente somente net10.0-windows.

A mudança de namespace/assembly de TabControl.Controls para DLH.Controls.Wpf exige atualizar referências e URIs XAML em consumidores existentes. O nome público TabControl permanece.

## GitHub Actions
O workflow `.github/workflows/ci.yml` executa em pushes para main, pull requests para main e por acionamento manual. Usa Windows e .NET 10, com permissão somente de leitura do repositório.

`eng/Validate.ps1` compila, executa as suítes pelo `dotnet test`, gera um relatório TRX, empacota somente após sucesso, verifica DLL, README e licença e instala o `.nupkg` em uma aplicação WPF temporária. Essa aplicação compila e executa usando apenas o pacote, cobrindo os recursos e APIs básicas dos dois controles. Logs e prévia ficam no artefato `test-results`; o nupkg fica no artefato `DLH.Controls.Wpf`.

Os testes podem ser filtrados por categoria:

```powershell
dotnet test tests/DLH.Controls.Wpf.AutomatedTests --filter TestCategory=DataGridView
dotnet test tests/DLH.Controls.Wpf.AutomatedTests --filter TestCategory=TabControl
dotnet test tests/DLH.Controls.Wpf.AutomatedTests --logger trx
```

A categoria `Visual` compara capturas do `TabControl`, do `DataGridView` e dos controles compartilhados com referências versionadas. Diferenças pequenas de antialiasing são toleradas; o limite de pixels alterados é de 2% para `TabControl` e `DataGridView` e 2,5% para os controles compartilhados, mantendo a diferença média máxima de 3. A execução conserva as imagens `actual` e `diff` no diretório de resultados.

No GitHub Actions, a solução é compilada separadamente em Debug e Release. A validação completa e o empacotamento só começam depois das duas configurações serem aprovadas. As capturas visual atual e de diferenças são anexadas ao resultado do teste e incluídas nos artefatos do workflow.

A execução completa também usa o coletor de cobertura do Visual Studio. O arquivo `.coverage` acompanha o TRX no artefato `test-results`; inicialmente ele serve para acompanhar a migração dos cenários para métodos independentes, sem bloquear releases por uma porcentagem arbitrária.

Após gerar o pacote, `eng/Test-PackageConsumer.ps1` cria duas aplicações WPF fora da solução e restaura dependências usando somente a pasta do `.nupkg` gerado. `CodeConsumer` exercita as APIs públicas dos quatro controles por código. `XamlConsumer` compila os quatro controles em XAML, abre uma janela real fora da área visível, aplica os templates, renderiza o conteúdo e abre o `ContextMenu`. O relatório `artifacts/test-results/package-consumers.json` registra pacote, versão, Windows, SDK, duração e resultado de cada consumidor.

Essa matriz detecta dependências acidentais de projetos ou arquivos do repositório, erros na API pública, falhas de compilação XAML, ausência de estilos e problemas básicos de carregamento e renderização. Avaliação visual subjetiva, leitor de tela e interação humana em escalas físicas diferentes continuam na revisão manual de acessibilidade.

O teste da categoria `API` compara tipos e membros públicos com `tests/DLH.Controls.Wpf.AutomatedTests/PublicApi/DLH.Controls.Wpf.txt`. Quando uma mudança pública for intencional, revise sua compatibilidade e execute o teste localmente com `UPDATE_PUBLIC_API=1`; copie o contrato gerado para a pasta versionada e revise o diff antes do commit.

Não publica no NuGet, não exige chave NuGet e não cria releases. Pode ser executado localmente no Windows com `./eng/Validate.ps1`.

Para consolidar a validação em um relatório de homologação, use `./eng/Invoke-ReleaseReadiness.ps1`. O parâmetro `-CheckPublicPackage` baixa a versão do NuGet e repete a matriz de consumo; `-Interactive` conduz e registra os testes físicos e assistivos que não podem ser concluídos pelo CI.

Publicação automática por release: consulte [publishing.md](publishing.md). O CI de push/PR continua sem publicar; o workflow publish.yml utiliza Trusted Publishing após o cadastro da política no NuGet.

# Preparação NuGet

ID: `DLH.Controls.Wpf`. Versão atual: `0.2.0-preview.2`. Alterações posteriores permanecem em desenvolvimento até a definição de uma nova versão.
A solução tem um único projeto empacotável; novos componentes entram no mesmo projeto em `Controls/NomeDoComponente`.

O pacote inclui DLL, recursos WPF compilados , README e LICENSE.txt. Não inclui demonstração, ícones de exemplo ou testes.

## Compatibilidade com Paket

Paket utiliza o mesmo formato `.nupkg` e não exige um artefato ou metadado exclusivo. O README incluído no pacote documenta `paket add`, `paket.dependencies`, `paket.references` e `paket install`. A página do NuGet.org apresenta a opção Paket CLI automaticamente para pacotes NuGet compatíveis.

A validação de empacotamento confirma que o README efetivamente incluído no `.nupkg` contém as instruções do Paket. Isso evita publicar uma versão cuja página não explique como adicionar a biblioteca por esse gerenciador.

Autor: Leonardo D. de L. Hessel. Licença personalizada de atribuição visível, versão 1.0, incluída via PackageLicenseFile. O texto completo está em LICENSE.txt.

Antes de publicar: verificar disponibilidade/permissão do ID e configurar autenticação de publicação. Atualmente somente net10.0-windows.

A mudança de namespace/assembly de CustomTabControl.Controls para DLH.Controls.Wpf exige atualizar referências e URIs XAML em consumidores existentes. O nome público CustomTabControl permanece.

## GitHub Actions
O workflow `.github/workflows/ci.yml` executa em pushes para main, pull requests para main e por acionamento manual. Usa Windows e .NET 10, com permissão somente de leitura do repositório.

`eng/Validate.ps1` compila, executa as suítes pelo `dotnet test`, gera um relatório TRX, empacota somente após sucesso, verifica DLL, README e licença e instala o `.nupkg` em uma aplicação WPF temporária. Essa aplicação compila e executa usando apenas o pacote, cobrindo os recursos e APIs básicas dos dois controles. Logs e prévia ficam no artefato `test-results`; o nupkg fica no artefato `DLH.Controls.Wpf`.

Os testes podem ser filtrados por categoria:

```powershell
dotnet test tests/DLH.Controls.Wpf.AutomatedTests --filter TestCategory=DataGridView
dotnet test tests/DLH.Controls.Wpf.AutomatedTests --filter TestCategory=TabControl
dotnet test tests/DLH.Controls.Wpf.AutomatedTests --logger trx
```

A categoria `Visual` compara a captura do `CustomTabControl` com a referência versionada. Diferenças pequenas de antialiasing são toleradas; mudanças superiores a 2% dos pixels ou diferença média superior a 3 falham. A execução conserva as imagens `actual` e `diff` no diretório de resultados.

Não publica no NuGet, não exige chave NuGet e não cria releases. Pode ser executado localmente no Windows com `./eng/Validate.ps1`.

Publicação automática por release: consulte [Publishing.md](Publishing.md). O CI de push/PR continua sem publicar; o workflow publish.yml utiliza Trusted Publishing após o cadastro da política no NuGet.

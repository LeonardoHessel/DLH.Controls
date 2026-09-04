# Preparação NuGet

ID: `DLH.Controls.Wpf`. Versão local inicial: `0.1.0-preview.1`.
A solução tem um único projeto empacotável; novos componentes entram no mesmo projeto em `Controls/NomeDoComponente`.

O pacote inclui DLL, recursos WPF compilados , README e LICENSE.txt. Não inclui demonstração, ícones de exemplo ou testes.

Autor: Leonardo D. de L. Hessel. Licença personalizada de atribuição visível, versão 1.0, incluída via PackageLicenseFile. O texto completo está em LICENSE.txt.

Antes de publicar: verificar disponibilidade/permissão do ID e configurar autenticação de publicação. Atualmente somente net10.0-windows.

A mudança de namespace/assembly de CustomTabControl.Controls para DLH.Controls.Wpf exige atualizar referências e URIs XAML em consumidores existentes. O nome público CustomTabControl permanece.

## GitHub Actions
O workflow `.github/workflows/ci.yml` executa em pushes para main, pull requests para main e por acionamento manual. Usa Windows e .NET 10, com permissão somente de leitura do repositório.

`eng/Validate.ps1` compila, executa as três suítes de integração (executáveis WPF, não dotnet test), gera o pacote somente após sucesso e verifica DLL, README e licença. Logs e prévia ficam no artefato test-results; o nupkg fica no artefato DLH.Controls.Wpf. Retenção de 14 dias. Na aba Actions, abra uma execução aprovada e baixe o pacote em Artifacts.

Não publica no NuGet, não exige chave NuGet e não cria releases. Pode ser executado localmente no Windows com `./eng/Validate.ps1`.

Publicação automática por release: consulte [Publishing.md](Publishing.md). O CI de push/PR continua sem publicar; o workflow publish.yml utiliza Trusted Publishing após o cadastro da política no NuGet.

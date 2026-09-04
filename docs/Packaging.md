# Preparação NuGet

ID: `DLH.Controls.Wpf`. Versão local inicial: `0.1.0-preview.1`.
A solução tem um único projeto empacotável; novos componentes entram no mesmo projeto em `Controls/NomeDoComponente`.

O pacote inclui DLL, recursos WPF compilados , README e LICENSE.txt. Não inclui demonstração, ícones de exemplo ou testes.

Autor: Leonardo D. de L. Hessel. Licença personalizada de atribuição visível, versão 1.0, incluída via PackageLicenseFile. O texto completo está em LICENSE.txt.

Antes de publicar: verificar disponibilidade/permissão do ID e configurar autenticação de publicação. Atualmente somente net10.0-windows.

A mudança de namespace/assembly de CustomTabControl.Controls para DLH.Controls.Wpf exige atualizar referências e URIs XAML em consumidores existentes. O nome público CustomTabControl permanece.

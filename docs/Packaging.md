# Preparação NuGet

ID: `DLH.Controls.Wpf`. Versão local inicial: `0.1.0-preview.1`.
A solução tem um único projeto empacotável; novos componentes entram no mesmo projeto em `Controls/NomeDoComponente`.

O pacote inclui DLL, recursos WPF compilados e README. Não inclui demonstração, ícones de exemplo ou testes.

Antes de publicar: definir a licença de distribuição, confirmar autoria (metadado inicial DLH), verificar disponibilidade/permissão do ID, escolher versão pública e decidir se haverá suporte a outros frameworks. Atualmente somente net10.0-windows. Não há URL de repositório ou licença inventadas no pacote.

A mudança de namespace/assembly de CustomTabControl.Controls para DLH.Controls.Wpf exige atualizar referências e URIs XAML em consumidores existentes. O nome público CustomTabControl permanece.

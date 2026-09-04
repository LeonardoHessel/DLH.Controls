# DLH Controls

Biblioteca de componentes WPF distribuídos em um único pacote: **DLH.Controls.Wpf**.

## Estrutura

- `src/DLH.Controls.Wpf`: biblioteca reutilizável; componentes em `Controls/`, estilos em `Themes/`.
- `samples/DLH.Controls.Wpf.Demo`: visualizador dos componentes.
- `tests/DLH.Controls.Wpf.Tests`: testes de integração WPF.
- `docs/TabControl.md`: funcionalidades e exemplos do TabControl.
- `docs/Packaging.md`: preparação do pacote.

Requer .NET 10 e Windows. O namespace público é `DLH.Controls.Wpf`; o controle mantém o nome `CustomTabControl`.

```xml
xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf"
```

```xml
<dlh:CustomTabControl>
    <dlh:CustomTabItem Header="Início">Conteúdo</dlh:CustomTabItem>
</dlh:CustomTabControl>
```

## Desenvolvimento

```powershell
dotnet build DLH.Controls.sln -c Release
dotnet run --project samples/DLH.Controls.Wpf.Demo -c Release
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --settings-only
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --configuration-only
dotnet pack src/DLH.Controls.Wpf -c Release -o artifacts/packages
```

A demonstração preserva os arquivos de preferências anteriores em `%LOCALAPPDATA%\CustomTabControl.Demo`. Somente a biblioteca é empacotada. Nenhum pacote foi publicado. Autor: Leonardo D. de L. Hessel. Versão inicial: 0.1.0-preview.1.

## Licença

Distribuído sob a **DLH Controls — Licença de Uso com Atribuição Visível, versão 1.0**. Consulte [LICENSE.txt](LICENSE.txt) para as condições completas.

Uso comercial, modificação e redistribuição são permitidos, observadas as condições da licença, incluindo o crédito acessível aos usuários:

> Este produto utiliza DLH Controls, desenvolvido por Leonardo D. de L. Hessel.

O crédito pode aparecer em Sobre, Créditos ou Licenças de terceiros; produtos sem interface gráfica devem disponibilizá-lo na documentação ou ajuda. Esta é uma licença personalizada, não identificada como MIT.

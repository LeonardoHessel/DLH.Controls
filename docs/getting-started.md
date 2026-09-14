# Primeiros passos

O pacote `DLH.Controls.Wpf` fornece controles para aplicações WPF em Windows com .NET 10.

## Instalação

Instale a versão mais recente pelo .NET CLI:

```powershell
dotnet add package DLH.Controls.Wpf --version 0.4.0-preview.3
```

Ou use uma referência no projeto:

```xml
<ItemGroup>
    <PackageReference Include="DLH.Controls.Wpf" Version="0.4.0-preview.3" />
</ItemGroup>
```

Como a versão atual é um pré-lançamento, habilite a exibição de versões de pré-lançamento ao procurar o pacote no Visual Studio.

## Namespace XAML

Declare o namespace na janela ou no controle que utilizará a biblioteca:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf">
</Window>
```

## Primeiro controle

```xml
<dlh:TabControl CornerRadius="12">
    <dlh:TabControlItem Header="Início">
        <TextBlock Margin="24" Text="Minha primeira página" />
    </dlh:TabControlItem>
    <dlh:TabControlItem Header="Configurações">
        <TextBox Margin="24" />
    </dlh:TabControlItem>
</dlh:TabControl>
```

Os templates e estilos padrão são descobertos automaticamente pelo WPF.

## Próximas leituras

- [TabControl](controls/tab-control.md)
- [DataGridView](controls/data-grid-view.md)
- [ScrollBar](controls/scroll-bar.md)
- [ContextMenu](controls/context-menu.md)
- [Integração e MVVM](guides/integration.md)
- [Manual consolidado](Manual.md)

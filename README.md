# DLH Controls

[![CI](https://github.com/LeonardoHessel/DLH.Controls/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/LeonardoHessel/DLH.Controls/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/DLH.Controls.Wpf?label=NuGet)](https://www.nuget.org/packages/DLH.Controls.Wpf)
[![Downloads](https://img.shields.io/nuget/dt/DLH.Controls.Wpf?label=Downloads)](https://www.nuget.org/packages/DLH.Controls.Wpf)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Plataforma](https://img.shields.io/badge/plataforma-Windows-0078D4)

**DLH Controls** é uma biblioteca de controles reutilizáveis e personalizáveis para aplicações .NET. O pacote atual, **DLH.Controls.Wpf**, oferece componentes visuais para WPF com suporte a MVVM, teclado, acessibilidade e às propriedades convencionais da plataforma.

> Requer Windows e .NET 10 com WPF. A versão atual é um pré-lançamento.

## Componentes

| Componente | Finalidade | Recursos principais |
|---|---|---|
| [`TabControl`](docs/controls/tab-control.md) | Organizar páginas e documentos em abas | quatro posições, reordenação, criação, renomeação, fechamento e persistência |
| [`DataGridView`](docs/controls/data-grid-view.md) | Exibir coleções em uma tabela rica | ordenação, filtros, seleção, edição, exportação, agrupamento e fixação aderente |
| [`ScrollBar`](docs/controls/scroll-bar.md) | Padronizar a rolagem de qualquer conteúdo | dois eixos, espessura, cores, raio, sombra e botões opcionais |
| [`ContextMenu`](docs/controls/context-menu.md) | Apresentar ações contextuais consistentes | ícones, valores, atalhos, itens marcáveis, escolhas e submenus |

### TabControl

![TabControl com abas superiores, inferiores e laterais](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/tab-control.png)

### DataGridView

![DataGridView com ordenação, status, múltiplas colunas e barras personalizadas](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/data-grid-view.png)

### ScrollBar

![ScrollBar horizontal e vertical controlando uma área de conteúdo nos dois eixos](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/scroll-bar.png)

### ContextMenu

![ContextMenu aberto com submenu de exportação](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/context-menu.png)

## Instalação

### CLI do .NET

```powershell
dotnet add package DLH.Controls.Wpf --version 0.4.0-preview.3
```

### Package Manager do Visual Studio

```powershell
Install-Package DLH.Controls.Wpf -Version 0.4.0-preview.3
```

### PackageReference

```xml
<PackageReference Include="DLH.Controls.Wpf" Version="0.4.0-preview.3" />
```

### Paket CLI

Adicione diretamente ao projeto:

```powershell
paket add DLH.Controls.Wpf --version 0.4.0-preview.3 --project caminho/SeuProjeto.csproj
```

Ou inclua no arquivo `paket.dependencies`:

```text
source https://api.nuget.org/v3/index.json
nuget DLH.Controls.Wpf 0.4.0-preview.3
```

Adicione `DLH.Controls.Wpf` ao `paket.references` do projeto e restaure:

```powershell
paket install
```

## Início rápido

Declare o namespace da biblioteca:

```xml
<Window
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:dlh="clr-namespace:DLH.Controls.Wpf;assembly=DLH.Controls.Wpf">
</Window>
```

Use os controles diretamente no XAML:

```xml
<dlh:TabControl CornerRadius="12"
                CanReorderTabs="True">
    <dlh:TabControlItem Header="Visão geral">
        <TextBlock Margin="24" Text="Conteúdo da página" />
    </dlh:TabControlItem>
    <dlh:TabControlItem Header="Editor">
        <TextBox Margin="24" AcceptsReturn="True" />
    </dlh:TabControlItem>
</dlh:TabControl>
```

Os estilos padrão são carregados pelo sistema de temas do WPF. Não é necessário copiar os templates para a aplicação consumidora.

Consulte o [guia de início](docs/getting-started.md) e o [manual consolidado](docs/Manual.md) para instalação, MVVM, temas e exemplos completos.

## Documentação

| Documento | Conteúdo |
|---|---|
| [Central da documentação](docs/README.md) | índice de guias, controles e processos de desenvolvimento |
| [Guia de início](docs/getting-started.md) | instalação, namespace e primeiro controle |
| [Manual consolidado](docs/Manual.md) | uso completo da biblioteca |
| [Referência da API](docs/ApiReference.md) | propriedades, eventos, métodos e tipos públicos |
| [Integração e MVVM](docs/guides/integration.md) | coleções, comandos, persistência e temas |
| [Acessibilidade](docs/guides/accessibility.md) | teclado, automação e verificações assistivas |
| [Desempenho](docs/guides/performance.md) | medições e cuidados com virtualização |
| [Histórico de versões](CHANGELOG.md) | alterações publicadas e trabalho em andamento |

## Estrutura do repositório

```text
src/        biblioteca e componentes
samples/    demonstração interativa e composições de captura
tests/      testes automatizados e cenários WPF
docs/       documentação para usuários e mantenedores
eng/        validação, empacotamento e testes de consumo
```

## Desenvolvimento

Restaure, compile e valide a solução:

```powershell
dotnet restore DLH.Controls.sln
dotnet build DLH.Controls.sln -c Release
./eng/Validate.ps1
```

Abra as aplicações de demonstração:

```powershell
dotnet run --project samples/DLH.Controls.Wpf.Demo -c Release
dotnet run --project samples/DLH.Controls.Wpf.Screenshots -c Release
```

A validação executa os testes, gera o pacote e o instala em uma aplicação WPF isolada. Consulte o [guia de testes e empacotamento](docs/development/packaging.md).

## Contribuição e segurança

Relatos de defeitos e propostas são bem-vindos. Antes de contribuir, consulte:

- [Como contribuir](CONTRIBUTING.md)
- [Código de conduta](CODE_OF_CONDUCT.md)
- [Política de segurança](SECURITY.md)

Não publique credenciais, dados pessoais, código proprietário ou detalhes exploráveis de vulnerabilidades em uma issue.

## Apoie o projeto

Se o DLH Controls estiver ajudando sua aplicação, você pode apoiar sua manutenção e o desenvolvimento de novos componentes.

### Pix

O Pix é a forma principal de apoio no Brasil. Escaneie o QR Code e escolha o valor da contribuição.

![QR Code para apoiar o DLH Controls por Pix](https://raw.githubusercontent.com/LeonardoHessel/DLH.Controls/main/docs/images/pix-qrcode.png)

**Chave Pix aleatória:** `65e95283-e2bd-46c3-b045-8773e8157df8`

### GitHub Sponsors

Para contribuições internacionais, use o [perfil de patrocínio de LeonardoHessel](https://github.com/sponsors/LeonardoHessel).

[![Apoie pelo GitHub Sponsors](https://img.shields.io/badge/Apoie-GitHub%20Sponsors-EA4AAA?logo=githubsponsors&logoColor=white)](https://github.com/sponsors/LeonardoHessel)

## Licença

Distribuído sob a **DLH Controls — Licença de Uso com Atribuição Visível, versão 1.0**. Consulte [LICENSE.txt](LICENSE.txt).

Uso comercial, modificação e redistribuição são permitidos conforme suas condições. Produtos que utilizem a biblioteca devem disponibilizar o crédito:

> Este produto utiliza DLH Controls, desenvolvido por Leonardo D. de L. Hessel.

## Pacote

- [DLH.Controls.Wpf no NuGet](https://www.nuget.org/packages/DLH.Controls.Wpf)
- Versão atual: `0.4.0-preview.3`
- Plataforma: Windows
- Framework: .NET 10 / WPF

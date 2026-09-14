# Publicação por release

Cadastre no NuGet.org > usuário > Trusted Publishing uma política com:

- Dono da política / usuário NuGet: LeonardoHessel
- Repository Owner: LeonardoHessel
- Repository: DLH.Controls
- Workflow File: publish.yml (somente o nome)
- Environment: vazio (o workflow não usa environments)
- Pacote/padrão: DLH.Controls.Wpf
- Escopo: publicar novas versões do pacote existente

Em repositório privado, a política pode ficar temporariamente ativa por sete dias; se expirar antes do primeiro uso, reative-a. Referência: https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing

Após cadastrar a política, crie uma release no GitHub usando uma tag do commit atual, por exemplo v0.1.0-preview.2, e marque Pre-release. Para uma versão estável, use v1.0.0 sem marcar Pre-release. Publique a release para disparar a automação. Rascunhos não publicam pacotes.

A versão vem da tag e substitui a versão base do projeto durante build/pack, sem alterar o arquivo csproj. A rotina valida o formato, executa todas as suítes, gera e guarda o pacote e só então obtém a credencial temporária NuGet para publicar. Não há API key permanente para cadastrar. Uma versão já publicada não é sobrescrita: o envio duplicado falha explicitamente.

A política no NuGet precisa ser cadastrada pelo titular antes da primeira release. Nenhuma release é criada por esta configuração. O CI em pushes/PRs continua somente validando e gerando artefatos.

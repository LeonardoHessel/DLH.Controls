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

Após cadastrar a política, prepare a versão em `eng/Version.props`, atualize o changelog e crie uma release no GitHub usando uma tag idêntica, por exemplo `v0.4.0-preview.4`. Marque **Pre-release** quando a versão contiver um sufixo de prévia. Para uma versão estável, use uma tag como `v1.0.0` sem marcar **Pre-release**. Publique a release para disparar a automação. Rascunhos não publicam pacotes.

`eng/Version.props` é a fonte central da versão preparada no repositório e também define a versão usada nos pacotes locais. Na publicação, a tag continua sendo a entrada do fluxo e substitui as propriedades de build/pack, mas a rotina exige que as duas versões sejam iguais. Assim, uma tag incorreta falha antes de gerar ou enviar o pacote.

A rotina valida o formato e a correspondência da versão, executa todas as suítes, gera e guarda o pacote e só então obtém a credencial temporária NuGet para publicar. Não há API key permanente para cadastrar. Uma versão já publicada não é sobrescrita: o envio duplicado falha explicitamente.

A política no NuGet precisa ser cadastrada pelo titular antes da primeira release. Nenhuma release é criada por esta configuração. O CI em pushes/PRs continua somente validando e gerando artefatos.

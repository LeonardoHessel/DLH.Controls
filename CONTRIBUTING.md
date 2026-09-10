# Como contribuir

Obrigado pelo interesse em melhorar o DLH Controls.

## Relatos e propostas

- Pesquise as issues existentes antes de abrir uma nova.
- Para defeitos, descreva o comportamento observado, o comportamento esperado e um exemplo mínimo reproduzível.
- Para novos recursos, explique o cenário de uso e a API pública sugerida.
- Não inclua credenciais, dados pessoais, código proprietário ou informações confidenciais.

## Desenvolvimento

Requisitos:

- Windows;
- SDK do .NET 10;
- workload de desktop do WPF.

Valide uma alteração antes de enviar o pull request:

```powershell
dotnet restore DLH.Controls.sln
./eng/Validate.ps1
```

O pull request deve manter a compatibilidade das APIs públicas já distribuídas, incluir documentação quando alterar comportamento público e acrescentar testes somente quando eles verificarem um risco real.

## Pull requests

1. Crie um fork e uma branch com um nome descritivo.
2. Faça alterações pequenas e relacionadas ao mesmo objetivo.
3. Explique o problema, o comportamento resultante e a validação realizada.
4. Aguarde a execução dos checks no GitHub Actions.

Ao contribuir, você concorda que sua contribuição será distribuída sob a licença do repositório, disponível em `LICENSE.txt`.

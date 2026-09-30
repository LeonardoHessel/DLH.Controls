# Preparação para 1.0.0

## Homologação automatizada e assistida

Execute a validação completa, colete as características reais do Windows e gere os relatórios de prontidão com:

```powershell
./eng/Invoke-ReleaseReadiness.ps1
```

Para conferir também a versão já publicada no NuGet e conduzir as etapas que exigem interação humana:

```powershell
./eng/Invoke-ReleaseReadiness.ps1 -CheckPublicPackage -Interactive
```

O modo automático recompila, executa os testes, empacota, valida aplicações consumidoras isoladas, registra Windows, SDK, cultura, alto contraste e os monitores com resolução, DPI e escala. Com `-CheckPublicPackage`, baixa o `.nupkg` do índice público e repete a matriz de consumo usando exatamente esse arquivo.

O modo `-Interactive` abre a demonstração e conduz cinco grupos: mouse físico, DPI e múltiplos monitores, teclado e foco, leitor de tela e alto contraste. Cada resultado é registrado como aprovado, falhou ou bloqueado. Os arquivos `release-readiness.json` e `release-readiness.md` ficam em `artifacts/release-readiness`.

O workflow **Release readiness** permite executar a parte automática manualmente no GitHub Actions e guardar as evidências por 30 dias. A execução remota não substitui os cinco grupos assistidos na máquina usada para homologação.

## Revisão de API e documentação

Concluído: inventário dos membros públicos, padrões, permissões de fechamento, eventos, comportamento síncrono, contratos de estado/configuração e limites de coleções. Nomes e assinaturas publicados preservados. Documentação de integração e referência separadas.

Corrigido: README ainda afirmava que o pacote não estava publicado; guia citava 61 cenários e diretório de preferências incorreto; texto confundia persistência de organização com a de configuração. Padrões da biblioteca agora separados dos ajustes do visualizador.

## Decisões propostas para estabilidade

- Manter namespace DLH.Controls.Wpf e nomes já publicados.
- Manter apenas net10.0-windows nesta etapa; não anunciar suporte a versões não testadas.
- Preservar helpers públicos já publicados. Novos helpers devem ser internos quando possível.
- Manter formato Version=1; mudanças futuras no esquema precisam prever migração.
- Fechamento assíncrono, cache de conteúdo visual e transferência de abas entre janelas não integram o contrato atual.

## Gates ainda abertos

| Validação | Estado |
|---|---|
| Compilação Debug/Release, 144 cenários legados e testes STA independentes | Automatizados no CI |
| Compatibilidade da API pública | Contrato versionado e comparado antes do pacote |
| Consumo do pacote em aplicação independente | Automatizado com o `.nupkg` recém-gerado |
| Regressão visual do TabControl | Automatizada com tolerância e imagem de diferenças |
| Fluxo combinado do DataGridView e culturas pt-BR/en-US | Automatizado em testes STA independentes |
| Mouse físico: arraste, soltar fora, captura e Alt+Tab | Conduzido e registrado pelo modo `-Interactive` |
| Monitores com DPI 125%, 150%, 200% e troca entre monitores | Inventário automático; interação conduzida pelo modo `-Interactive` |
| Percurso Tab/Shift+Tab, setas, Ctrl+Tab, foco visível | Conduzido e registrado pelo modo `-Interactive` |
| Leitor de tela com nomes/seleção/fechamento | Conduzido e registrado pelo modo `-Interactive`; metadados têm cobertura automática parcial |
| Alto contraste com foco, ícones e estados | Estado coletado automaticamente; inspeção conduzida pelo modo `-Interactive` |
| Revisão jurídica da licença personalizada | Não foi realizada por esta revisão técnica |

Não criar release 1.0.0 automaticamente. Consolidar evidências de uso real e resolver falhas antes de declarar estabilidade. Esta revisão não certifica ausência de bugs; não é uma auditoria linha a linha de todo o código.

# Preparação para 1.0.0

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
| Mouse físico: arraste, soltar fora, captura e Alt+Tab | Pendente manual |
| Monitores com DPI 125%, 150%, 200% e troca entre monitores | Pendente manual; LayoutTransform não substitui DPI |
| Percurso Tab/Shift+Tab, setas, Ctrl+Tab, foco visível | Pendente manual completo |
| Leitor de tela com nomes/seleção/fechamento | Pendente manual; metadados têm cobertura parcial |
| Revisão jurídica da licença personalizada | Não foi realizada por esta revisão técnica |

Não criar release 1.0.0 automaticamente. Consolidar evidências de uso real e resolver falhas antes de declarar estabilidade. Esta revisão não certifica ausência de bugs; não é uma auditoria linha a linha de todo o código.

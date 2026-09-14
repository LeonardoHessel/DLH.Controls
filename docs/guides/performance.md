# Linha de base do contorno

Data: 4 de setembro de 2026  
Ambiente: Release, .NET SDK 10.0.400, Windows, Lenovo 83NS, Intel Core i5-13420H, 15,7 GB de RAM.  
Escopo: janela WPF não exibida, escala lógica 100%. É uma linha de base relativa do laboratório, não um benchmark da experiência completa na tela.

Cada cenário foi repetido três vezes, com 2, 10 e 50 abas. `idle` chama apenas a atualização do contorno; os demais números incluem o layout provocado pela ação. A coluna “geometrias” conta substituições do objeto desenhado, não chamadas ao evento.

| Cenário | Operações | Resultado observado |
|---|---:|---|
| Ocioso | 1.000 | 0 geometrias; 6,3–9,1 ms conforme a quantidade de abas |
| Redimensionar | 30 | 29 geometrias; 11,8–25,0 ms |
| Alternar fonte | 30 | 29 geometrias; 23,6–176,7 ms; o custo cresce com o layout de 50 cabeçalhos |
| Rolar cabeçalhos | 30 | 0 geometrias com 2 abas, 29 com overflow; 1,9–7,4 ms |
| Atualizar durante arraste | 30 | 0 geometrias enquanto a aba selecionada usa a prévia; 0,9–2,8 ms |

## Decisão

Manter `LayoutUpdated` e o cache `lastShape` nesta etapa. A atualização ociosa não reconstruiu nenhuma geometria em 9.000 chamadas acumuladas nas três repetições, e o tempo não cresceu de forma material com 2, 10 ou 50 abas. Os cenários que mudam medidas reconstruíram o contorno como esperado.

Foram observados cerca de 576 KB em 1.000 verificações ociosas, atribuídos às consultas de coordenadas e template necessárias para confirmar que a forma não mudou. Uma experiência que apenas moveu a função local responsável pelos limites não alterou esse número e foi descartada. Evitar essas consultas exigiria substituir o mecanismo abrangente por invalidações específicas; os dados atuais não justificam esse risco.

O maior custo apareceu ao relayoutar 50 cabeçalhos após mudança de fonte; essa medida inclui o layout WPF inteiro e não demonstra um gargalo no desenho do contorno. Trocar o mecanismo por uma lista de eventos específicos aumentaria o risco de perder alterações de template, fonte, DPI ou rolagem sem evidência de ganho.

O comando reproduzível é:

```powershell
dotnet run --project tests/DLH.Controls.Wpf.Tests -c Release -- --performance-only --report artifacts/test-results/performance.json
```

Escalas reais, janela visível e ferramentas de profiling continuam como validação manual se surgir um relato de desempenho.

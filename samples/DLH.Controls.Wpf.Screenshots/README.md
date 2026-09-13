# Aplicativo de capturas

Este projeto mantém as composições usadas nas imagens da documentação separadas da demonstração interativa.

Abra o seletor de componentes:

```powershell
dotnet run --project samples/DLH.Controls.Wpf.Screenshots -c Release
```

Para abrir uma composição diretamente, informe `tabcontrol`, `datagridview`, `scrollbar` ou `contextmenu`:

```powershell
dotnet run --project samples/DLH.Controls.Wpf.Screenshots -c Release -- datagridview
```

As imagens finais ficam em `docs/images`. Preserve o tamanho original da área da janela para que novas capturas mantenham dimensões consistentes.

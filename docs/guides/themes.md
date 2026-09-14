# Temas e recursos

As propriedades convencionais `Background`, `Foreground`, `BorderBrush`, `BorderThickness`, `Padding`, `FontFamily` e `FontSize` continuam disponíveis.

Recursos principais do `TabControl`:

```xml
<SolidColorBrush x:Key="Tabs.Surface" Color="#35373C" />
<SolidColorBrush x:Key="Tabs.Hover" Color="#454850" />
<SolidColorBrush x:Key="Tabs.Text" Color="#F2F3F5" />
<SolidColorBrush x:Key="Tabs.Muted" Color="#BCC0CA" />
<SolidColorBrush x:Key="Tabs.Edge" Color="#4C5058" />
<SolidColorBrush x:Key="Tabs.Focus" Color="#9CC9FF" />
```

Recursos principais do `DataGridView`:

```xml
<SolidColorBrush x:Key="DataGridView.Surface" Color="#35373C" />
<SolidColorBrush x:Key="DataGridView.Header" Color="#2F3136" />
<SolidColorBrush x:Key="DataGridView.Hover" Color="#454850" />
<SolidColorBrush x:Key="DataGridView.Selection" Color="#334F8AC9" />
<SolidColorBrush x:Key="DataGridView.Text" Color="#F2F3F5" />
<SolidColorBrush x:Key="DataGridView.Edge" Color="#4C5058" />
<SolidColorBrush x:Key="DataGridView.Focus" Color="#9CC9FF" />
```

Como são `DynamicResource`, esses valores podem ser substituídos durante a execução para alternar temas.

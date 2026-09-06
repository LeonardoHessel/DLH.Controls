using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tabs = DLH.Controls.Wpf.CustomTabControl;

namespace DLH.Controls.Wpf.Demo;

public sealed class TabSettingsWindow : Window
{
    private readonly List<(DependencyProperty Property, FrameworkElement Editor)> editors = [];
    private readonly Tabs[] targets;
    private readonly ComboBox scope = new() { ItemsSource = new[] { "Todos os modelos", "Documentos", "Lateral", "Simples" }, SelectedIndex = 0 };
    private readonly ComboBox placement = new() { ItemsSource = new[] { "Manter posição de cada modelo", "Superior", "Inferior", "Esquerda", "Direita" }, SelectedIndex = 0 };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };

    public TabSettingsWindow(Tabs[] controls)
    {
        targets = controls;
        Title = "Configurações das abas"; Width = 520; Height = 740;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "Aplicar configurações em", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(scope);
        panel.Children.Add(new TextBlock { Text = "Os valores iniciais são do modelo Documentos. Use vírgula para decimais; duração no formato 00:00:00.180. Recuo aceita NaN (automático).", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10) });
        Add("Posição das abas", placement);
        Field("Raio de todas as curvas", Tabs.CornerRadiusProperty);
        Field("Espaço entre abas", Tabs.TabSpacingProperty);
        Field("Recuo da primeira aba", Tabs.HeaderIndentProperty);
        Field("Espaço interno do corpo", Control.PaddingProperty);
        Field("Espessura da borda", Control.BorderThicknessProperty);
        Field("Tamanho da fonte", Control.FontSizeProperty);
        Field("Fonte", Control.FontFamilyProperty);
        Field("Cor do corpo", Control.BackgroundProperty);
        Field("Cor do texto", Control.ForegroundProperty);
        Field("Cor da borda", Control.BorderBrushProperty);
        Field("Sombra habilitada", Tabs.IsShadowEnabledProperty);
        Field("Cor da sombra (#AARRGGBB)", Tabs.ShadowColorProperty);
        Field("Opacidade da sombra (0 a 1)", Tabs.ShadowOpacityProperty);
        Field("Desfoque da sombra", Tabs.ShadowBlurRadiusProperty);
        Field("Distância da sombra", Tabs.ShadowDepthProperty);
        Field("Direção da sombra (graus)", Tabs.ShadowDirectionProperty);
        Field("Permitir reorganização", Tabs.CanReorderTabsProperty);
        Field("Mostrar prévia do arraste", Tabs.IsDragPreviewEnabledProperty);
        Field("Animar movimentação", Tabs.IsAnimationEnabledProperty);
        Field("Duração da animação", Tabs.DragAnimationDurationProperty);
        Field("Opacidade da prévia (0 a 1)", Tabs.DragPreviewOpacityProperty);
        Field("Distância mínima do arraste (px)", Tabs.MinimumDragDistanceProperty);
        var cursor = new ComboBox { ItemsSource = new[] { "ScrollWE", "ScrollNS", "Hand", "Arrow", "SizeAll", "SizeWE", "SizeNS" }, Text = controls[0].TabDragCursor.ToString(), IsEditable = false };
        cursor.SelectedItem = controls[0].TabDragCursor.ToString();
        editors.Add((Tabs.TabDragCursorProperty, cursor)); Add("Cursor durante o arraste", cursor);
        Field("Permitir exclusão de abas", Tabs.CanCloseTabsProperty);
        Field("Mostrar botão de fechar", Tabs.ShowCloseButtonsProperty);
        Field("Permitir adição de abas", Tabs.CanAddTabsProperty);
        Field("Permitir renomear abas", Tabs.CanRenameTabsProperty);
        Field("Propriedade do título editável", Tabs.TabHeaderPathProperty);
        Field("Ativação da renomeação", Tabs.RenameActivationProperty);
        var apply = new Button { Content = "Aplicar configurações", Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 16, 0, 0) };
        apply.Click += (_, _) => Apply(); panel.Children.Add(apply);
        var reset = new Button { Content = "Restaurar padrões", Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(12, 8, 12, 8) };
        reset.Click += (_, _) =>
        {
            foreach (var target in scope.SelectedIndex == 0 ? targets : new[] { targets[scope.SelectedIndex - 1] })
            {
                target.ResetConfiguration();
                target.SetResourceReference(Control.BackgroundProperty, "Tabs.Surface");
                target.SetResourceReference(Control.ForegroundProperty, "Tabs.Text");
                target.SetResourceReference(Control.BorderBrushProperty, "Tabs.Edge");
            }
            RefreshEditors(); status.Text = "Padrões restaurados. Use Salvar configurações na janela principal para guardar.";
        };
        panel.Children.Add(reset); panel.Children.Add(status);
        scope.SelectionChanged += (_, _) => RefreshEditors();
        void Add(string label, FrameworkElement editor)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 10, 0, 4) });
            panel.Children.Add(editor);
        }
        void Field(string label, DependencyProperty property)
        {
            var value = controls[0].GetValue(property);
            FrameworkElement editor = property.PropertyType == typeof(bool)
                ? new CheckBox { IsChecked = (bool)value }
                : new TextBox { Text = TypeDescriptor.GetConverter(property.PropertyType).ConvertToString(null, CultureInfo.CurrentCulture, value), Padding = new Thickness(5) };
            editors.Add((property, editor)); Add(label, editor);
        }
    }

    private void RefreshEditors()
    {
        var source = targets[Math.Max(0, scope.SelectedIndex - 1)];
        foreach (var (property, editor) in editors)
        {
            var value = source.GetValue(property);
            if (editor is CheckBox check) check.IsChecked = (bool)value;
            else if (editor is TextBox text) text.Text = TypeDescriptor.GetConverter(property.PropertyType).ConvertToString(null, CultureInfo.CurrentCulture, value);
            else ((ComboBox)editor).SelectedItem = value.ToString();
        }
        placement.SelectedIndex = 0;
    }
    private void Apply()
    {
        var values = new List<(DependencyProperty Property, object? Value)>();
        foreach (var (property, editor) in editors)
        {
            try
            {
                object? value = editor is CheckBox check ? check.IsChecked == true
                    : property.PropertyType == typeof(string) && editor is TextBox nullableText && string.IsNullOrWhiteSpace(nullableText.Text) ? null
                    : TypeDescriptor.GetConverter(property.PropertyType).ConvertFromString(null, CultureInfo.CurrentCulture,
                        editor is TextBox text ? text.Text : ((ComboBox)editor).SelectedItem?.ToString() ?? "Arrow");
                if (!property.IsValidValue(value)) throw new ArgumentException("Valor fora do intervalo permitido.");
                values.Add((property, value));
            }
            catch (Exception error) when (error is ArgumentException or FormatException or NotSupportedException)
            { status.Text = $"Confira {property.Name}: {error.Message}"; return; }
        }
        var selected = scope.SelectedIndex == 0 ? targets : new[] { targets[scope.SelectedIndex - 1] };
        foreach (var target in selected)
        {
            foreach (var (property, value) in values) target.SetCurrentValue(property, value);
            if (placement.SelectedIndex > 0) target.TabStripPlacement = new[] { Dock.Top, Dock.Bottom, Dock.Left, Dock.Right }[placement.SelectedIndex - 1];
        }
        status.Text = $"Configurações aplicadas a {selected.Length} modelo(s).";
    }
}

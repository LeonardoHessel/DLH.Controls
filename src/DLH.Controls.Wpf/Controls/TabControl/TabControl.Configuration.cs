using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace DLH.Controls.Wpf;

/// <summary>Portable configuration data. Storage and theme selection belong to the host application.</summary>
public sealed class TabControlConfiguration
{
    public int Version { get; set; } = 1;
    public Dictionary<string, string?> Values { get; set; } = new();
}

public partial class TabControl
{
    private static DependencyProperty[] ConfigurationProperties =>
    [
        CornerRadiusProperty, TabSpacingProperty, HeaderIndentProperty, TabStripPlacementProperty,
        PaddingProperty, BorderThicknessProperty, FontSizeProperty, FontFamilyProperty,
        BackgroundProperty, ForegroundProperty, BorderBrushProperty,
        IsShadowEnabledProperty, ShadowColorProperty, ShadowOpacityProperty, ShadowBlurRadiusProperty,
        ShadowDepthProperty, ShadowDirectionProperty, CanReorderTabsProperty, TabDragCursorProperty,
        IsDragPreviewEnabledProperty, IsAnimationEnabledProperty, DragAnimationDurationProperty,
        DragPreviewOpacityProperty, MinimumDragDistanceProperty, CanCloseTabsProperty, ShowCloseButtonsProperty,
        CanAddTabsProperty, CanRenameTabsProperty, TabHeaderPathProperty, RenameActivationProperty
    ];

    public TabControlConfiguration CaptureConfiguration()
    {
        var configuration = new TabControlConfiguration();
        foreach (var property in ConfigurationProperties)
        {
            var value = GetValue(property);
            try
            {
                configuration.Values[property.Name] = value is null ? null :
                    TypeDescriptor.GetConverter(property.PropertyType).ConvertToInvariantString(value);
            }
            catch (Exception error) when (error is NotSupportedException or ArgumentException)
            { throw new InvalidOperationException($"A configuração {property.Name} não pode ser serializada. Use um valor suportado pelo conversor WPF.", error); }
        }
        return configuration;
    }

    private static List<(DependencyProperty Property, object? Value)> ParseConfiguration(TabControlConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.Version != 1 || configuration.Values is null)
            throw new ArgumentException("Versão ou configurações inválidas.", nameof(configuration));
        var result = new List<(DependencyProperty, object?)>();
        foreach (var entry in configuration.Values)
        {
            var property = ConfigurationProperties.FirstOrDefault(property => property.Name == entry.Key)
                ?? throw new ArgumentException($"Configuração desconhecida: {entry.Key}.");
            object? value;
            try { value = entry.Value is null ? null : TypeDescriptor.GetConverter(property.PropertyType).ConvertFromInvariantString(entry.Value); }
            catch (Exception error) when (error is FormatException or NotSupportedException or ArgumentException or OverflowException)
            { throw new ArgumentException($"Configuração inválida: {entry.Key}.", error); }
            if (!property.IsValidValue(value)) throw new ArgumentException($"Valor inválido: {entry.Key}.");
            result.Add((property, value));
        }
        return result;
    }

    public static void ValidateConfiguration(TabControlConfiguration configuration) => ParseConfiguration(configuration);

    /// <summary>Validates all values before changing the control. Preserves existing bindings.</summary>
    public void RestoreConfiguration(TabControlConfiguration configuration)
    {
        var values = ParseConfiguration(configuration);
        CancelTabDrag(); ResetDragPreview();
        foreach (var (property, value) in values) SetCurrentValue(property, value);
    }

    /// <summary>Resets configurable properties to their metadata defaults, preserving bindings.</summary>
    public void ResetConfiguration()
    {
        CancelTabDrag(); ResetDragPreview();
        foreach (var property in ConfigurationProperties)
            SetCurrentValue(property, property.GetMetadata(GetType()).DefaultValue);
    }
}


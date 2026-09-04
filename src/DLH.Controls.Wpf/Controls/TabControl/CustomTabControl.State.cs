using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace DLH.Controls.Wpf;

/// <summary>Only layout and selection; content and document creation remain application-owned.</summary>
public sealed class TabControlState
{
    public int Version { get; set; } = 1;
    public List<string> Order { get; set; } = new();
    public string? SelectedKey { get; set; }
}

public partial class CustomTabControl
{
    public static readonly DependencyProperty ItemKeyPathProperty = DependencyProperty.Register(
        nameof(ItemKeyPath), typeof(string), typeof(CustomTabControl), new PropertyMetadata("Id"), value => value is string path && !string.IsNullOrWhiteSpace(path));
    public string ItemKeyPath { get => (string)GetValue(ItemKeyPathProperty); set => SetValue(ItemKeyPathProperty, value); }
    public static readonly DependencyProperty TabKeyProperty = DependencyProperty.RegisterAttached(
        "TabKey", typeof(string), typeof(CustomTabControl), new PropertyMetadata(null));
    public static string? GetTabKey(DependencyObject item) => (string?)item.GetValue(TabKeyProperty);
    public static void SetTabKey(DependencyObject item, string? value) => item.SetValue(TabKeyProperty, value);

    private List<(string Key, object Item)> KeyedItems(Func<object, string>? keySelector)
    {
        var result = new List<(string, object)>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Items.Cast<object>())
        {
            var key = keySelector?.Invoke(item);
            if (keySelector is null)
            {
                if (item is DependencyObject element) key = GetTabKey(element);
                if (key is null)
                {
                    object? value = item;
                    foreach (var member in ItemKeyPath.Split('.'))
                        value = value is null ? null : TypeDescriptor.GetProperties(value)[member]?.GetValue(value);
                    key = value as string;
                }
            }
            if (string.IsNullOrWhiteSpace(key) || !used.Add(key))
                throw new InvalidOperationException("Cada aba deve ter uma chave de texto estável, não vazia e única (ItemKeyPath, TabKey ou keySelector).");
            result.Add((key, item));
        }
        return result;
    }

    public TabControlState CaptureState(Func<object, string>? keySelector = null)
    {
        var items = KeyedItems(keySelector);
        return new TabControlState { Order = items.Select(pair => pair.Key).ToList(),
            SelectedKey = items.FirstOrDefault(pair => ReferenceEquals(pair.Item, SelectedItem) || Equals(pair.Item, SelectedItem)).Key };
    }

    public void RestoreState(TabControlState state, Func<object, string>? keySelector = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Version != 1 || state.Order is null || state.Order.Any(string.IsNullOrWhiteSpace) ||
            state.Order.Distinct(StringComparer.Ordinal).Count() != state.Order.Count)
            throw new ArgumentException("Estado inválido ou versão não suportada.", nameof(state));
        var items = KeyedItems(keySelector);
        var list = EditableList() ?? throw new InvalidOperationException("A origem precisa ser uma lista mutável sem ordenação, filtro ou agrupamento.");
        var byKey = items.ToDictionary(pair => pair.Key, pair => pair.Item, StringComparer.Ordinal);
        var knownKeys = state.Order.Where(byKey.ContainsKey).ToHashSet(StringComparer.Ordinal);
        var desired = state.Order.Where(byKey.ContainsKey).Select(key => byKey[key])
            .Concat(items.Where(pair => !knownKeys.Contains(pair.Key)).Select(pair => pair.Item)).ToArray();
        CancelTabDrag(); ResetDragPreview();
        var selected = SelectedItem;
        for (var index = 0; index < desired.Length; index++)
        {
            var current = list.IndexOf(desired[index]);
            if (current != index) MoveListItem(list, current, index);
        }
        if (state.SelectedKey is null) selected = null;
        else if (byKey.TryGetValue(state.SelectedKey, out var restored) &&
            ItemContainerGenerator.ContainerFromItem(restored) is not TabItem { IsEnabled: false }) selected = restored;
        SetCurrentValue(SelectedItemProperty, selected);
        RevealSelectedTab();
        StateRestored?.Invoke(this, EventArgs.Empty);
    }

    public void SaveState(Stream destination, Func<object, string>? keySelector = null) =>
        JsonSerializer.Serialize(destination, CaptureState(keySelector), new JsonSerializerOptions { WriteIndented = true });

    public void LoadState(Stream source, Func<object, string>? keySelector = null) =>
        RestoreState(JsonSerializer.Deserialize<TabControlState>(source) ?? throw new InvalidDataException("Estado vazio."), keySelector);
}

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DotPudica.Core.ViewModels;
using Godot;

namespace Samples.InventoryMvvmTest;

public partial class InventoryTestViewModel : ViewModelBase
{
    private readonly Random _random = new();
    private int _nextItemId = 1;

    public int GridColumns => 10;
    public int GridRows => 16;
    public float ItemFontScale => 0.34f;
    public int ItemFontMinSize => 9;
    public int ItemFontMaxSize => 34;

    [ObservableProperty]
    private int _viewACellSize = 42;

    [ObservableProperty]
    private int _viewBCellSize = 30;

    [ObservableProperty]
    private bool _syncCellSize;

    [ObservableProperty]
    private int _revision;

    public ObservableCollection<InventoryItemData> Items { get; } = [];

    public InventoryTestViewModel()
    {
        AddItemFixed("AK-74M", 0, 0, 4, 2, new Color("4D7A91"));
        AddItemFixed("6B13", 4, 0, 2, 3, new Color("6E6E4D"));
        AddItemFixed("Salewa", 6, 0, 2, 2, new Color("905E4A"));
    }

    public bool TryMoveItem(int itemId, int x, int y)
    {
        var item = Items.FirstOrDefault(it => it.Id == itemId);
        if (item == null)
            return false;

        if (!CanPlaceItem(itemId, x, y, item.Width, item.Height))
            return false;

        item.X = x;
        item.Y = y;
        Revision++;
        return true;
    }

    public bool CanPlaceItem(int itemId, int x, int y, int width, int height)
    {
        if (x < 0 || y < 0)
            return false;

        if (x + width > GridColumns || y + height > GridRows)
            return false;

        foreach (var other in Items)
        {
            if (other.Id == itemId)
                continue;

            var overlap = x < other.X + other.Width
                          && x + width > other.X
                          && y < other.Y + other.Height
                          && y + height > other.Y;
            if (overlap)
                return false;
        }

        return true;
    }

    public bool AddRandomItem()
    {
        var templates = new (string Name, int Width, int Height)[]
        {
            ("7.62x39", 1, 1),
            ("CMS", 2, 1),
            ("M4A1", 4, 2),
            ("Bandage", 1, 1),
            ("Grizzly", 2, 2),
            ("Helmet", 2, 2),
            ("Mag", 1, 2),
            ("Armor", 3, 3),
            ("Water", 1, 2)
        };

        for (var attempt = 0; attempt < 120; attempt++)
        {
            var tpl = templates[_random.Next(templates.Length)];
            var x = _random.Next(0, GridColumns - tpl.Width + 1);
            var y = _random.Next(0, GridRows - tpl.Height + 1);

            if (!CanPlaceItem(-1, x, y, tpl.Width, tpl.Height))
                continue;

            var color = Color.FromHsv((float)_random.NextDouble(), 0.45f, 0.95f);
            Items.Add(new InventoryItemData(
                _nextItemId++,
                tpl.Name,
                x,
                y,
                tpl.Width,
                tpl.Height,
                color));
            Revision++;
            return true;
        }

        return false;
    }

    public bool RemoveItem(int itemId)
    {
        var item = Items.FirstOrDefault(it => it.Id == itemId);
        if (item == null)
            return false;

        Items.Remove(item);
        Revision++;
        return true;
    }

    public int CalculateItemFontSize(int cellSize, InventoryItemData item)
    {
        var baseSize = (int)MathF.Round(MathF.Min(item.Width, item.Height) * cellSize * ItemFontScale);
        return Mathf.Clamp(baseSize, ItemFontMinSize, ItemFontMaxSize);
    }

    partial void OnSyncCellSizeChanged(bool value)
    {
        if (value)
            ViewBCellSize = ViewACellSize;
    }

    partial void OnViewACellSizeChanged(int value)
    {
        if (SyncCellSize && ViewBCellSize != value)
            ViewBCellSize = value;
    }

    partial void OnViewBCellSizeChanged(int value)
    {
        if (SyncCellSize && ViewACellSize != value)
            ViewACellSize = value;
    }

    private void AddItemFixed(string name, int x, int y, int width, int height, Color color)
    {
        Items.Add(new InventoryItemData(
            _nextItemId++,
            name,
            x,
            y,
            width,
            height,
            color));
    }
}

public sealed class InventoryItemData(
    int id,
    string name,
    int x,
    int y,
    int width,
    int height,
    Color color)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public Color Color { get; } = color;
}

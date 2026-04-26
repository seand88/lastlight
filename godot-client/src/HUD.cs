using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using LastLight.Common;

public partial class HUD : CanvasLayer
{
    private Label _statsLabel = null!;
    private ProgressBar _hpBar = null!;
    
    // Leaderboard
    private VBoxContainer _leaderboardBox = null!;
    
    // Inventory
    private VBoxContainer _bottomVBox = null!;
    private GridContainer _equipmentGrid = null!;
    private GridContainer _inventoryGrid = null!;
    
    // Minimap
    private Minimap _minimap = null!;

    private int _selectedSlotIndex = -1;

    [Signal] public delegate void SwapItemRequestedEventHandler(int fromIndex, int toIndex);
    [Signal] public delegate void UseItemRequestedEventHandler(int slotIndex);

    public override void _Ready()
    {
        // 1. Top Left - Stats & HP
        var statsPanel = new Panel();
        statsPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        statsPanel.Size = new Godot.Vector2(250, 120);
        statsPanel.Position = new Godot.Vector2(10, 10);
        AddChild(statsPanel);

        _statsLabel = new Label();
        _statsLabel.Position = new Godot.Vector2(10, 10);
        statsPanel.AddChild(_statsLabel);

        _hpBar = new ProgressBar();
        _hpBar.Position = new Godot.Vector2(10, 90);
        _hpBar.Size = new Godot.Vector2(230, 20);
        _hpBar.ShowPercentage = true;
        statsPanel.AddChild(_hpBar);

        // 2. Top Right - Minimap & Leaderboard
        var rightVBox = new VBoxContainer();
        rightVBox.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        rightVBox.OffsetLeft = -220;
        rightVBox.OffsetTop = 10;
        rightVBox.OffsetRight = -20;
        rightVBox.OffsetBottom = 510;
        AddChild(rightVBox);

        _minimap = new Minimap();
        _minimap.CustomMinimumSize = new Godot.Vector2(200, 200);
        rightVBox.AddChild(_minimap);

        var lbPanel = new PanelContainer();
        lbPanel.CustomMinimumSize = new Godot.Vector2(200, 150);
        rightVBox.AddChild(lbPanel);

        _leaderboardBox = new VBoxContainer();
        lbPanel.AddChild(_leaderboardBox);

        // 3. Bottom Center - Inventory & Equipment
        _bottomVBox = new VBoxContainer();
        _bottomVBox.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _bottomVBox.OffsetTop = -160;
        _bottomVBox.OffsetBottom = -10;
        _bottomVBox.Alignment = BoxContainer.AlignmentMode.Center;
        _bottomVBox.Visible = false; // Hidden by default
        AddChild(_bottomVBox);

        _equipmentGrid = new GridContainer();
        _equipmentGrid.Columns = 3;
        _bottomVBox.AddChild(_equipmentGrid);

        _inventoryGrid = new GridContainer();
        _inventoryGrid.Columns = 4;
        _bottomVBox.AddChild(_inventoryGrid);

        // Initialize slots
        for (int i = 0; i < 3; i++) {
            var slot = CreateSlot(i);
            _equipmentGrid.AddChild(slot);
        }
        for (int i = 0; i < 8; i++) {
            var slot = CreateSlot(i + 3);
            _inventoryGrid.AddChild(slot);
        }

        // Inventory Toggle Button
        var invBtn = new Button();
        invBtn.Text = "Inventory (Tab)";
        invBtn.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        invBtn.OffsetTop = -50;
        invBtn.OffsetLeft = 10;
        invBtn.OffsetRight = 150;
        invBtn.OffsetBottom = -10;
        invBtn.Pressed += () => _bottomVBox.Visible = !_bottomVBox.Visible;
        AddChild(invBtn);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Tab)
            {
                _bottomVBox.Visible = !_bottomVBox.Visible;
            }
        }
    }

    private Control CreateSlot(int slotIndex)
    {
        var panel = new Panel();
        panel.CustomMinimumSize = new Godot.Vector2(40, 40);
        
        var tex = new TextureRect();
        tex.Name = "Icon";
        tex.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        tex.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        tex.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        tex.CustomMinimumSize = new Godot.Vector2(32, 32);
        panel.AddChild(tex);

        var btn = new Button();
        btn.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        btn.Flat = true;
        btn.GuiInput += (e) => OnSlotGuiInput(e, slotIndex);
        panel.AddChild(btn);

        return panel;
    }

    private void OnSlotGuiInput(InputEvent @event, int slotIndex)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (_selectedSlotIndex == -1) {
                    _selectedSlotIndex = slotIndex;
                    HighlightSlot(slotIndex, true);
                } else {
                    if (_selectedSlotIndex != slotIndex) {
                        EmitSignal(SignalName.SwapItemRequested, _selectedSlotIndex, slotIndex);
                    }
                    HighlightSlot(_selectedSlotIndex, false);
                    _selectedSlotIndex = -1;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                EmitSignal(SignalName.UseItemRequested, slotIndex);
                if (_selectedSlotIndex != -1) {
                    HighlightSlot(_selectedSlotIndex, false);
                    _selectedSlotIndex = -1;
                }
            }
        }
    }

    private void HighlightSlot(int index, bool highlight)
    {
        Control? slot = index < 3 ? _equipmentGrid.GetChildOrNull<Control>(index) : _inventoryGrid.GetChildOrNull<Control>(index - 3);
        if (slot is Panel p) {
            p.SelfModulate = highlight ? Colors.Yellow : Colors.White;
        }
    }

    public void UpdateStatus(int health, int maxHealth, int level, int attack, int defense, int speed, int dex, int vit, int wis)
    {
        _statsLabel.Text = $"Level: {level}\nHP: {health}/{maxHealth}\nAtt: {attack} Def: {defense}\nSpd: {speed} Dex: {dex}\nVit: {vit} Wis: {wis}";
        _hpBar.MaxValue = maxHealth;
        _hpBar.Value = health;
    }

    public void UpdateInventory(ItemInfo[] equipment, ItemInfo[] inventory)
    {
        for (int i = 0; i < 3; i++) {
            var slot = _equipmentGrid.GetChild<Control>(i);
            var tex = slot.GetNode<TextureRect>("Icon");
            if (equipment != null && i < equipment.Length && equipment[i].ItemId != 0) {
                tex.Texture = GetItemTexture(equipment[i]);
            } else {
                tex.Texture = null;
            }
        }

        for (int i = 0; i < 8; i++) {
            var slot = _inventoryGrid.GetChild<Control>(i);
            var tex = slot.GetNode<TextureRect>("Icon");
            if (inventory != null && i < inventory.Length && inventory[i].ItemId != 0) {
                tex.Texture = GetItemTexture(inventory[i]);
            } else {
                tex.Texture = null;
            }
        }
    }

    private Texture2D GetItemTexture(ItemInfo item)
    {
        if (item.Category == ItemCategory.Weapon) return GD.Load<Texture2D>("res://assets/weapon_upgrade.png");
        if (item.Category == ItemCategory.Consumable) return GD.Load<Texture2D>("res://assets/potion.png");
        return null;
    }

    public void UpdateLeaderboard(LeaderboardEntry[] entries)
    {
        // Clear existing
        foreach (var child in _leaderboardBox.GetChildren()) {
            child.QueueFree();
        }

        var title = new Label { Text = "Leaderboard", HorizontalAlignment = HorizontalAlignment.Center };
        _leaderboardBox.AddChild(title);

        if (entries == null) return;
        
        for (int i = 0; i < Math.Min(5, entries.Length); i++)
        {
            var entry = entries[i];
            var lbl = new Label { Text = $"{i+1}. {entry.Username} - {entry.Score}" };
            if (i == 0) lbl.Modulate = Colors.Gold;
            else if (i == 1) lbl.Modulate = Colors.Silver;
            else if (i == 2) lbl.Modulate = Colors.DarkOrange;
            _leaderboardBox.AddChild(lbl);
        }
    }

    public void UpdateMinimap(Godot.Vector2 localPos, Dictionary<int, Player> players, Dictionary<int, Enemy> enemies, Dictionary<int, Spawner> spawners, Dictionary<int, Portal> portals, WorldManager worldManager)
    {
        _minimap.UpdateData(localPos, players, enemies, spawners, portals, worldManager);
    }
}

public partial class Minimap : Control
{
    private Godot.Vector2 _localPos;
    private Dictionary<int, Player> _players = new();
    private Dictionary<int, Enemy> _enemies = new();
    private Dictionary<int, Spawner> _spawners = new();
    private Dictionary<int, Portal> _portals = new();
    private WorldManager _worldManager;

    public void UpdateData(Godot.Vector2 localPos, Dictionary<int, Player> players, Dictionary<int, Enemy> enemies, Dictionary<int, Spawner> spawners, Dictionary<int, Portal> portals, WorldManager worldManager)
    {
        _localPos = localPos;
        _players = players;
        _enemies = enemies;
        _spawners = spawners;
        _portals = portals;
        _worldManager = worldManager;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Draw background
        DrawRect(new Rect2(Godot.Vector2.Zero, Size), new Color(0, 0, 0, 0.5f));

        if (_worldManager == null || _worldManager.Tiles == null) return;

        float ms = Size.X;
        float mx = 0;
        float my = 0;

        // Draw tiles (simplified for performance, maybe draw every N tiles or just key features)
        int step = Math.Max(1, _worldManager.Width / 50); // don't draw 300x300 dots
        for (int x = 0; x < _worldManager.Width; x+=step) {
            for (int y = 0; y < _worldManager.Height; y+=step) {
                Color c = _worldManager.Tiles[x, y] switch { 
                    TileType.Wall => Colors.Gray, 
                    TileType.Water => Colors.Blue, 
                    TileType.Sand => Colors.SandyBrown, 
                    _ => Colors.Transparent 
                };
                if (c != Colors.Transparent) {
                    float px = mx + (x * ms / _worldManager.Width);
                    float py = my + (y * ms / _worldManager.Height);
                    DrawRect(new Rect2(px, py, 2, 2), c * 0.5f);
                }
            }
        }

        void DrawDot(Godot.Vector2 worldPos, Color color, float size = 3f) {
            float px = mx + (worldPos.X / 32f * ms / _worldManager.Width);
            float py = my + (worldPos.Y / 32f * ms / _worldManager.Height);
            DrawRect(new Rect2(px - size/2, py - size/2, size, size), color);
        }

        // Draw Entities
        foreach (var p in _portals.Values) DrawDot(p.GlobalPosition, Colors.MediumPurple, 6);
        foreach (var s in _spawners.Values) DrawDot(s.GlobalPosition, Colors.Orange, 5);
        foreach (var e in _enemies.Values) DrawDot(e.GlobalPosition, Colors.Red, 3);
        foreach (var p in _players.Values) {
            if (p.IsLocal) DrawDot(p.GlobalPosition, Colors.White, 5);
            else DrawDot(p.GlobalPosition, Colors.Cyan, 4);
        }
    }
}

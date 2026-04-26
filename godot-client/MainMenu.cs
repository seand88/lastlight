using Godot;
using System;

public partial class MainMenu : CanvasLayer
{
    [Signal] public delegate void PlayRequestedEventHandler(string ip, string username);

    private LineEdit _usernameInput = null!;
    private VBoxContainer _menuBox = null!;
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        var panel = new Panel();
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.SelfModulate = new Color(0, 0, 0, 0.8f);
        AddChild(panel);

        _menuBox = new VBoxContainer();
        _menuBox.SetAnchorsPreset(Control.LayoutPreset.Center);
        _menuBox.CustomMinimumSize = new Godot.Vector2(300, 200);
        panel.AddChild(_menuBox);

        var title = new Label();
        title.Text = "LastLight (Godot Client)";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 24);
        _menuBox.AddChild(title);

        _menuBox.AddChild(new Control { CustomMinimumSize = new Godot.Vector2(0, 20) });

        var userLabel = new Label { Text = "Username:" };
        _menuBox.AddChild(userLabel);

        _usernameInput = new LineEdit();
        _usernameInput.Text = "GodotUser_" + GD.Randi() % 1000;
        _menuBox.AddChild(_usernameInput);

        _menuBox.AddChild(new Control { CustomMinimumSize = new Godot.Vector2(0, 20) });

        var btnLocal = new Button { Text = "Play Local (127.0.0.1)" };
        btnLocal.Pressed += () => EmitSignal(SignalName.PlayRequested, "127.0.0.1", _usernameInput.Text);
        _menuBox.AddChild(btnLocal);

        var btnRemote = new Button { Text = "Play Remote (169.155.55.157)" };
        btnRemote.Pressed += () => EmitSignal(SignalName.PlayRequested, "169.155.55.157", _usernameInput.Text);
        _menuBox.AddChild(btnRemote);

        _statusLabel = new Label();
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.Modulate = Colors.Red;
        _menuBox.AddChild(_statusLabel);
    }

    public void ShowError(string error)
    {
        _statusLabel.Text = error;
        Visible = true;
    }
}

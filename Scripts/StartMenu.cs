using Godot;

[Tool]
public partial class StartMenu : Control
{
	[Export] public PackedScene GameScene;
	[Export] public float InnerRingRadius = 260f;
	[Export] public float OuterRingRadius = 330f;
	[Export] public float RingThickness = 2.5f;
	[Export] public Color RingColor = new(0.08f, 0.08f, 0.08f, 1f);

	private Control _rings;
	private bool _signalsConnected;

	public override void _Ready()
	{
		_rings = GetNodeOrNull<Control>("Rings");
		if (_rings != null)
			_rings.Draw += OnRingsDraw;

		if (Engine.IsEditorHint())
		{
			CallDeferred(MethodName.RedrawRings);
			return;
		}

		Input.MouseMode = Input.MouseModeEnum.Visible;
		ConnectButtonSignals();
		CallDeferred(MethodName.RedrawRings);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			RedrawRings();
	}

	private void ConnectButtonSignals()
	{
		if (_signalsConnected)
			return;
		_signalsConnected = true;

		GetNode<Button>("StartButton").Pressed += OnStartPressed;
		GetNode<Button>("ArchiveButton/Content/Hit").Pressed += OnArchivePressed;
		GetNode<Button>("CreditsButton/Content/Hit").Pressed += OnCreditsPressed;
		GetNode<Button>("SettingsButton/Content/Hit").Pressed += OnSettingsPressed;
	}

	private void RedrawRings()
	{
		_rings ??= GetNodeOrNull<Control>("Rings");
		_rings?.QueueRedraw();
	}

	private void OnRingsDraw()
	{
		if (_rings == null)
			return;
		var center = _rings.Size * 0.5f;
		if (center == Vector2.Zero)
			return;
		_rings.DrawArc(center, InnerRingRadius, 0f, Mathf.Tau, 128, RingColor, RingThickness, true);
		_rings.DrawArc(center, OuterRingRadius, 0f, Mathf.Tau, 128, RingColor, RingThickness, true);
	}

	private void OnStartPressed()
	{
		GD.Print("[StartMenu] 开始游戏 → res://main.tscn");
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Error err = GetTree().ChangeSceneToFile("res://main.tscn");
		if (err != Error.Ok)
			GD.PrintErr($"[StartMenu] 切换场景失败: {err}");
	}

	private void OnArchivePressed()
	{
		GD.Print("[StartMenu] 存档 — 占位，尚未接入");
	}

	private void OnCreditsPressed()
	{
		GD.Print("[StartMenu] 制作者 — 占位，尚未接入");
	}

	private void OnSettingsPressed()
	{
		GD.Print("[StartMenu] 设置 — 占位，尚未接入");
	}
}

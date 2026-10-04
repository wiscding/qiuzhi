using Godot;

[Tool]
public partial class StartMenu : Control
{
	[Export(PropertyHint.File, "*.tscn")] public string GameScenePath = "res://main.tscn";
	[Export] public float HoverScale = 1.06f;
	[Export] public float HoverTweenTime = 0.12f;

	//美术原图尺寸，Stage 下的子节点都按这套像素坐标摆
	private static readonly Vector2 DesignSize = new(2736f, 1536f);

	private Control _stage;

	public override void _Ready()
	{
		_stage = GetNode<Control>("Stage");
		LayoutStage();

		if (Engine.IsEditorHint())
			return;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<CanvasItem>("Stage/LayoutReference").Visible = false;

		SetupButton("Stage/StartButton", OnStartPressed);
		SetupButton("Stage/SettingsButton", OnSettingsPressed);
		SetupButton("Stage/CreditsButton", OnCreditsPressed);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			LayoutStage();
	}

	//等比铺满屏幕（多出来的部分裁掉），保证底图和组件始终对齐
	private void LayoutStage()
	{
		_stage ??= GetNodeOrNull<Control>("Stage");
		if (_stage == null || Size.X <= 0f || Size.Y <= 0f)
			return;

		float s = Mathf.Max(Size.X / DesignSize.X, Size.Y / DesignSize.Y);
		_stage.Size = DesignSize;
		_stage.Scale = new Vector2(s, s);
		_stage.Position = (Size - DesignSize * s) * 0.5f;
	}

	private void SetupButton(string path, System.Action onPressed)
	{
		var button = GetNode<BaseButton>(path);
		button.PivotOffset = button.Size * 0.5f;
		button.MouseEntered += () => TweenScale(button, HoverScale);
		button.MouseExited += () => TweenScale(button, 1f);
		button.Pressed += onPressed;
	}

	private void TweenScale(Control target, float scale)
	{
		var tween = target.CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(target, "scale", new Vector2(scale, scale), HoverTweenTime);
	}

	private void OnStartPressed()
	{
		GD.Print($"[StartMenu] 开始观测 → {GameScenePath}");
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Error err = GetTree().ChangeSceneToFile(GameScenePath);
		if (err != Error.Ok)
			GD.PrintErr($"[StartMenu] 切换场景失败: {err}");
	}

	private void OnSettingsPressed()
	{
		GD.Print("[StartMenu] 设置 — 占位，尚未接入");
	}

	private void OnCreditsPressed()
	{
		GD.Print("[StartMenu] 制作人 — 占位，尚未接入");
	}
}

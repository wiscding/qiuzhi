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
	private Control _overlayRoot;
	private Label _overlayTitle;
	private Label _overlayBody;
	private bool _overlayOpen;

	public override void _Ready()
	{
		_stage = GetNode<Control>("Stage");
		LayoutStage();
		BuildOverlay();

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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_overlayOpen)
			return;
		if (@event.IsActionPressed("ui_cancel") ||
			(@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape))
		{
			HideOverlay();
			GetViewport().SetInputAsHandled();
		}
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
		if (_overlayOpen)
			return;
		GD.Print($"[StartMenu] 开始观测 → {GameScenePath}");
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Error err = GetTree().ChangeSceneToFile(GameScenePath);
		if (err != Error.Ok)
			GD.PrintErr($"[StartMenu] 切换场景失败: {err}");
	}

	private void OnSettingsPressed()
	{
		ShowOverlay(
			"设置",
			"音量、画质与键位设置页尚未接入。\n\n当前键位：\n· 移动 WASD\n· 视角 鼠标\n· 重力切换 E（需重力靴）\n· 交互 F\n· 瞄准 右键 / 抓放 左键\n· 道具切换 1 / 2\n\n按 Esc 或点「返回」关闭。");
	}

	private void OnCreditsPressed()
	{
		ShowOverlay(
			"制作人",
			"囚知 qiuzhi\n\n程序 / 关卡搭建：开发中\n美术 / UI：项目素材包\n策划文档：桌面《新手关卡》《对话一览》等\n\n本页为占位致谢页，正式名单待补。\n\n按 Esc 或点「返回」关闭。");
	}

	private void BuildOverlay()
	{
		_overlayRoot = new Control
		{
			Name = "ShellOverlay",
			Visible = false,
			MouseFilter = MouseFilterEnum.Stop,
		};
		_overlayRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_overlayRoot);

		var dim = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.72f),
			MouseFilter = MouseFilterEnum.Stop,
		};
		dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_overlayRoot.AddChild(dim);

		var panel = new PanelContainer
		{
			Name = "Panel",
			MouseFilter = MouseFilterEnum.Stop,
		};
		panel.AnchorLeft = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -280;
		panel.OffsetTop = -200;
		panel.OffsetRight = 280;
		panel.OffsetBottom = 200;
		_overlayRoot.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 28);
		margin.AddThemeConstantOverride("margin_top", 24);
		margin.AddThemeConstantOverride("margin_right", 28);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 14);
		margin.AddChild(vbox);

		_overlayTitle = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_overlayTitle.AddThemeFontSizeOverride("font_size", 28);
		_overlayTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.93f, 0.88f));
		vbox.AddChild(_overlayTitle);

		_overlayBody = new Label
		{
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_overlayBody.AddThemeFontSizeOverride("font_size", 16);
		_overlayBody.AddThemeColorOverride("font_color", new Color(0.85f, 0.86f, 0.88f));
		vbox.AddChild(_overlayBody);

		var back = new Button
		{
			Text = "返回",
			CustomMinimumSize = new Vector2(120, 36),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
		};
		back.Pressed += HideOverlay;
		vbox.AddChild(back);
	}

	private void ShowOverlay(string title, string body)
	{
		_overlayTitle.Text = title;
		_overlayBody.Text = body;
		_overlayRoot.Visible = true;
		_overlayOpen = true;
		_stage.Modulate = new Color(1, 1, 1, 0.35f);
	}

	private void HideOverlay()
	{
		_overlayRoot.Visible = false;
		_overlayOpen = false;
		_stage.Modulate = Colors.White;
	}
}

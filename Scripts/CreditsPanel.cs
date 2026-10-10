using Godot;

/// <summary>开始菜单制作人页：与设置页同套 UI 素材。</summary>
public partial class CreditsPanel : Control
{
	private const string UiDir = "res://Art/界面示意/UI独立PNG/";

	private static readonly string[] Names =
	{
		"黄颖",
		"卢鹏宇",
		"王晨煊",
		"王业嘉",
	};

	[Signal] public delegate void ClosedEventHandler();

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Visible = false;
		BuildUi();
		AudioSettings.Instance?.HookButtonClicks(this);
	}

	public void Open()
	{
		Visible = true;
	}

	public void Close()
	{
		Visible = false;
		EmitSignal(SignalName.Closed);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;
		if (@event.IsActionPressed("ui_cancel") ||
			(@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	private void BuildUi()
	{
		var dimTex = LoadTex("弹窗通用_全屏遮罩.png");
		var panelTex = LoadTex("对话设置_面板底图.png");
		var backIcon = LoadTex("图标_返回.png");

		Control dim = dimTex != null
			? new TextureRect
			{
				Texture = dimTex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				Modulate = new Color(1, 1, 1, 0.85f),
				MouseFilter = MouseFilterEnum.Stop,
			}
			: new ColorRect
			{
				Color = new Color(0, 0, 0, 0.72f),
				MouseFilter = MouseFilterEnum.Stop,
			};
		dim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(dim);

		var panel = new Control
		{
			Name = "Panel",
			CustomMinimumSize = new Vector2(560, 480),
			MouseFilter = MouseFilterEnum.Stop,
		};
		panel.AnchorLeft = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -280;
		panel.OffsetTop = -240;
		panel.OffsetRight = 280;
		panel.OffsetBottom = 240;
		AddChild(panel);

		Control bg = panelTex != null
			? new TextureRect
			{
				Texture = panelTex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = MouseFilterEnum.Ignore,
			}
			: new ColorRect
			{
				Color = new Color(0.06f, 0.12f, 0.16f, 0.96f),
				MouseFilter = MouseFilterEnum.Ignore,
			};
		bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		panel.AddChild(bg);

		var margin = new MarginContainer();
		margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 44);
		margin.AddThemeConstantOverride("margin_top", 36);
		margin.AddThemeConstantOverride("margin_right", 44);
		margin.AddThemeConstantOverride("margin_bottom", 28);
		panel.AddChild(margin);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 12);
		margin.AddChild(root);

		var title = new Label
		{
			Text = "制作人",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		title.AddThemeFontSizeOverride("font_size", 34);
		title.AddThemeColorOverride("font_color", new Color(0.95f, 0.97f, 0.98f));
		root.AddChild(title);

		var project = new Label
		{
			Text = "囚知 qiuzhi",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		project.AddThemeFontSizeOverride("font_size", 16);
		project.AddThemeColorOverride("font_color", new Color(0.55f, 0.7f, 0.75f));
		root.AddChild(project);

		root.AddChild(MakeDivider());

		var list = new VBoxContainer();
		list.AddThemeConstantOverride("separation", 10);
		list.SizeFlagsVertical = SizeFlags.ExpandFill;
		root.AddChild(list);

		foreach (string name in Names)
		{
			var row = new Label
			{
				Text = name,
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			row.AddThemeFontSizeOverride("font_size", 26);
			row.AddThemeColorOverride("font_color", new Color(0.92f, 0.95f, 0.96f));
			list.AddChild(row);
		}

		root.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

		var backBtn = new Button
		{
			Text = "  返回主界面",
			CustomMinimumSize = new Vector2(200, 48),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			Flat = true,
		};
		backBtn.AddThemeFontSizeOverride("font_size", 18);
		backBtn.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 0.96f));
		backBtn.AddThemeColorOverride("font_hover_color", new Color(0.55f, 0.9f, 0.95f));
		if (backIcon != null)
			backBtn.Icon = backIcon;
		backBtn.AddThemeStyleboxOverride("normal", MakeButtonStyle(new Color(0.2f, 0.55f, 0.6f, 0.95f)));
		backBtn.AddThemeStyleboxOverride("hover", MakeButtonStyle(new Color(0.28f, 0.68f, 0.72f, 1f)));
		backBtn.AddThemeStyleboxOverride("pressed", MakeButtonStyle(new Color(0.15f, 0.42f, 0.48f, 1f)));
		backBtn.Pressed += Close;
		root.AddChild(backBtn);
	}

	private static StyleBoxFlat MakeButtonStyle(Color bg)
	{
		return new StyleBoxFlat
		{
			BgColor = bg,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomRight = 4,
			CornerRadiusBottomLeft = 4,
			ContentMarginLeft = 18,
			ContentMarginRight = 22,
			ContentMarginTop = 10,
			ContentMarginBottom = 10,
		};
	}

	private static Control MakeDivider()
	{
		return new ColorRect
		{
			Color = new Color(0.35f, 0.5f, 0.55f, 0.35f),
			CustomMinimumSize = new Vector2(0, 1),
			MouseFilter = MouseFilterEnum.Ignore,
		};
	}

	private static Texture2D LoadTex(string fileName)
	{
		string path = UiDir + fileName;
		if (!ResourceLoader.Exists(path))
		{
			GD.PrintErr($"[CreditsPanel] 缺少贴图：{path}");
			return null;
		}
		return GD.Load<Texture2D>(path);
	}
}

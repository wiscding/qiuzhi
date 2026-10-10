using Godot;

/// <summary>开始菜单设置页：BGM / 音效 开关与音量滑条、返回主界面。</summary>
public partial class SettingsPanel : Control
{
	private const string UiDir = "res://Art/界面示意/UI独立PNG/";

	private TextureButton _bgmToggle;
	private TextureButton _sfxToggle;
	private SliderRow _bgmSlider;
	private SliderRow _sfxSlider;
	private Texture2D _toggleOn;
	private Texture2D _toggleOff;
	private Texture2D _trackTex;
	private Texture2D _fillTex;
	private Texture2D _knobTex;
	private Texture2D _volumeIcon;

	[Signal] public delegate void ClosedEventHandler();

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		Visible = false;
		BuildUi();
		AudioSettings.Instance?.HookButtonClicks(this);
		RefreshFromSettings();
	}

	public void Open()
	{
		RefreshFromSettings();
		Visible = true;
	}

	public void Close()
	{
		_bgmSlider?.StopDrag();
		_sfxSlider?.StopDrag();
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
		_toggleOn = LoadTex("设置_开关_开启.png");
		_toggleOff = LoadTex("设置_开关_关闭.png");
		_trackTex = LoadTex("设置_滑杆轨道.png");
		_fillTex = LoadTex("设置_滑杆已填充.png");
		_knobTex = LoadTex("设置_滑杆圆形手柄.png");
		_volumeIcon = LoadTex("图标_音量.png");
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
			CustomMinimumSize = new Vector2(720, 620),
			MouseFilter = MouseFilterEnum.Stop,
		};
		panel.AnchorLeft = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -360;
		panel.OffsetTop = -310;
		panel.OffsetRight = 360;
		panel.OffsetBottom = 310;
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
		margin.AddThemeConstantOverride("margin_left", 40);
		margin.AddThemeConstantOverride("margin_top", 28);
		margin.AddThemeConstantOverride("margin_right", 40);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		panel.AddChild(margin);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 14);
		margin.AddChild(root);

		var title = new Label { Text = "设置" };
		title.AddThemeFontSizeOverride("font_size", 34);
		title.AddThemeColorOverride("font_color", new Color(0.95f, 0.97f, 0.98f));
		root.AddChild(title);

		var subtitle = new Label { Text = "音频" };
		subtitle.AddThemeFontSizeOverride("font_size", 16);
		subtitle.AddThemeColorOverride("font_color", new Color(0.55f, 0.7f, 0.75f));
		root.AddChild(subtitle);
		root.AddChild(MakeDivider());

		_bgmToggle = AddToggleRow(root, "BGM", "背景音乐开关", OnBgmTogglePressed);
		root.AddChild(MakeDivider());
		_bgmSlider = AddSliderRow(root, "BGM 音量", v =>
		{
			AudioSettings.Instance?.SetBgmVolume(v);
			RefreshFromSettings();
		});
		root.AddChild(MakeDivider());

		_sfxToggle = AddToggleRow(root, "音效", "游戏音效开关", OnSfxTogglePressed);
		root.AddChild(MakeDivider());
		_sfxSlider = AddSliderRow(root, "音效音量", v =>
		{
			AudioSettings.Instance?.SetSfxVolume(v);
			RefreshFromSettings();
		});

		root.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

		var backBtn = new Button
		{
			Text = "  返回主界面",
			CustomMinimumSize = new Vector2(200, 48),
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
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

	private TextureButton AddToggleRow(VBoxContainer root, string title, string desc, System.Action onPressed)
	{
		var toggleRow = new HBoxContainer();
		toggleRow.AddThemeConstantOverride("separation", 16);
		root.AddChild(toggleRow);

		var toggleTexts = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		toggleRow.AddChild(toggleTexts);

		var toggleTitle = new Label { Text = title };
		toggleTitle.AddThemeFontSizeOverride("font_size", 22);
		toggleTitle.AddThemeColorOverride("font_color", Colors.White);
		toggleTexts.AddChild(toggleTitle);

		var toggleDesc = new Label { Text = desc };
		toggleDesc.AddThemeFontSizeOverride("font_size", 14);
		toggleDesc.AddThemeColorOverride("font_color", new Color(0.65f, 0.72f, 0.75f));
		toggleTexts.AddChild(toggleDesc);

		var toggle = new TextureButton
		{
			IgnoreTextureSize = true,
			StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(96, 48),
			TextureNormal = _toggleOn,
		};
		toggle.Pressed += () => onPressed();
		toggleRow.AddChild(toggle);
		return toggle;
	}

	private SliderRow AddSliderRow(VBoxContainer root, string title, System.Action<float> onChanged)
	{
		var volHeader = new HBoxContainer();
		volHeader.AddThemeConstantOverride("separation", 10);
		root.AddChild(volHeader);

		if (_volumeIcon != null)
		{
			volHeader.AddChild(new TextureRect
			{
				Texture = _volumeIcon,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = new Vector2(28, 28),
				MouseFilter = MouseFilterEnum.Ignore,
			});
		}

		var volTitle = new Label { Text = title, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		volTitle.AddThemeFontSizeOverride("font_size", 22);
		volTitle.AddThemeColorOverride("font_color", Colors.White);
		volHeader.AddChild(volTitle);

		var volumeValue = new Label
		{
			Text = "80%",
			HorizontalAlignment = HorizontalAlignment.Right,
			CustomMinimumSize = new Vector2(64, 0),
		};
		volumeValue.AddThemeFontSizeOverride("font_size", 18);
		volumeValue.AddThemeColorOverride("font_color", new Color(0.45f, 0.85f, 0.9f));
		volHeader.AddChild(volumeValue);

		var track = new Control
		{
			CustomMinimumSize = new Vector2(0, 36),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Stop,
		};
		root.AddChild(track);

		Control trackVisual = _trackTex != null
			? new TextureRect
			{
				Texture = _trackTex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = MouseFilterEnum.Ignore,
			}
			: new ColorRect
			{
				Color = new Color(0.2f, 0.28f, 0.32f),
				MouseFilter = MouseFilterEnum.Ignore,
			};
		trackVisual.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		trackVisual.OffsetTop = 14;
		trackVisual.OffsetBottom = -14;
		track.AddChild(trackVisual);

		Control fillBar = _fillTex != null
			? new TextureRect
			{
				Texture = _fillTex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = MouseFilterEnum.Ignore,
			}
			: new ColorRect
			{
				Color = new Color(0.45f, 0.78f, 0.82f),
				MouseFilter = MouseFilterEnum.Ignore,
			};
		fillBar.AnchorLeft = 0;
		fillBar.AnchorTop = 0;
		fillBar.AnchorRight = 0;
		fillBar.AnchorBottom = 1;
		fillBar.OffsetTop = 14;
		fillBar.OffsetBottom = -14;
		track.AddChild(fillBar);

		var knob = new TextureRect
		{
			Texture = _knobTex,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		knob.AnchorLeft = 0;
		knob.AnchorTop = 0.5f;
		knob.AnchorRight = 0;
		knob.AnchorBottom = 0.5f;
		knob.OffsetLeft = -14;
		knob.OffsetTop = -14;
		knob.OffsetRight = 14;
		knob.OffsetBottom = 14;
		track.AddChild(knob);

		var row = new SliderRow(track, fillBar, knob, volumeValue, onChanged);
		track.GuiInput += row.OnGuiInput;
		track.Resized += row.RefreshVisual;
		return row;
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
			GD.PrintErr($"[SettingsPanel] 缺少贴图：{path}");
			return null;
		}
		return GD.Load<Texture2D>(path);
	}

	private void OnBgmTogglePressed()
	{
		AudioSettings.Instance?.ToggleBgm();
		RefreshFromSettings();
	}

	private void OnSfxTogglePressed()
	{
		AudioSettings.Instance?.ToggleSfx();
		RefreshFromSettings();
	}

	private void RefreshFromSettings()
	{
		var audio = AudioSettings.Instance;
		bool bgmOn = audio?.BgmEnabled ?? true;
		float bgmVol = audio?.BgmVolume ?? 0.8f;
		bool sfxOn = audio?.SfxEnabled ?? true;
		float sfxVol = audio?.SfxVolume ?? 0.8f;

		if (_bgmToggle != null)
			_bgmToggle.TextureNormal = bgmOn ? _toggleOn : _toggleOff;
		if (_sfxToggle != null)
			_sfxToggle.TextureNormal = sfxOn ? _toggleOn : _toggleOff;

		_bgmSlider?.SetValue(bgmVol);
		_sfxSlider?.SetValue(sfxVol);
	}

	private sealed class SliderRow
	{
		private readonly Control _track;
		private readonly Control _fillBar;
		private readonly TextureRect _knob;
		private readonly Label _valueLabel;
		private readonly System.Action<float> _onChanged;
		private bool _dragging;
		private float _value = 0.8f;

		public SliderRow(Control track, Control fillBar, TextureRect knob, Label valueLabel, System.Action<float> onChanged)
		{
			_track = track;
			_fillBar = fillBar;
			_knob = knob;
			_valueLabel = valueLabel;
			_onChanged = onChanged;
		}

		public void StopDrag() => _dragging = false;

		public void SetValue(float v)
		{
			_value = Mathf.Clamp(v, 0f, 1f);
			if (_valueLabel != null)
				_valueLabel.Text = $"{Mathf.RoundToInt(_value * 100f)}%";
			RefreshVisual();
		}

		public void OnGuiInput(InputEvent @event)
		{
			if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
			{
				_dragging = mb.Pressed;
				if (mb.Pressed)
					UpdateFromMouse();
				_track.AcceptEvent();
			}
			else if (@event is InputEventMouseMotion && _dragging)
			{
				UpdateFromMouse();
				_track.AcceptEvent();
			}
		}

		private void UpdateFromMouse()
		{
			float width = Mathf.Max(_track.Size.X, 1f);
			float v = Mathf.Clamp(_track.GetLocalMousePosition().X / width, 0f, 1f);
			_onChanged?.Invoke(v);
		}

		public void RefreshVisual()
		{
			float width = Mathf.Max(_track.Size.X, 1f);
			float w = width * _value;

			if (_fillBar != null)
			{
				_fillBar.OffsetLeft = 0;
				_fillBar.OffsetRight = w;
				_fillBar.AnchorRight = 0;
			}

			if (_knob != null)
			{
				_knob.OffsetLeft = w - 14;
				_knob.OffsetRight = w + 14;
			}
		}
	}
}

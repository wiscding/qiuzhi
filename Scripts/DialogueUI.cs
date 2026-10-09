using Godot;
using System.Collections.Generic;
using System.Text;

public partial class DialogueUI : Control
{
	public static DialogueUI Instance { get; private set; }
	public static bool IsOpen => Instance != null && Instance._active;

	[Export] public Texture2D StageBackground;
	[Export] public Texture2D BottomFade;
	[Export] public Texture2D NameMarker;
	[Export] public Texture2D NameDivider;
	[Export] public Texture2D AvatarFrame;
	[Export] public Texture2D ContinueIcon;
	[Export] public float CharsPerSecond = 38f;
	[Export] public float GlitchInterval = 0.07f;
	[Export] public float GlitchChance = 0.14f;

	private Control _stageRoot;
	private ColorRect _blackout;
	private TextureRect _background;
	private TextureRect _portraitCenter;
	private TextureRect _portraitRight;
	private TextureRect _bottomFade;
	private TextureRect _nameMarker;
	private Label _speakerName;
	private Label _speakerTitle;
	private TextureRect _divider;
	private Control _avatarRoot;
	private TextureRect _avatarFrame;
	private TextureRect _avatarImage;
	private Label _bodyText;
	private Control _continueRow;
	private Label _centerText;
	private TextureRect _fullscreenImage;
	private ColorRect _clickCatcher;
	private Color _blackoutDefault = Colors.Black;

	private readonly List<DialogueLine> _lines = new();
	private readonly RandomNumberGenerator _rng = new();
	private int _index;
	private bool _active;
	private string _dialogId = "";
	private Input.MouseModeEnum _prevMouse = Input.MouseModeEnum.Captured;

	private string _fullText = "";
	private int _visibleChars;
	private float _typeTimer;
	private bool _typing;
	private bool _glitchLine;
	private float _glitchTimer;
	private Label _typeTarget;

	private static readonly string GlitchPool = "█▓▒░◆◇※＊＃＠＆％Ｘｘ０１ｌＩ░▒";

	public override void _Ready()
	{
		Instance = this;
		_rng.Randomize();
		BuildUi();
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;
		SetProcess(false);
		SetProcessUnhandledInput(true);
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public bool Play(string dialogId)
	{
		if (!DialogueCatalog.TryGet(dialogId, out var lines))
		{
			GD.PrintErr($"[DialogueUI] 未知对话：{dialogId}");
			return false;
		}
		if (_active)
		{
			GD.Print($"[DialogueUI] 已在播放中，忽略：{dialogId}");
			return false;
		}

		_lines.Clear();
		_lines.AddRange(lines);
		_index = 0;
		_dialogId = dialogId;
		_active = true;
		_prevMouse = Input.MouseMode;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Visible = true;
		MouseFilter = MouseFilterEnum.Stop;
		_clickCatcher.MouseFilter = MouseFilterEnum.Stop;
		GameEvents.EmitDialogStarted(dialogId);
		ShowCurrent();
		return true;
	}

	public override void _Process(double delta)
	{
		if (!_active)
			return;

		float dt = (float)delta;

		if (_typing)
		{
			_typeTimer += dt;
			float interval = 1f / Mathf.Max(CharsPerSecond, 1f);
			while (_typeTimer >= interval && _visibleChars < _fullText.Length)
			{
				_typeTimer -= interval;
				_visibleChars++;
				// 换行稍快一点
				if (_visibleChars < _fullText.Length && _fullText[_visibleChars - 1] == '\n')
					_visibleChars = Mathf.Min(_fullText.Length, _visibleChars + 1);
			}

			RefreshTypedText();

			if (_visibleChars >= _fullText.Length)
			{
				_typing = false;
				_continueRow.Visible = true;
				if (!_glitchLine)
					SetProcess(false);
			}
		}

		if (_glitchLine)
		{
			_glitchTimer += dt;
			if (_glitchTimer >= GlitchInterval)
			{
				_glitchTimer = 0f;
				RefreshTypedText();
				float a = _rng.RandfRange(0.72f, 1f);
				_centerText.Modulate = new Color(1f, 1f, 1f, a);
				// 偶发横向微抖
				if (_rng.Randf() < 0.35f)
					_centerText.Position = new Vector2(_rng.RandfRange(-3f, 3f), _rng.RandfRange(-1.5f, 1.5f));
				else
					_centerText.Position = Vector2.Zero;
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_active)
			return;

		bool advance = false;
		if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("interact"))
			advance = true;
		else if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			advance = true;
		else if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space)
			advance = true;

		if (!advance)
			return;

		GetViewport().SetInputAsHandled();
		Advance();
	}

	private void Advance()
	{
		if (_typing)
		{
			FinishTyping();
			return;
		}

		StopLineEffects();
		_index++;
		if (_index >= _lines.Count)
		{
			Close();
			return;
		}
		ShowCurrent();
	}

	private void FinishTyping()
	{
		_visibleChars = _fullText.Length;
		_typing = false;
		RefreshTypedText();
		_continueRow.Visible = true;
		if (!_glitchLine)
			SetProcess(false);
	}

	private void Close()
	{
		StopLineEffects();
		_active = false;
		_lines.Clear();
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;
		_clickCatcher.MouseFilter = MouseFilterEnum.Ignore;
		Input.MouseMode = _prevMouse;
		string endedId = _dialogId;
		_dialogId = "";
		GameEvents.EmitDialogEnded(endedId);
	}

	private void StopLineEffects()
	{
		_typing = false;
		_glitchLine = false;
		_typeTimer = 0f;
		_glitchTimer = 0f;
		SetProcess(false);
		if (_centerText != null)
		{
			_centerText.Modulate = Colors.White;
			_centerText.Position = Vector2.Zero;
		}
	}

	private void ShowCurrent()
	{
		DialogueLine line = _lines[_index];
		ApplyLayout(line);

		_speakerName.Text = line.Speaker;
		_speakerTitle.Text = line.Title;

		bool black = line.Layout == DialogueLayout.BlackCenter;
		bool fullscreen = line.Layout == DialogueLayout.FullscreenImage;
		bool cardBack = line.Layout == DialogueLayout.CardBack;
		bool hideChrome = black || fullscreen || cardBack;

		_bodyText.Visible = !hideChrome || fullscreen; // 全屏图可带底部说明
		_speakerName.Visible = !hideChrome;
		_nameMarker.Visible = !hideChrome;
		_divider.Visible = !hideChrome;
		_speakerTitle.Visible = !hideChrome && !string.IsNullOrEmpty(line.Title);
		_bottomFade.Visible = !hideChrome || fullscreen;
		_centerText.Visible = black || cardBack;
		_fullscreenImage.Visible = fullscreen;
		_continueRow.Visible = false;

		_centerText.AddThemeColorOverride("font_color",
			cardBack ? new Color(0.12f, 0.12f, 0.14f) : new Color(0.92f, 0.93f, 0.95f));
		_centerText.AddThemeFontSizeOverride("font_size", cardBack ? 26 : 28);

		if (line.ShowAvatar && !string.IsNullOrEmpty(line.AvatarPath) && !hideChrome)
		{
			_avatarRoot.Visible = true;
			_avatarImage.Texture = LoadTex(line.AvatarPath);
		}
		else
		{
			_avatarRoot.Visible = false;
		}

		if (fullscreen)
		{
			_fullscreenImage.Texture = string.IsNullOrEmpty(line.PortraitPath)
				? null
				: LoadTex(line.PortraitPath);
			StartTypewriter(line.Text, _bodyText, false);
		}
		else if (cardBack || black)
		{
			StartTypewriter(line.Text, _centerText, black);
		}
		else
		{
			StartTypewriter(line.Text, _bodyText, false);
		}
	}

	private void StartTypewriter(string text, Label target, bool glitch)
	{
		_fullText = text ?? "";
		_visibleChars = 0;
		_typeTimer = 0f;
		_glitchTimer = 0f;
		_typing = _fullText.Length > 0;
		_glitchLine = glitch;
		_typeTarget = target;
		_bodyText.Text = "";
		_centerText.Text = "";
		_centerText.Modulate = Colors.White;
		_centerText.Position = Vector2.Zero;

		if (!_typing)
		{
			_continueRow.Visible = true;
			SetProcess(glitch);
			return;
		}

		RefreshTypedText();
		SetProcess(true);
	}

	private void RefreshTypedText()
	{
		if (_typeTarget == null)
			return;

		int count = Mathf.Clamp(_visibleChars, 0, _fullText.Length);
		string visible = _fullText.Substring(0, count);
		if (_glitchLine && visible.Length > 0)
			visible = ApplyGlitch(visible);
		_typeTarget.Text = visible;
	}

	private string ApplyGlitch(string source)
	{
		var sb = new StringBuilder(source.Length);
		for (int i = 0; i < source.Length; i++)
		{
			char c = source[i];
			if (char.IsWhiteSpace(c) || _rng.Randf() >= GlitchChance)
			{
				sb.Append(c);
				continue;
			}
			sb.Append(GlitchPool[_rng.RandiRange(0, GlitchPool.Length - 1)]);
		}
		return sb.ToString();
	}

	private void ApplyLayout(DialogueLine line)
	{
		bool cinematic = line.Layout is DialogueLayout.DoctorCenter or DialogueLayout.DoctorBack
			or DialogueLayout.PlayerAsk or DialogueLayout.PlayerCenter;
		bool black = line.Layout == DialogueLayout.BlackCenter;
		bool fullscreen = line.Layout == DialogueLayout.FullscreenImage;
		bool cardBack = line.Layout == DialogueLayout.CardBack;

		_stageRoot.Visible = cinematic || black || fullscreen || cardBack;
		_blackout.Visible = black || fullscreen || cardBack;
		_blackout.Color = cardBack
			? new Color(0.96f, 0.95f, 0.9f, 1f)
			: fullscreen
				? new Color(0.04f, 0.04f, 0.06f, 0.94f)
				: _blackoutDefault;
		_background.Visible = cinematic;
		if (_background.Visible)
			_background.Texture = StageBackground ?? LoadTex(DialogueCatalog.StageBackgroundPath);

		_portraitCenter.Visible = false;
		_portraitRight.Visible = false;
		_fullscreenImage.Visible = false;

		Texture2D portrait = string.IsNullOrEmpty(line.PortraitPath) ? null : LoadTex(line.PortraitPath);
		switch (line.Layout)
		{
			case DialogueLayout.DoctorCenter:
			case DialogueLayout.PlayerCenter:
			case DialogueLayout.DoctorBack:
				_portraitCenter.Visible = portrait != null;
				_portraitCenter.Texture = portrait;
				break;
			case DialogueLayout.PlayerAsk:
				_portraitRight.Visible = portrait != null;
				_portraitRight.Texture = portrait;
				break;
			case DialogueLayout.FullscreenImage:
				_fullscreenImage.Visible = portrait != null;
				_fullscreenImage.Texture = portrait;
				break;
		}
	}

	private static Texture2D LoadTex(string path)
	{
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
		{
			GD.PrintErr($"[DialogueUI] 缺少贴图：{path}");
			return null;
		}
		return GD.Load<Texture2D>(path);
	}

	private void BuildUi()
	{
		AnchorRight = 1;
		AnchorBottom = 1;
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;

		_clickCatcher = new ColorRect
		{
			Name = "ClickCatcher",
			Color = new Color(0, 0, 0, 0),
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_clickCatcher.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_clickCatcher);
		_clickCatcher.GuiInput += OnCatcherGuiInput;

		_stageRoot = new Control { Name = "StageRoot", MouseFilter = MouseFilterEnum.Ignore };
		_stageRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(_stageRoot);

		_blackout = new ColorRect
		{
			Name = "Blackout",
			Color = Colors.Black,
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_blackoutDefault = Colors.Black;
		_blackout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_stageRoot.AddChild(_blackout);

		_fullscreenImage = MakeTex("FullscreenImage", null);
		_fullscreenImage.Visible = false;
		_fullscreenImage.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_fullscreenImage.OffsetLeft = 120;
		_fullscreenImage.OffsetTop = 40;
		_fullscreenImage.OffsetRight = -120;
		_fullscreenImage.OffsetBottom = -120;
		_fullscreenImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_fullscreenImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_stageRoot.AddChild(_fullscreenImage);

		_background = MakeTex("Background", StageBackground);
		_background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_background.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_background.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
		_stageRoot.AddChild(_background);

		_portraitCenter = MakeTex("PortraitCenter", null);
		_portraitCenter.AnchorLeft = 0.5f;
		_portraitCenter.AnchorTop = 0.08f;
		_portraitCenter.AnchorRight = 0.5f;
		_portraitCenter.AnchorBottom = 0.08f;
		_portraitCenter.OffsetLeft = -280;
		_portraitCenter.OffsetRight = 280;
		_portraitCenter.OffsetTop = 0;
		_portraitCenter.OffsetBottom = 520;
		_portraitCenter.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_portraitCenter.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_stageRoot.AddChild(_portraitCenter);

		_portraitRight = MakeTex("PortraitRight", null);
		_portraitRight.AnchorLeft = 1f;
		_portraitRight.AnchorTop = 0.05f;
		_portraitRight.AnchorRight = 1f;
		_portraitRight.AnchorBottom = 0.05f;
		_portraitRight.OffsetLeft = -520;
		_portraitRight.OffsetRight = -40;
		_portraitRight.OffsetTop = 0;
		_portraitRight.OffsetBottom = 560;
		_portraitRight.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_portraitRight.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_stageRoot.AddChild(_portraitRight);

		_bottomFade = MakeTex("BottomFade", BottomFade ?? LoadTex("res://Art/Dialog/UI/对话通用_底部渐暗承托层.png"));
		_bottomFade.AnchorTop = 1f;
		_bottomFade.AnchorBottom = 1f;
		_bottomFade.AnchorRight = 1f;
		_bottomFade.OffsetTop = -220;
		_bottomFade.OffsetBottom = 0;
		_bottomFade.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_bottomFade.StretchMode = TextureRect.StretchModeEnum.Scale;
		AddChild(_bottomFade);

		_nameMarker = MakeTex("NameMarker", NameMarker ?? LoadTex("res://Art/Dialog/UI/对话通用_姓名几何标记.png"));
		_nameMarker.AnchorTop = 1f;
		_nameMarker.AnchorBottom = 1f;
		_nameMarker.OffsetLeft = 72;
		_nameMarker.OffsetTop = -168;
		_nameMarker.OffsetRight = 92;
		_nameMarker.OffsetBottom = -148;
		_nameMarker.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_nameMarker.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		AddChild(_nameMarker);

		_speakerName = new Label
		{
			Name = "SpeakerName",
			Text = "",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_speakerName.AddThemeColorOverride("font_color", new Color(0.95f, 0.93f, 0.87f));
		_speakerName.AddThemeFontSizeOverride("font_size", 28);
		_speakerName.AnchorTop = 1f;
		_speakerName.AnchorBottom = 1f;
		_speakerName.OffsetLeft = 104;
		_speakerName.OffsetTop = -176;
		_speakerName.OffsetRight = 360;
		_speakerName.OffsetBottom = -140;
		AddChild(_speakerName);

		_speakerTitle = new Label
		{
			Name = "SpeakerTitle",
			Text = "",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_speakerTitle.AddThemeColorOverride("font_color", new Color(0.62f, 0.72f, 0.78f, 0.9f));
		_speakerTitle.AddThemeFontSizeOverride("font_size", 16);
		_speakerTitle.AnchorTop = 1f;
		_speakerTitle.AnchorBottom = 1f;
		_speakerTitle.OffsetLeft = 280;
		_speakerTitle.OffsetTop = -168;
		_speakerTitle.OffsetRight = 560;
		_speakerTitle.OffsetBottom = -140;
		_speakerTitle.VerticalAlignment = VerticalAlignment.Center;
		AddChild(_speakerTitle);

		_divider = MakeTex("Divider", NameDivider ?? LoadTex("res://Art/Dialog/UI/对话通用_姓名下方分隔线.png"));
		_divider.AnchorTop = 1f;
		_divider.AnchorBottom = 1f;
		_divider.OffsetLeft = 72;
		_divider.OffsetTop = -136;
		_divider.OffsetRight = 420;
		_divider.OffsetBottom = -132;
		_divider.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_divider.StretchMode = TextureRect.StretchModeEnum.Scale;
		AddChild(_divider);

		_avatarRoot = new Control
		{
			Name = "AvatarRoot",
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_avatarRoot.AnchorTop = 1f;
		_avatarRoot.AnchorBottom = 1f;
		_avatarRoot.OffsetLeft = 64;
		_avatarRoot.OffsetTop = -320;
		_avatarRoot.OffsetRight = 196;
		_avatarRoot.OffsetBottom = -188;
		AddChild(_avatarRoot);

		_avatarFrame = MakeTex("AvatarFrame", AvatarFrame ?? LoadTex("res://Art/Dialog/UI/对话通用_说话人头像边框.png"));
		_avatarFrame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_avatarFrame.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_avatarFrame.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_avatarRoot.AddChild(_avatarFrame);

		_avatarImage = MakeTex("AvatarImage", null);
		_avatarImage.SetAnchorsPreset(LayoutPreset.FullRect);
		_avatarImage.OffsetLeft = 12;
		_avatarImage.OffsetTop = 12;
		_avatarImage.OffsetRight = -12;
		_avatarImage.OffsetBottom = -12;
		_avatarImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_avatarImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
		_avatarRoot.AddChild(_avatarImage);

		_bodyText = new Label
		{
			Name = "BodyText",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_bodyText.AddThemeColorOverride("font_color", new Color(0.95f, 0.93f, 0.87f));
		_bodyText.AddThemeFontSizeOverride("font_size", 22);
		_bodyText.AnchorTop = 1f;
		_bodyText.AnchorRight = 1f;
		_bodyText.AnchorBottom = 1f;
		_bodyText.OffsetLeft = 72;
		_bodyText.OffsetTop = -124;
		_bodyText.OffsetRight = -72;
		_bodyText.OffsetBottom = -48;
		AddChild(_bodyText);

		_centerText = new Label
		{
			Name = "CenterText",
			Visible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_centerText.AddThemeColorOverride("font_color", new Color(0.92f, 0.93f, 0.95f));
		_centerText.AddThemeFontSizeOverride("font_size", 28);
		_centerText.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_centerText.OffsetLeft = 160;
		_centerText.OffsetRight = -160;
		AddChild(_centerText);

		_continueRow = new HBoxContainer
		{
			Name = "ContinueRow",
			MouseFilter = MouseFilterEnum.Ignore,
			Alignment = BoxContainer.AlignmentMode.End,
		};
		_continueRow.AnchorLeft = 1f;
		_continueRow.AnchorTop = 1f;
		_continueRow.AnchorRight = 1f;
		_continueRow.AnchorBottom = 1f;
		_continueRow.OffsetLeft = -260;
		_continueRow.OffsetTop = -42;
		_continueRow.OffsetRight = -48;
		_continueRow.OffsetBottom = -16;
		AddChild(_continueRow);

		var contLabel = new Label
		{
			Text = "空格 / 点击继续",
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center,
		};
		contLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.78f, 0.8f, 0.85f));
		contLabel.AddThemeFontSizeOverride("font_size", 14);
		_continueRow.AddChild(contLabel);

		var contIcon = MakeTex("ContinueIcon", ContinueIcon ?? LoadTex("res://Art/Dialog/UI/图标_继续指示.png"));
		contIcon.CustomMinimumSize = new Vector2(18, 18);
		contIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		contIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		_continueRow.AddChild(contIcon);
	}

	private void OnCatcherGuiInput(InputEvent @event)
	{
		if (!_active)
			return;
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			AcceptEvent();
			Advance();
		}
	}

	private static TextureRect MakeTex(string name, Texture2D tex)
	{
		return new TextureRect
		{
			Name = name,
			Texture = tex,
			MouseFilter = MouseFilterEnum.Ignore,
		};
	}
}

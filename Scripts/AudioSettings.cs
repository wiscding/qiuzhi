using Godot;

/// <summary>BGM / 音效 开关与音量：写入对应总线，并持久化到 user://settings.cfg。</summary>
public partial class AudioSettings : Node
{
	public static AudioSettings Instance { get; private set; }

	public const string MusicBusName = "Music";
	public const string SfxBusName = "SFX";
	private const string ConfigPath = "user://settings.cfg";

	[Export(PropertyHint.File, "*.ogg,*.mp3,*.wav")]
	public string BgmStreamPath = "res://Bgm/解密游戏孤独氛围特雷门琴背景音乐.mp3";

	[Export(PropertyHint.File, "*.ogg,*.mp3,*.wav")]
	public string ButtonSfxPath = "res://Sound/按钮.mp3";

	[Export(PropertyHint.File, "*.ogg,*.mp3,*.wav")]
	public string DoorOpenSfxPath = "res://Sound/开门.mp3";

	public bool BgmEnabled { get; private set; } = true;
	/// <summary>0～1 线性音量。</summary>
	public float BgmVolume { get; private set; } = 0.8f;

	public bool SfxEnabled { get; private set; } = true;
	/// <summary>0～1 线性音量。</summary>
	public float SfxVolume { get; private set; } = 0.8f;

	private AudioStreamPlayer _bgmPlayer;
	private AudioStream _buttonSfx;
	private AudioStream _doorOpenSfx;

	public override void _Ready()
	{
		Instance = this;
		EnsureBus(MusicBusName);
		EnsureBus(SfxBusName);
		Load();
		_bgmPlayer = new AudioStreamPlayer
		{
			Name = "BgmPlayer",
			Bus = MusicBusName,
		};
		AddChild(_bgmPlayer);
		TryBindStream();
		_buttonSfx = LoadStream(ButtonSfxPath);
		_doorOpenSfx = LoadStream(DoorOpenSfxPath);
		Apply();
		if (BgmEnabled)
			TryPlay();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public void SetBgmEnabled(bool enabled)
	{
		BgmEnabled = enabled;
		Apply();
		Save();
		if (enabled)
			TryPlay();
		else
			_bgmPlayer?.Stop();
	}

	public void SetBgmVolume(float linear01)
	{
		BgmVolume = Mathf.Clamp(linear01, 0f, 1f);
		Apply();
		Save();
	}

	public void ToggleBgm() => SetBgmEnabled(!BgmEnabled);

	public void SetSfxEnabled(bool enabled)
	{
		SfxEnabled = enabled;
		Apply();
		Save();
	}

	public void SetSfxVolume(float linear01)
	{
		SfxVolume = Mathf.Clamp(linear01, 0f, 1f);
		Apply();
		Save();
	}

	public void ToggleSfx() => SetSfxEnabled(!SfxEnabled);

	public void PlayButtonSfx() => PlaySfx(_buttonSfx);

	public void PlayDoorOpenSfx() => PlaySfx(_doorOpenSfx);

	/// <summary>给节点树里所有 BaseButton 挂上按钮音效（幂等：用 meta 标记已挂钩）。</summary>
	public void HookButtonClicks(Node root)
	{
		if (root == null)
			return;
		var stack = new System.Collections.Generic.Stack<Node>();
		stack.Push(root);
		while (stack.Count > 0)
		{
			var n = stack.Pop();
			if (n is BaseButton btn && !btn.HasMeta("sfx_button_hooked"))
			{
				btn.SetMeta("sfx_button_hooked", true);
				btn.Pressed += PlayButtonSfx;
			}
			foreach (Node child in n.GetChildren())
				stack.Push(child);
		}
	}

	private void PlaySfx(AudioStream stream)
	{
		if (!SfxEnabled || stream == null)
			return;
		var player = new AudioStreamPlayer
		{
			Bus = SfxBusName,
			Stream = stream,
		};
		AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	private static AudioStream LoadStream(string path)
	{
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
		{
			GD.PrintErr($"[AudioSettings] 音效未找到：{path}");
			return null;
		}
		return GD.Load<AudioStream>(path);
	}

	private static void EnsureBus(string busName)
	{
		if (AudioServer.GetBusIndex(busName) >= 0)
			return;
		int idx = AudioServer.BusCount;
		AudioServer.AddBus(idx);
		AudioServer.SetBusName(idx, busName);
		AudioServer.SetBusSend(idx, "Master");
	}

	private void Apply()
	{
		ApplyBus(MusicBusName, BgmEnabled, BgmVolume);
		ApplyBus(SfxBusName, SfxEnabled, SfxVolume);
	}

	private static void ApplyBus(string busName, bool enabled, float volume01)
	{
		int idx = AudioServer.GetBusIndex(busName);
		if (idx < 0)
			return;
		AudioServer.SetBusMute(idx, !enabled);
		float linear = Mathf.Max(volume01, 0.0001f);
		AudioServer.SetBusVolumeDb(idx, Mathf.LinearToDb(linear));
	}

	private void TryBindStream()
	{
		if (_bgmPlayer == null)
			return;
		if (string.IsNullOrEmpty(BgmStreamPath) || !ResourceLoader.Exists(BgmStreamPath))
		{
			GD.PrintErr($"[AudioSettings] BGM 未找到：{BgmStreamPath}");
			return;
		}
		var stream = GD.Load<AudioStream>(BgmStreamPath);
		if (stream is AudioStreamMP3 mp3)
			mp3.Loop = true;
		else if (stream is AudioStreamOggVorbis ogg)
			ogg.Loop = true;
		_bgmPlayer.Stream = stream;
	}

	private void TryPlay()
	{
		if (_bgmPlayer?.Stream == null || !BgmEnabled)
			return;
		if (!_bgmPlayer.Playing)
			_bgmPlayer.Play();
	}

	private void Load()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(ConfigPath) != Error.Ok)
			return;
		BgmEnabled = cfg.GetValue("audio", "bgm_enabled", true).AsBool();
		BgmVolume = cfg.GetValue("audio", "bgm_volume", 0.8f).AsSingle();
		SfxEnabled = cfg.GetValue("audio", "sfx_enabled", true).AsBool();
		SfxVolume = cfg.GetValue("audio", "sfx_volume", 0.8f).AsSingle();
	}

	private void Save()
	{
		var cfg = new ConfigFile();
		cfg.Load(ConfigPath); // 保留其它段
		cfg.SetValue("audio", "bgm_enabled", BgmEnabled);
		cfg.SetValue("audio", "bgm_volume", BgmVolume);
		cfg.SetValue("audio", "sfx_enabled", SfxEnabled);
		cfg.SetValue("audio", "sfx_volume", SfxVolume);
		cfg.Save(ConfigPath);
	}
}

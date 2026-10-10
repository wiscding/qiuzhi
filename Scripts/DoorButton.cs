using Godot;

public partial class DoorButton : Interactable
{
	[Export] public int ButtonIndex = 1;
	[Export] public Color IdleColor = new(1f, 0.75f, 0f, 1f);
	[Export] public Color LitColor = new(0.35f, 1f, 0.55f, 1f);
	[Export] public string PressAnimationName = "Animation";

	private bool pressed;
	private AnimationPlayer _anim;
	private MeshInstance3D _statusLamp;
	private StandardMaterial3D _lampMat;

	public override void _Ready()
	{
		_anim = FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer;
		_statusLamp = FindChild("Status_Lamp", recursive: true, owned: false) as MeshInstance3D;
		if (_statusLamp != null)
		{
			_lampMat = _statusLamp.GetActiveMaterial(0)?.Duplicate() as StandardMaterial3D;
			if (_lampMat == null)
				_lampMat = new StandardMaterial3D();
			_lampMat.AlbedoColor = IdleColor;
			_lampMat.EmissionEnabled = false;
			_statusLamp.SetSurfaceOverrideMaterial(0, _lampMat);
		}
	}

	public override void Interact()
	{
		if (pressed)
			return;
		pressed = true;
		AudioSettings.Instance?.PlayButtonSfx();
		if (_anim != null && _anim.HasAnimation(PressAnimationName))
			_anim.Play(PressAnimationName);
		ApplyLit();
		GD.Print($"[DoorButton] 按钮 {ButtonIndex} 被按下");
		GameEvents.EmitDoorButtonPressed(ButtonIndex);
	}

	public override bool CanInteract()
	{
		if (pressed)
			return false;
		return base.CanInteract();
	}

	private void ApplyLit()
	{
		if (_lampMat == null)
			return;
		_lampMat.AlbedoColor = LitColor;
		_lampMat.EmissionEnabled = true;
		_lampMat.Emission = LitColor;
		_lampMat.EmissionEnergyMultiplier = 1.6f;
	}
}

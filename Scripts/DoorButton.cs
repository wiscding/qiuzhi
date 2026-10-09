using Godot;

public partial class DoorButton : Interactable
{
	[Export] public int ButtonIndex = 1;
	[Export] public Color IdleColor = new(1f, 0.75f, 0f, 1f);
	[Export] public Color LitColor = new(0.35f, 1f, 0.55f, 1f);

	private bool pressed;
	private MeshInstance3D _mesh;
	private StandardMaterial3D _mat;

	public override void _Ready()
	{
		_mesh = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
		if (_mesh != null)
		{
			_mat = _mesh.GetActiveMaterial(0)?.Duplicate() as StandardMaterial3D;
			if (_mat == null)
				_mat = new StandardMaterial3D();
			_mat.AlbedoColor = IdleColor;
			_mat.EmissionEnabled = false;
			_mesh.SetSurfaceOverrideMaterial(0, _mat);
		}
	}

	public override void Interact()
	{
		if (pressed)
			return;
		pressed = true;
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
		if (_mat == null)
			return;
		_mat.AlbedoColor = LitColor;
		_mat.EmissionEnabled = true;
		_mat.Emission = LitColor;
		_mat.EmissionEnergyMultiplier = 1.6f;
	}
}

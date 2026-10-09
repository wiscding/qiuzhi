using Godot;

/// <summary>挂在出生点门上：首次按 F 触发对话 3。</summary>
public partial class DoorHandle : Interactable
{
	[Export] public string DialogId = "dialog_3";
	private bool _used;

	public override void _Ready()
	{
		Prompt = "按F查看房门";
	}

	public override void Interact()
	{
		if (_used)
			return;
		_used = true;
		GD.Print("[DoorHandle] 首次与门交互");
		GameEvents.EmitDialogRequested(DialogId);
	}

	public override bool CanInteract()
	{
		if (_used)
			return false;
		return base.CanInteract();
	}
}

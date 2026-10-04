using Godot;

public partial class DoorButton : Interactable
{
	[Export] public int ButtonIndex = 1;
	private bool pressed = false;

	public override void Interact()
	{
		if(pressed)
			return;
		pressed = true;
		GD.Print($"[DoorButton] 按钮 {ButtonIndex} 被按下");
		GameEvents.EmitDoorButtonPressed(ButtonIndex);
	}

	//按下之后就不再可交互（提示会自动消失，也不会重复触发）
	public override bool CanInteract()
	{
		if(pressed)
			return false;
		return base.CanInteract();
	}
}

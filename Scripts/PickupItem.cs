using Godot;
using System;

public partial class PickupItem : Interactable
{
	//inspector里可选
	[Export] public ItemType Item = ItemType.None;

	private bool collected = false;
	public bool IsCollected => collected;

	public override void _Ready()
	{
		//查找的人不用关心自己被挂在哪个父节点下面
		AddToGroup("pickup");
	}

	public override void Interact()
	{
		if(collected)
			return;
		collected = true;
		GD.Print($"[PickupItem] 获得道具：{Item}");
		GameEvents.EmitItemCollected(Item);
		Visible = false;
	}

	public override bool CanInteract()
	{
		// 已捡起就不再可交互（不会显示提示、按了也没反应）
		if (collected) 
			return false;
		return base.CanInteract();
	}
}

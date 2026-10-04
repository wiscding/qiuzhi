using Godot;
using System;

public abstract partial class Interactable : StaticBody3D
{
	[Export] public ItemType RequiredItem = ItemType.None;
	[Export] public string Prompt = "按F交互";
	public string GetPrompt() => Prompt;
	public virtual bool CanInteract()
	{
		if(RequiredItem == ItemType.None)
			return true;
		return GameManager.Instance.State.HasItem(RequiredItem);
	}
	public abstract void Interact();
}

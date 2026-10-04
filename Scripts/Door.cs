using Godot;
using System;

public partial class Door : AnimatableBody3D
{
	[Export] public float OpenDistance = 5f;
	[Export] public float OpenSpeed = 3f;

	private bool isOpening = false;
	private float targetY = 0f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GameEvents.DoorUnlocked += OnDoorUnlocked;
	}
	public override void _ExitTree()
	{
		GameEvents.DoorUnlocked -= OnDoorUnlocked;
	}

	private void OnDoorUnlocked()
	{
		isOpening = true;
		targetY = Position.Y - OpenDistance;
		GD.Print("[Door] 收到解锁事件，门开始下沉");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!isOpening) 
			return;

		float newY = Mathf.MoveToward(Position.Y, targetY, OpenSpeed * (float)delta);
		Position = new Vector3(Position.X, newY, Position.Z);

		if (Mathf.IsEqualApprox(newY, targetY))
			isOpening = false;
	}
}

using Godot;

public partial class Door : AnimatableBody3D
{
	[Export] public float OpenDistance = 5f;
	[Export] public float OpenSpeed = 3f;
	/// <summary>三按钮解锁后下沉开门（新手屋正门）。</summary>
	[Export] public bool OpenOnDoorUnlocked = true;
	/// <summary>拾取该道具后直接消失（密室门）。</summary>
	[Export] public ItemType HideOnItem = ItemType.None;

	private bool isOpening;
	private float targetY;

	public override void _Ready()
	{
		if (OpenOnDoorUnlocked)
			GameEvents.DoorUnlocked += OnDoorUnlocked;
		if (HideOnItem != ItemType.None)
			GameEvents.ItemCollected += OnItemCollected;
	}

	public override void _ExitTree()
	{
		GameEvents.DoorUnlocked -= OnDoorUnlocked;
		GameEvents.ItemCollected -= OnItemCollected;
	}

	private void OnDoorUnlocked()
	{
		isOpening = true;
		targetY = Position.Y - OpenDistance;
		GD.Print($"[Door] {Name} 解锁，开始下沉");
	}

	private void OnItemCollected(ItemType type)
	{
		if (type != HideOnItem)
			return;
		Visible = false;
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D shape)
				shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
		}
		GD.Print($"[Door] {Name} 因拾取 {type} 消失");
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

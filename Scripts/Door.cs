using Godot;

public partial class Door : AnimatableBody3D
{
	[Export] public float OpenDistance = 5f;
	[Export] public float OpenSpeed = 3f;
	/// <summary>三按钮解锁后播放开门动画（无动画则下沉）。</summary>
	[Export] public bool OpenOnDoorUnlocked = true;
	/// <summary>拾取该道具后直接消失（密室门）。</summary>
	[Export] public ItemType HideOnItem = ItemType.None;
	[Export] public string OpenAnimationName = "Animation";

	private bool isOpening;
	private float targetY;
	private AnimationPlayer _anim;

	public override void _Ready()
	{
		_anim = FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer;
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
		DisableCollision();
		AudioSettings.Instance?.PlayDoorOpenSfx();
		if (_anim != null && _anim.HasAnimation(OpenAnimationName))
		{
			_anim.Play(OpenAnimationName);
			GD.Print($"[Door] {Name} 解锁，播放开门动画");
			return;
		}

		isOpening = true;
		targetY = Position.Y - OpenDistance;
		GD.Print($"[Door] {Name} 解锁，开始下沉");
	}

	private void OnItemCollected(ItemType type)
	{
		if (type != HideOnItem)
			return;
		Visible = false;
		DisableCollision();
		GD.Print($"[Door] {Name} 因拾取 {type} 消失");
	}

	private void DisableCollision()
	{
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D shape)
				shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
		}
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

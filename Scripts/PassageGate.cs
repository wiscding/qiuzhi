using Godot;

/// <summary>盖板正确入槽前挡住通道的碰撞体。</summary>
public partial class PassageGate : StaticBody3D
{
	[Export] public string UnlockPuzzleGroup = "tutorial_cover";

	public override void _Ready()
	{
		GameEvents.PuzzleCompleted += OnPuzzleCompleted;
		if (GameManager.Instance != null &&
			GameManager.Instance.State.CompletedPuzzles.Contains(UnlockPuzzleGroup))
		{
			Open();
		}
	}

	public override void _ExitTree()
	{
		GameEvents.PuzzleCompleted -= OnPuzzleCompleted;
	}

	private void OnPuzzleCompleted(string puzzleId)
	{
		if (puzzleId == UnlockPuzzleGroup)
			Open();
	}

	private void Open()
	{
		Visible = false;
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D shape)
				shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
		}
		GD.Print($"[PassageGate] 通道已打开：{UnlockPuzzleGroup}");
	}
}

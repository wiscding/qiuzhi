using Godot;

/// <summary>石堆拼图完成后，向心重力下显示的纹路占位。</summary>
public partial class CentripetalPattern : Node3D
{
	[Export] public string RequiredPuzzleGroup = "tutorial_thirds";

	private bool _puzzleDone;
	private bool _centripetal;

	public override void _Ready()
	{
		Visible = false;
		_puzzleDone = GameManager.Instance != null &&
			GameManager.Instance.State.CompletedPuzzles.Contains(RequiredPuzzleGroup);
		_centripetal = GameManager.Instance != null &&
			GameManager.Instance.State.CurrentGravity == GravityMode.Centripetal;

		GameEvents.PuzzleCompleted += OnPuzzleCompleted;
		GameEvents.GravityChanged += OnGravityChanged;
		Refresh();
	}

	public override void _ExitTree()
	{
		GameEvents.PuzzleCompleted -= OnPuzzleCompleted;
		GameEvents.GravityChanged -= OnGravityChanged;
	}

	private void OnPuzzleCompleted(string puzzleId)
	{
		if (puzzleId != RequiredPuzzleGroup)
			return;
		_puzzleDone = true;
		Refresh();
	}

	private void OnGravityChanged(GravityMode mode)
	{
		_centripetal = mode == GravityMode.Centripetal;
		Refresh();
	}

	private void Refresh()
	{
		Visible = _puzzleDone && _centripetal;
	}
}

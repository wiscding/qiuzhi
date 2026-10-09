using Godot;

public partial class BlockSlot : Area3D
{
	/// <summary>0 表示任意地块都能放；非 0 时必须 BlockIndex 对齐才可放置（石堆三块规则）。</summary>
	[Export] public int RequiredBlockIndex = 0;
	[Export] public string PuzzleGroup = "";

	public bool IsOccupied { get; set; } = false;
	public MovableBlock Occupant { get; set; }

	public bool IsCorrect =>
		Occupant != null && (RequiredBlockIndex == 0 || Occupant.BlockIndex == RequiredBlockIndex);

	public override void _Ready()
	{
		AddToGroup("block_slot");
	}

	public bool Accepts(MovableBlock block)
	{
		if (block == null || IsOccupied)
			return false;
		return RequiredBlockIndex == 0 || block.BlockIndex == RequiredBlockIndex;
	}

	public void Place(MovableBlock block)
	{
		Occupant = block;
		IsOccupied = true;
		block.CurrentSlot = this;
		block.GlobalTransform = GlobalTransform;
		block.IsHeld = false;
		block.SetCollisionEnabled(true);
	}

	public void Clear()
	{
		if (Occupant != null)
			Occupant.CurrentSlot = null;
		Occupant = null;
		IsOccupied = false;
	}
}

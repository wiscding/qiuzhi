using Godot;
using System;

public partial class MovableBlock : CharacterBody3D
{
	[Export] public int BlockIndex = 1;
	public bool IsHeld{get; set;} = false;
	//当前所在缺口（拿在手里 / 没放进去时为 null）
	public BlockSlot CurrentSlot{get; set;} = null;

	public override void _Ready()
	{
		AddToGroup("movable_block");
	}

	//测试
	//private Vector3 lastPos;
	//public override void _PhysicsProcess(double delta)
	//{
		//if (!GlobalPosition.IsEqualApprox(lastPos))
		//{
			//GD.Print($"[Block{BlockIndex}] 动了：{lastPos} → {GlobalPosition}  held={IsHeld}");
			//lastPos = GlobalPosition;
		//}
	//}
	//测试结束

	public void SetCollisionEnabled(bool enabled)
	{
		foreach (Node child in GetChildren())
		{
			if(child is CollisionShape3D shape)
				shape.SetDeferred("disabled", !enabled);
		}
	}
}

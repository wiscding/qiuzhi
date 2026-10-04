using Godot;
using System;

public partial class ShardGuide : Node3D
{
	[Export] public bool AlwaysShow = false;
	private MeshInstance3D thread;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		thread = GetNode<MeshInstance3D>("Thread");
		thread.Visible = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		//5号事件没完成前不指引
		if(!AlwaysShow && !GameManager.Instance.State.CenterObserved)
		{
			thread.Visible = false;
			return;
		}
		PickupItem nearest = FindNearestShard();
		if (nearest == null)
		{
			//碎片都收齐了，丝线隐去
			thread.Visible = false; 
			return;
		}
		thread.Visible = true;
		//只取水平方向：把目标的Y压到和自己一样高，避免正上下方时算不出朝向
		Vector3 flat = new Vector3(nearest.GlobalPosition.X, GlobalPosition.Y, nearest.GlobalPosition.Z);
		if(flat.DistanceSquaredTo(GlobalPosition) < 0.01f)
			return;
		LookAt(flat, Vector3.Up);
	}

	private PickupItem FindNearestShard()
	{
		PickupItem best = null;
		float bestDist = float.MaxValue;
		foreach (Node node in GetTree().GetNodesInGroup("pickup"))
		{
			if (node is not PickupItem pickup)
				continue;
			if (pickup.IsCollected)
				continue;
			if (!IsShard(pickup.Item))
				continue;
			float d = GlobalPosition.DistanceSquaredTo(pickup.GlobalPosition);
			if (d < bestDist)
			{
				bestDist = d;
				best = pickup;
			}
		}
		return best;
	}

	private bool IsShard(ItemType type)
	{
		return type == ItemType.PostcardShard1 || type == ItemType.PostcardShard2 || type == ItemType.PostcardShard3;
	}
}

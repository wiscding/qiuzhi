using Godot;

public partial class TriggerZone : Area3D
{
	//触发区编号
	[Export] public string TriggerId = "";
	//是否只触发一次
	[Export] public bool OnlyOnce = true;
	//运行时是否显示可视方块
	[Export] public bool ShowDebugVisual = false;

	private bool fired = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		//跑起来把调试方块藏掉：编辑器里看得见、游戏里看不见
		if (!ShowDebugVisual)
		{
			MeshInstance3D visual = GetNodeOrNull<MeshInstance3D>("DebugVisual");
			if (visual != null)
				visual.Visible = false;
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if(OnlyOnce && fired)
			return;
		if(body is not Player)
			return;
		fired = true;
		GameEvents.EmitTriggerEntered(TriggerId);
	}
}

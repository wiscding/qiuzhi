using Godot;
using System;

public partial class GameManager : Node
{
	public static GameManager Instance{get; private set;}
	public GameState State{get;} = new();

	//明信片是否已经拼成
	private bool postcardAssembled = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

		GameEvents.GravityChanged += OnGravityChanged;
		GameEvents.ItemCollected += OnItemCollected;
		GameEvents.DoorButtonPressed += OnDoorButtonPressed;
		GameEvents.DoorUnlocked += OnDoorUnlocked;
		GameEvents.BlockMoved += OnBlockMoved;
		GameEvents.PlayerEnteredLayer += OnPlayerEnteredLayer;
		GameEvents.DialogRequested += OnDialogRequested;
		GameEvents.TriggerEntered += OnTriggerEntered;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GameEvents.GravityChanged -= OnGravityChanged;
		GameEvents.ItemCollected -= OnItemCollected;
		GameEvents.DoorButtonPressed -= OnDoorButtonPressed;
		GameEvents.DoorUnlocked -= OnDoorUnlocked;
		GameEvents.BlockMoved -= OnBlockMoved;
		GameEvents.PlayerEnteredLayer -= OnPlayerEnteredLayer;
		GameEvents.DialogRequested -= OnDialogRequested;
		GameEvents.TriggerEntered -= OnTriggerEntered;
	}

	private void OnGravityChanged(GravityMode mode)
	{
		GD.Print($"[GameManager] 收到重力切换，状态更新为：{mode}");
		State.CurrentGravity = mode;
	}

	private void OnItemCollected(ItemType type)
	{
		GD.Print($"[GameManager] 收到获得道具：{type}");
		State.AddItem(type);
		if (!postcardAssembled && State.PostcardShardCount >= 3)
		{
			postcardAssembled = true;
			GD.Print("[GameManager] 明信片碎片集齐，拼成了完整的明信片！");
		}
	}

	private void OnDoorButtonPressed(int index)
	{
		GD.Print($"[GameManager] 收到按下门锁按钮：{index}");
		State.MarkButtonPressed(index);

		if(!State.DoorUnlocked && State.PressedButtons.Count >= 3)
		{
			GD.Print("[GameManager] 三个按钮都已按下，解锁门");
			GameEvents.EmitDoorUnlocked();
		}
	}

	private void OnDoorUnlocked()
	{
		GD.Print("[GameManager] 门已解锁");
		State.DoorUnlocked = true;
	}

	private void OnBlockMoved(int blockId, bool moved)
	{
		GD.Print($"[GameManager] 收到地块 {blockId} 移动，状态：{moved}");
		if (moved)
		{
			State.MarkBlockMoved(blockId);
		}
		else
		{
			State.UnmarkBlockMoved(blockId);
		}
	}

	private void OnPlayerEnteredLayer(GameLayer layer)
	{
		GD.Print($"[GameManager] 玩家进入：{layer}");
		State.CurrentLayer = layer;
	}

	private void OnDialogRequested(string dialogId)
	{
		GD.Print($"[GameManager] 收到对话请求：{dialogId}");
		//接对话系统
	}

	private void OnTriggerEntered(string triggerId)
	{
		GD.Print($"[GameManager] 处理触发区：{triggerId}");
		//按 id 分发：弹对话/给道具/开门/点亮丝线等
		//5号事件：到达C层圆心处观察景观
		if(triggerId == "c_center")
		{
			State.CenterObserved = true;
			GD.Print("[GameManager] 已到达圆心，景观观察完成");
		}
		GameEvents.EmitDialogRequested(triggerId);
	}
}

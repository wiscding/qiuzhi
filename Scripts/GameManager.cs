using Godot;
using System;
using System.Collections.Generic;

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
		GameEvents.PuzzleCompleted += OnPuzzleCompleted;
		GameEvents.PlayerEnteredLayer += OnPlayerEnteredLayer;
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
		GameEvents.PuzzleCompleted -= OnPuzzleCompleted;
		GameEvents.PlayerEnteredLayer -= OnPlayerEnteredLayer;
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
		// 新手关目前只有一块碎片，拾取即视为获得明信片
		if (!postcardAssembled && State.HasPostcard)
		{
			postcardAssembled = true;
			GD.Print("[GameManager] 获得明信片碎片");
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
		CheckPuzzles();
	}

	private void CheckPuzzles()
	{
		var groups = new Dictionary<string, (int filled, int total)>();
		foreach (Node node in GetTree().GetNodesInGroup("block_slot"))
		{
			if (node is not BlockSlot slot)
				continue;
			if (string.IsNullOrEmpty(slot.PuzzleGroup))
				continue;
			if (!groups.TryGetValue(slot.PuzzleGroup, out var counts))
				counts = (0, 0);
			counts.total++;
			if (slot.IsCorrect)
				counts.filled++;
			groups[slot.PuzzleGroup] = counts;
		}

		foreach (var pair in groups)
		{
			if (pair.Value.total == 0 || pair.Value.filled < pair.Value.total)
				continue;
			if (State.CompletedPuzzles.Contains(pair.Key))
				continue;
			State.CompletedPuzzles.Add(pair.Key);
			GameEvents.EmitPuzzleCompleted(pair.Key);
		}
	}

	private void OnPuzzleCompleted(string puzzleId)
	{
		GD.Print($"[GameManager] 拼图完成：{puzzleId}");
	}

	private void OnPlayerEnteredLayer(GameLayer layer)
	{
		GD.Print($"[GameManager] 玩家进入：{layer}");
		State.CurrentLayer = layer;
	}

	private void OnTriggerEntered(string triggerId)
	{
		GD.Print($"[GameManager] 处理触发区：{triggerId}");
		if (triggerId == "c_center")
		{
			State.CenterObserved = true;
			GD.Print("[GameManager] 已到达圆心，景观观察完成");
		}
	}
}

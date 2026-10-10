using Godot;
using System;

//离心和向心
public enum GravityMode{Centrifugal, Centripetal}
//物品类型：重力靴，魔法棒，明信片碎片123
public enum ItemType{None, GravityBoots, MagicWand, PostcardShard1, PostcardShard2, PostcardShard3}
//B层，C层
public enum GameLayer{LayerB, LayerC}

public static class GameEvents
{
	//重力切换
	public static event Action<GravityMode> GravityChanged;
	public static void EmitGravityChanged(GravityMode mode)
	{
		GD.Print($"[GameEvents] 重力切换为：{mode}");
		GravityChanged?.Invoke(mode);
	}

	//获得道具
	public static event Action<ItemType> ItemCollected;
	public static void EmitItemCollected(ItemType type)
	{
		GD.Print($"[GameEvents] 获得道具：{type}");
		ItemCollected?.Invoke(type);
	}

	//按下门锁按钮
	public static event Action<int> DoorButtonPressed;
	public static void EmitDoorButtonPressed(int index)
	{
		GD.Print($"[GameEvents] 按下门锁按钮：{index}");
		DoorButtonPressed?.Invoke(index);
	}

	//门解锁
	public static event Action DoorUnlocked;
	public static void EmitDoorUnlocked()
	{
		GD.Print($"[GameEvents] 门解锁");
		DoorUnlocked?.Invoke();
	}

	//移动地块（moved=true 表示放进了某个缺口）
	public static event Action<int, bool> BlockMoved;
	public static void EmitBlockMoved(int blockId, bool moved)
	{
		GD.Print($"[GameEvents] 地块 {blockId} 移动状态：{moved}");
		BlockMoved?.Invoke(blockId, moved);
	}

	//一组缺口全部放对
	public static event Action<string> PuzzleCompleted;
	public static void EmitPuzzleCompleted(string puzzleId)
	{
		GD.Print($"[GameEvents] 拼图完成：{puzzleId}");
		PuzzleCompleted?.Invoke(puzzleId);
	}

	//玩家进入某一层
	public static event Action<GameLayer> PlayerEnteredLayer;
	public static void EmitPlayerEnteredLayer(GameLayer layer)
	{
		GD.Print($"[GameEvents] 玩家进入层：{layer}");
		PlayerEnteredLayer?.Invoke(layer);
	}

	//请求显示对话
	public static event Action<string> DialogRequested;
	public static void EmitDialogRequested(string dialogId)
	{
		GD.Print($"[GameEvents] 请求显示对话：{dialogId}");
		DialogRequested?.Invoke(dialogId);
	}

	public static event Action<string> DialogStarted;
	public static void EmitDialogStarted(string dialogId)
	{
		GD.Print($"[GameEvents] 对话开始：{dialogId}");
		DialogStarted?.Invoke(dialogId);
	}

	public static event Action<string> DialogEnded;
	public static void EmitDialogEnded(string dialogId = "")
	{
		GD.Print($"[GameEvents] 对话结束：{dialogId}");
		DialogEnded?.Invoke(dialogId);
	}

	//玩家进入触发区(3，4，5号事件)
	public static event Action<string> TriggerEntered;
	public static void EmitTriggerEntered(string triggerId)
	{
		GD.Print($"[GameEvents] 玩家进入触发区：{triggerId}");
		TriggerEntered?.Invoke(triggerId);
	}

	//当前目标文案变化
	public static event Action<string> ObjectiveChanged;
	public static void EmitObjectiveChanged(string text)
	{
		GD.Print($"[GameEvents] 当前目标：{text}");
		ObjectiveChanged?.Invoke(text);
	}

	//装备切换
	public static event Action<EquippedTool> EquippedChanged;
	public static void EmitEquippedChanged(EquippedTool tool)
	{
		GD.Print($"[GameEvents] 装备切换：{tool}");
		EquippedChanged?.Invoke(tool);
	}

	//请求玩家视线看向某世界坐标（触发区引导）
	public static event Action<Vector3, float> LookAtRequested;
	public static void EmitLookAtRequested(Vector3 worldPosition, float duration = 1.1f)
	{
		GD.Print($"[GameEvents] 视线引导 → {worldPosition}");
		LookAtRequested?.Invoke(worldPosition, duration);
	}

	/// <summary>圆心明信片视线检测三条件同时满足。</summary>
	public static event Action PostcardSightSucceeded;
	public static void EmitPostcardSightSucceeded()
	{
		GD.Print("[GameEvents] 明信片视线检测成功");
		PostcardSightSucceeded?.Invoke();
	}
}

using Godot;
using System.Collections.Generic;

public enum EquippedTool
{
	None,
	MagicWand,
	Postcard,
}

public class GameState 
{
	//当前重力模式
	public GravityMode CurrentGravity{get; set;} = GravityMode.Centrifugal;
	//当前层
	public GameLayer CurrentLayer{get; set;} = GameLayer.LayerC;
	//已获得道具
	public HashSet<ItemType> Items{get;} = new();
	//当前装备（道具栏高亮）
	public EquippedTool Equipped{get; set;} = EquippedTool.None;
	//已按下的门锁
	public HashSet<int> PressedButtons{get;} = new();
	//是否解锁
	public bool DoorUnlocked{get; set;} = false;
	//已移动的地块
	public HashSet<int> MovedBlocks{get;} = new();
	//已完成的地块拼图（PuzzleGroup）
	public HashSet<string> CompletedPuzzles{get;} = new();

	//是否已到达C层圆心观察过景观
	public bool CenterObserved{get; set;} = false;

	public void AddItem(ItemType type) =>Items.Add(type);
	public bool HasItem(ItemType type) => Items.Contains(type);
	public bool HasPostcard => PostcardShardCount > 0;

	public void MarkButtonPressed(int index) => PressedButtons.Add(index);
	public bool IsButtonPressed(int index) => PressedButtons.Contains(index);

	public void MarkBlockMoved(int blockId) => MovedBlocks.Add(blockId);
	public void UnmarkBlockMoved(int blockId) => MovedBlocks.Remove(blockId);
	public bool IsBlockMoved(int blockId) => MovedBlocks.Contains(blockId);
	//已收集的明信片碎片数量
	public int PostcardShardCount
	{
		get
		{
			int count = 0;
			if(HasItem(ItemType.PostcardShard1)) count++;
			if(HasItem(ItemType.PostcardShard2)) count++;
			if(HasItem(ItemType.PostcardShard3)) count++;
			return count;
		}
	}
}

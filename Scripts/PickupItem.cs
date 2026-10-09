using Godot;

public partial class PickupItem : Interactable
{
	[Export] public ItemType Item = ItemType.None;
	/// <summary>非空时，对应拼图完成前不可拾取。</summary>
	[Export] public string UnlockPuzzleGroup = "";
	/// <summary>非空时，对应对话结束后才可拾取。</summary>
	[Export] public string UnlockDialogId = "";

	private bool collected;
	private bool unlocked;
	public bool IsCollected => collected;
	public bool IsUnlocked => unlocked;

	public override void _Ready()
	{
		AddToGroup("pickup");
		bool needsPuzzle = !string.IsNullOrEmpty(UnlockPuzzleGroup);
		bool needsDialog = !string.IsNullOrEmpty(UnlockDialogId);
		unlocked = !needsPuzzle && !needsDialog;

		if (needsPuzzle)
		{
			GameEvents.PuzzleCompleted += OnPuzzleCompleted;
			if (GameManager.Instance != null &&
				GameManager.Instance.State.CompletedPuzzles.Contains(UnlockPuzzleGroup))
			{
				TryUnlockFromPuzzle();
			}
		}

		if (needsDialog)
			GameEvents.DialogEnded += OnDialogEnded;

		if (!unlocked)
			ApplyLockedVisual();
	}

	public override void _ExitTree()
	{
		GameEvents.PuzzleCompleted -= OnPuzzleCompleted;
		GameEvents.DialogEnded -= OnDialogEnded;
	}

	public override void Interact()
	{
		if (collected || !unlocked)
			return;
		collected = true;
		GD.Print($"[PickupItem] 获得道具：{Item}");
		GameEvents.EmitItemCollected(Item);
		Visible = false;
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D shape)
				shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
		}
	}

	public override bool CanInteract()
	{
		if (collected || !unlocked)
			return false;
		return base.CanInteract();
	}

	private void OnPuzzleCompleted(string puzzleId)
	{
		if (puzzleId == UnlockPuzzleGroup)
			TryUnlockFromPuzzle();
	}

	private void TryUnlockFromPuzzle()
	{
		// 仅拼图条件时解锁；若还绑了对话，仍等对话
		if (string.IsNullOrEmpty(UnlockDialogId))
			Unlock("puzzle:" + UnlockPuzzleGroup);
	}

	private void OnDialogEnded(string dialogId)
	{
		if (!unlocked && dialogId == UnlockDialogId)
			Unlock("dialog:" + UnlockDialogId);
	}

	private void Unlock(string reason)
	{
		unlocked = true;
		SetMeshAlbedoAlpha(1f);
		GD.Print($"[PickupItem] 已解锁：{Item}（{reason}）");
	}

	private void ApplyLockedVisual()
	{
		// 未解锁时略暗，仍可见，方便玩家知道石堆下有东西
		SetMeshAlbedoAlpha(0.4f);
	}

	private void SetMeshAlbedoAlpha(float alpha)
	{
		foreach (Node child in GetChildren())
		{
			if (child is not MeshInstance3D mesh)
				continue;
			var mat = mesh.GetActiveMaterial(0)?.Duplicate() as StandardMaterial3D;
			if (mat == null)
				mat = new StandardMaterial3D();
			var c = mat.AlbedoColor;
			c.A = alpha;
			mat.AlbedoColor = c;
			mat.Transparency = alpha < 0.99f
				? BaseMaterial3D.TransparencyEnum.Alpha
				: BaseMaterial3D.TransparencyEnum.Disabled;
			mesh.SetSurfaceOverrideMaterial(0, mat);
		}
	}
}

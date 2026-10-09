using Godot;

/// <summary>玩法 HUD：任务栏、道具栏、准星。</summary>
public partial class HudController : CanvasLayer
{
	public static HudController Instance { get; private set; }

	private Label _taskLabel;
	private TextureRect _reticle;
	private TextureRect _slot1Icon;
	private TextureRect _slot2Icon;
	private Label _slot1Name;
	private Label _slot2Name;
	private Control _taskHint;
	private Control _itemSlots;
	private Control _crosshair;
	private Control _keyHints;
	private Control _interactHint;

	private Texture2D _reticleIdle;
	private Texture2D _reticleInteract;
	private Texture2D _slotDisabled;
	private Texture2D _slotNormal;
	private Texture2D _slotWandSelected;
	private Texture2D _slotPostcardSelected;
	private Texture2D _iconWand;
	private Texture2D _iconPostcard;

	private bool _aimingInteractable;
	private string _objective = "听完开场，观察附近的房门";

	public override void _Ready()
	{
		Instance = this;

		_taskHint = GetNode<Control>("TaskHint");
		_itemSlots = GetNode<Control>("ItemSlots");
		_crosshair = GetNode<Control>("Crosshair");
		_keyHints = GetNodeOrNull<Control>("KeyHints");
		_interactHint = GetNodeOrNull<Control>("InteractHint");
		_taskLabel = GetNode<Label>("TaskHint/Label");
		_reticle = GetNode<TextureRect>("Crosshair/Reticle");
		_slot1Icon = GetNode<TextureRect>("ItemSlots/Slot1/Icon");
		_slot2Icon = GetNode<TextureRect>("ItemSlots/Slot2/Icon");
		_slot1Name = GetNode<Label>("ItemSlots/Slot1/Name");
		_slot2Name = GetNode<Label>("ItemSlots/Slot2/Name");

		_reticleIdle = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/reticle_idle.png");
		_reticleInteract = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/reticle_interact.png");
		_slotDisabled = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/slot_disabled.png");
		_slotNormal = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/slot_normal.png");
		_slotWandSelected = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/slot_wand_selected.png");
		_slotPostcardSelected = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/slot_postcard_selected.png");
		_iconWand = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/icon_wand.png");
		_iconPostcard = GD.Load<Texture2D>("res://Art/PlayMenu/PNG/icon_postcard.png");

		GameEvents.ItemCollected += OnItemCollected;
		GameEvents.DoorUnlocked += OnDoorUnlocked;
		GameEvents.PuzzleCompleted += OnPuzzleCompleted;
		GameEvents.TriggerEntered += OnTriggerEntered;
		GameEvents.DialogStarted += OnDialogStarted;
		GameEvents.DialogEnded += OnDialogEnded;
		GameEvents.ObjectiveChanged += OnObjectiveChanged;
		GameEvents.EquippedChanged += OnEquippedChanged;

		_taskLabel.Text = _objective;
		RefreshInventory();
		_aimingInteractable = true; // force first apply
		SetAimingInteractable(false);
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;

		GameEvents.ItemCollected -= OnItemCollected;
		GameEvents.DoorUnlocked -= OnDoorUnlocked;
		GameEvents.PuzzleCompleted -= OnPuzzleCompleted;
		GameEvents.TriggerEntered -= OnTriggerEntered;
		GameEvents.DialogStarted -= OnDialogStarted;
		GameEvents.DialogEnded -= OnDialogEnded;
		GameEvents.ObjectiveChanged -= OnObjectiveChanged;
		GameEvents.EquippedChanged -= OnEquippedChanged;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (DialogueUI.IsOpen)
			return;
		if (@event is not InputEventKey key || !key.Pressed || key.Echo)
			return;

		var state = GameManager.Instance?.State;
		if (state == null)
			return;

		if (key.PhysicalKeycode == Key.Key1 && state.HasItem(ItemType.MagicWand))
			Equip(EquippedTool.MagicWand);
		else if (key.PhysicalKeycode == Key.Key2 && state.HasPostcard)
			Equip(EquippedTool.Postcard);
	}

	public void SetAimingInteractable(bool canInteract)
	{
		if (_aimingInteractable == canInteract && _reticle != null)
			return;
		_aimingInteractable = canInteract;
		if (_reticle == null)
			return;
		_reticle.Texture = canInteract ? _reticleInteract : _reticleIdle;
	}

	private void Equip(EquippedTool tool)
	{
		var state = GameManager.Instance?.State;
		if (state == null || state.Equipped == tool)
			return;
		state.Equipped = tool;
		GameEvents.EmitEquippedChanged(tool);
	}

	private void OnEquippedChanged(EquippedTool tool) => RefreshInventory();

	private void OnObjectiveChanged(string text)
	{
		_objective = text;
		if (_taskLabel != null)
			_taskLabel.Text = text;
	}

	private void OnItemCollected(ItemType type)
	{
		var state = GameManager.Instance?.State;
		if (state == null)
			return;

		if (type == ItemType.MagicWand)
		{
			state.Equipped = EquippedTool.MagicWand;
			GameEvents.EmitEquippedChanged(EquippedTool.MagicWand);
			SetObjective("用法杖移开挡路的地块，打通通道");
		}
		else if (type == ItemType.PostcardShard1 || type == ItemType.PostcardShard2 || type == ItemType.PostcardShard3)
		{
			if (state.Equipped == EquippedTool.None)
			{
				state.Equipped = EquippedTool.Postcard;
				GameEvents.EmitEquippedChanged(EquippedTool.Postcard);
			}
			SetObjective("前往密室中心，观察景观");
		}
		else if (type == ItemType.GravityBoots)
		{
			SetObjective("用重力切换靴，按下三个门锁按钮");
		}

		RefreshInventory();
	}

	private void OnDoorUnlocked()
	{
		SetObjective("推开房门，离开起始房间");
	}

	private void OnPuzzleCompleted(string puzzleId)
	{
		if (puzzleId == "tutorial_cover")
			SetObjective("穿过通道，前往上层空间");
		else if (puzzleId == "tutorial_thirds")
			SetObjective("拾取明信片碎片");
	}

	private void OnTriggerEntered(string triggerId)
	{
		switch (triggerId)
		{
			case "cover_block":
				SetObjective("拾取魔法棒，移开挡路的地块");
				break;
			case "rubble":
				SetObjective("移开石堆，查看下方物品");
				break;
			case "c_center":
				var state = GameManager.Instance?.State;
				if (state != null && state.HasPostcard)
					SetObjective("新手引导完成");
				break;
		}
	}

	private void OnDialogStarted(string dialogId)
	{
		SetExplorationHudVisible(false);

		// 对话节点与目标文案对齐（触发区/拾取已写过的会自然覆盖）
		switch (dialogId)
		{
			case "dialog_1":
				SetObjective("听完开场，观察附近的房门");
				break;
			case "dialog_5":
				SetObjective("拾取魔法棒，移开挡路的地块");
				break;
			case "dialog_6":
				SetObjective("用法杖移开挡路的地块，打通通道");
				break;
			case "dialog_7":
				SetObjective("穿过通道，前往上层空间");
				break;
			case "dialog_8":
				SetObjective("移开石堆，查看下方物品");
				break;
			case "dialog_9":
				SetObjective("拾取明信片碎片");
				break;
			case "dialog_10":
				SetObjective("前往密室中心，观察景观");
				break;
		}
	}

	private void OnDialogEnded(string dialogId)
	{
		SetExplorationHudVisible(true);
		if (dialogId == "dialog_1")
			SetObjective("靠近房门，抬头观察");
		else if (dialogId == "dialog_2")
			SetObjective("拾取附近的重力切换靴");
	}

	private void SetExplorationHudVisible(bool visible)
	{
		if (_taskHint != null)
			_taskHint.Visible = visible;
		if (_itemSlots != null)
			_itemSlots.Visible = visible;
		if (_crosshair != null)
			_crosshair.Visible = visible;
		if (_keyHints != null)
			_keyHints.Visible = visible;
		if (!visible && _interactHint != null)
			_interactHint.Visible = false;
	}

	private void SetObjective(string text)
	{
		if (_objective == text)
			return;
		GameEvents.EmitObjectiveChanged(text);
	}

	private void RefreshInventory()
	{
		var state = GameManager.Instance?.State;
		bool hasWand = state != null && state.HasItem(ItemType.MagicWand);
		bool hasCard = state != null && state.HasPostcard;
		var equipped = state?.Equipped ?? EquippedTool.None;

		ApplySlot(_slot1Icon, _slot1Name, hasWand, equipped == EquippedTool.MagicWand, _iconWand, _slotWandSelected);
		ApplySlot(_slot2Icon, _slot2Name, hasCard, equipped == EquippedTool.Postcard, _iconPostcard, _slotPostcardSelected);
	}

	private void ApplySlot(TextureRect icon, Label nameLabel, bool owned, bool selected, Texture2D itemIcon, Texture2D selectedSlot)
	{
		if (icon == null)
			return;

		if (!owned)
		{
			icon.Texture = _slotDisabled;
			if (nameLabel != null)
				nameLabel.Modulate = new Color(1, 1, 1, 0.35f);
			return;
		}

		if (nameLabel != null)
			nameLabel.Modulate = Colors.White;

		if (selected)
			icon.Texture = selectedSlot;
		else
			icon.Texture = itemIcon ?? _slotNormal;
	}
}

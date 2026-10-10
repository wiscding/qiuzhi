using Godot;

/// <summary>新手关对话触发编排（进入游戏、区域、道具、拼图、门）。</summary>
public partial class TutorialDirector : Node
{
	private bool _introPlayed;
	private bool _dialog2Played;
	private bool _dialog4Played;
	private bool _dialog5Played;
	private bool _dialog6Played;
	private bool _dialog7Played;
	private bool _dialog8Played;
	private bool _dialog9Played;
	private bool _dialog10Played;
	private bool _centerFinaleDone;
	private bool _enteredLayerB;
	private bool _dialog18Played;

	private Node3D _environment;

	public override void _Ready()
	{
		_environment = GetNodeOrNull<Node3D>("../Environment");
		GameEvents.TriggerEntered += OnTriggerEntered;
		GameEvents.ItemCollected += OnItemCollected;
		GameEvents.DoorButtonPressed += OnDoorButtonPressed;
		GameEvents.PuzzleCompleted += OnPuzzleCompleted;
		GameEvents.DialogRequested += OnDialogRequested;
		GameEvents.PlayerEnteredLayer += OnPlayerEnteredLayer;
		GameEvents.PostcardSightSucceeded += OnPostcardSightSucceeded;
		GameEvents.DialogEnded += OnDialogEnded;
		CallDeferred(MethodName.PlayIntro);
	}

	public override void _ExitTree()
	{
		GameEvents.TriggerEntered -= OnTriggerEntered;
		GameEvents.ItemCollected -= OnItemCollected;
		GameEvents.DoorButtonPressed -= OnDoorButtonPressed;
		GameEvents.PuzzleCompleted -= OnPuzzleCompleted;
		GameEvents.DialogRequested -= OnDialogRequested;
		GameEvents.PlayerEnteredLayer -= OnPlayerEnteredLayer;
		GameEvents.PostcardSightSucceeded -= OnPostcardSightSucceeded;
		GameEvents.DialogEnded -= OnDialogEnded;
	}

	private void PlayIntro()
	{
		if (_introPlayed)
			return;
		_introPlayed = true;
		GameEvents.EmitDialogRequested("dialog_1");
	}

	private void OnDialogRequested(string dialogId)
	{
		if (DialogueUI.Instance == null)
		{
			GD.PrintErr("[TutorialDirector] DialogueUI 未就绪");
			return;
		}
		DialogueUI.Instance.Play(dialogId);
	}

	private void OnTriggerEntered(string triggerId)
	{
		switch (triggerId)
		{
			case "door_hint":
				// 看向门后播对话 2；结束后附近重力靴才可捡
				if (!_dialog2Played)
				{
					_dialog2Played = true;
					LookThenDialog("dialog_2", ResolveLook("Door"));
				}
				break;
			case "cover_block":
				if (!_dialog5Played)
				{
					_dialog5Played = true;
					LookThenDialog("dialog_5", ResolveLook("MovableBlock"));
				}
				break;
			case "rubble":
				// 石堆区在 B 层半径内；层状态由 Player 半径判定维护
				if (!_dialog8Played)
				{
					_dialog8Played = true;
					LookThenDialog("dialog_8", ResolveLook("MovableBlock_Third"));
				}
				break;
			case "c_center":
				HandleCenterTrigger();
				break;
			case "intro":
				// 开场已由 PlayIntro 处理
				break;
		}
	}

	private void HandleCenterTrigger()
	{
		var state = GameManager.Instance?.State;
		if (state != null)
			state.CenterObserved = true;

		// 事件 5：有碎片后到圆心收尾（对话一览无「对话11」，只引导观察+目标文案）
		if (!_centerFinaleDone && state != null && state.HasPostcard)
		{
			_centerFinaleDone = true;
			GameEvents.EmitLookAtRequested(Vector3.Zero, 1.25f);
			CallDeferred(MethodName.DeferredCenterFinale);
		}
		else if (!_centerFinaleDone)
		{
			GameEvents.EmitLookAtRequested(Vector3.Zero, 0.9f);
		}
	}

	private async void DeferredCenterFinale()
	{
		await ToSignal(GetTree().CreateTimer(0.85), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree())
			return;
		GameEvents.EmitObjectiveChanged("新手引导完成 · 圆心举明信片右键对准拼图");
	}

	private void OnPostcardSightSucceeded()
	{
		if (_dialog18Played)
			return;
		_dialog18Played = true;
		GameEvents.EmitDialogRequested("dialog_18");
	}

	private void OnDialogEnded(string dialogId)
	{
		if (dialogId != "dialog_18")
			return;
		GameEvents.EmitObjectiveChanged("离开关卡结算");
		GD.Print("[TutorialDirector] 对话 18 结束 → 返回开始菜单结算");
		CallDeferred(MethodName.GoToSettlement);
	}

	private void GoToSettlement()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Error err = GetTree().ChangeSceneToFile("res://StartMenu.tscn");
		if (err != Error.Ok)
			GD.PrintErr($"[TutorialDirector] 返回开始菜单失败：{err}");
	}

	private async void LookThenDialog(string dialogId, Vector3? lookAt)
	{
		if (lookAt.HasValue)
			GameEvents.EmitLookAtRequested(lookAt.Value, 1.0f);

		await ToSignal(GetTree().CreateTimer(0.75), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree())
			return;
		GameEvents.EmitDialogRequested(dialogId);
	}

	private Vector3? ResolveLook(string nodeName)
	{
		if (_environment == null)
			return null;
		var node = _environment.GetNodeOrNull<Node3D>(nodeName);
		return node?.GlobalPosition;
	}

	private void OnPlayerEnteredLayer(GameLayer layer)
	{
		if (layer != GameLayer.LayerB || _enteredLayerB)
			return;
		_enteredLayerB = true;
		GD.Print("[TutorialDirector] 首次进入 B 层");
	}

	private void OnItemCollected(ItemType type)
	{
		if (type == ItemType.MagicWand && !_dialog6Played)
		{
			_dialog6Played = true;
			GameEvents.EmitDialogRequested("dialog_6");
		}
		else if (type == ItemType.PostcardShard1 && !_dialog10Played)
		{
			_dialog10Played = true;
			GameEvents.EmitDialogRequested("dialog_10");
		}
	}

	private void OnDoorButtonPressed(int index)
	{
		if (_dialog4Played)
			return;
		_dialog4Played = true;
		GameEvents.EmitDialogRequested("dialog_4");
	}

	private void OnPuzzleCompleted(string puzzleId)
	{
		if (puzzleId == "tutorial_cover" && !_dialog7Played)
		{
			_dialog7Played = true;
			GameEvents.EmitDialogRequested("dialog_7");
		}
		else if (puzzleId == "tutorial_thirds" && !_dialog9Played)
		{
			_dialog9Played = true;
			GameEvents.EmitDialogRequested("dialog_9");
		}
	}
}

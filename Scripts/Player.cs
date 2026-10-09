using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 6.0f;
	[Export] public float MouseSensitivity = 0.002f;

	//测试用拥有重力切换开关
	[Export] public bool HasGravitySwitch = false;

	//离球心多近算"在核心附近"（只用于让速度收敛，必须小于小球的内半径）
	[Export] public float HoverRadius = 1.5f;
	//在核心附近时速度的每秒衰减系数（越小停得越快）
	[Export] public float HoverDamping = 0.002f;
	//从球心沿视线射出去时的初速度
	[Export] public float LaunchSpeed = 6.0f;

	/// <summary>距球心小于此值视为 B 层（内壳；新手屋通道内侧）。</summary>
	[Export] public float LayerBMaxRadius = 10.0f;
	/// <summary>距球心大于此值视为 C 层（外壳；出生点/新手屋）。中间为滞回带。</summary>
	[Export] public float LayerCMinRadius = 10.5f;

	private bool isHovering = false;
	private GameLayer _currentLayer = GameLayer.LayerC;
	private bool _layerInitialized;

	private bool isCentrifugal = true;
	//private float yaw = 0f;
	private float pitch = 0f;
	private MovableBlock heldBlock = null;

	//GravityPivot 的重力翻转平滑
	//private float gravityPivotTargetX = 0f;
	//private bool isGravityRotating = false;

	//鼠标这一帧累积的"转身量"，在 _PhysicsProcess 里统一施加
	private float pendingYaw = 0f;

	//_Ready 里取一次，之后直接用字段
	//private Node3D gravityPivot;
	private Node3D head;
	private RayCast3D interactRay;
	private RayCast3D aimRay;
	private Label hintLabel;

	//当前射线瞄准的可交互物
	private Interactable targetInteractable = null;

	[Export] public float LookAssistSpeed = 3.2f;
	private Vector3? _lookAssistTarget;
	private float _lookAssistTime;

	public override void _Ready()
	{
		AddToGroup("player");
		//gravityPivot = GetNode<Node3D>("GravityPivot");
		head = GetNode<Node3D>("Head");
		interactRay = GetNode<RayCast3D>("Head/Camera3D/InteractRay");
		aimRay = GetNode<RayCast3D>("Head/Camera3D/AimRay");
		hintLabel = GetNode<Label>("../UI/InteractHint");
		HasGravitySwitch = GameManager.Instance != null &&
			GameManager.Instance.State.HasItem(ItemType.GravityBoots);
		GameEvents.ItemCollected += OnItemCollected;
		GameEvents.LookAtRequested += OnLookAtRequested;

		// 从开始菜单点进来时，按钮松手可能把鼠标模式顶掉；延后一帧再捕获
		CallDeferred(MethodName.CaptureMouseForLook);
		CallDeferred(MethodName.SnapSpawnToShell);
		CallDeferred(MethodName.InitLayerFromPosition);
	}

	public override void _ExitTree()
	{
		GameEvents.ItemCollected -= OnItemCollected;
		GameEvents.LookAtRequested -= OnLookAtRequested;
	}

	private void OnLookAtRequested(Vector3 worldPosition, float duration)
	{
		_lookAssistTarget = worldPosition;
		_lookAssistTime = Mathf.Max(0.2f, duration);
	}

	private void OnItemCollected(ItemType type)
	{
		if (type == ItemType.GravityBoots)
			HasGravitySwitch = true;
	}

	private void CaptureMouseForLook()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private void InitLayerFromPosition()
	{
		_layerInitialized = false;
		UpdateLayerFromPosition();
	}

	/// <summary>
	/// 双壳按距球心半径判定：C=外壳（大半径），B=内壳（小半径）。
	/// LayerBMaxRadius～LayerCMinRadius 为滞回带，避免通道附近来回抖。
	/// 球心悬停时保持上一层，不因半径≈0 误判。
	/// </summary>
	private void UpdateLayerFromPosition()
	{
		float r = GlobalPosition.Length();
		if (r < HoverRadius)
			return;

		GameLayer next = _currentLayer;
		if (r <= LayerBMaxRadius)
			next = GameLayer.LayerB;
		else if (r >= LayerCMinRadius)
			next = GameLayer.LayerC;

		if (_layerInitialized && next == _currentLayer)
			return;

		_currentLayer = next;
		_layerInitialized = true;
		GameEvents.EmitPlayerEnteredLayer(next);
	}

	/// <summary>出生点靠近新手屋时，沿径向贴到球壳内侧，减少穿模。</summary>
	private void SnapSpawnToShell()
	{
		Vector3 outward = GlobalPosition.LengthSquared() > 0.01f
			? GlobalPosition.Normalized()
			: Vector3.Down;
		var space = GetWorld3D().DirectSpaceState;
		var query = PhysicsRayQueryParameters3D.Create(outward * 7f, outward * 16f);
		query.CollideWithAreas = false;
		query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		var hit = space.IntersectRay(query);
		if (hit.Count == 0)
			return;

		Vector3 point = hit["position"].AsVector3();
		Vector3 normal = hit["normal"].AsVector3();
		// 站在内侧：沿法线（朝球心一侧）抬起约胶囊半高
		GlobalPosition = point + normal.Normalized() * 1.05f;
		GD.Print($"[Player] 出生贴壳 → {GlobalPosition}");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (DialogueUI.IsOpen)
		{
			Velocity = Vector3.Zero;
			hintLabel.Visible = false;
			HudController.Instance?.SetAimingInteractable(false);
			return;
		}

		bool isAiming = Input.IsActionPressed("aim");
		Vector3 up = GetUpVector();

		float centerDistance = GlobalPosition.Length();
		bool wasHovering = isHovering;
		isHovering = centerDistance < HoverRadius;

		//能不能操控移动：只看"脚底下有没有东西"。
		//在空中（切换过程中、球心悬停）不能移动，落定之后恢复
		bool canMove = IsOnFloor();

		//把"朝向"重新投影到当前切平面，重建正交基
		Basis b = GlobalTransform.Basis;
		if (!isHovering)
		{
			Vector3 forward = -b.Z;
			forward -= up * forward.Dot(up);
			if (forward.LengthSquared() < 0.0001f)
				forward = up.Cross(Vector3.Right);
			forward = forward.Normalized();
			b = new Basis(forward.Cross(up), up, -forward);
		}
		
		//悬停时保留当前朝向；转身轴换成"当前基的 Y 轴"——它是最后一次有效重建出来的上方向
		Vector3 turnAxis = isHovering ? b.Y : up;

		//触发区视线引导：优先于鼠标累积量
		if (_lookAssistTarget.HasValue && _lookAssistTime > 0f)
		{
			_lookAssistTime -= (float)delta;
			ApplyLookAssist(ref b, up, (float)delta);
			pendingYaw = 0f;
			if (_lookAssistTime <= 0f)
				_lookAssistTarget = null;
		}
		else if (pendingYaw != 0f)
		{
			//这一帧累积的鼠标转身量，绕本地上轴施加
			b = b.Rotated(turnAxis, pendingYaw);
			pendingYaw = 0f;
		}
		GlobalTransform = new Transform3D(b, GlobalPosition);

		//头部俯仰
		head.Rotation = new Vector3(pitch, 0, 0);

		//移动,本地 XZ 平面现在就是切平面
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		if (canMove)
		{
			Vector3 direction = (GlobalTransform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
			if (direction != Vector3.Zero)
				Velocity = direction * Speed;
			else
				Velocity = up * Velocity.Dot(up);
		}

		//重力永远和上相反
		float gravity = 10f;
		Velocity -= up * gravity * (float)delta;

		//在球心附近强阻尼，把向心的来回摆动收敛成"停住"
		if (isHovering && !isCentrifugal)
			Velocity *= Mathf.Pow(HoverDamping, (float)delta);

		//切换重力
		if (Input.IsActionJustPressed("toggle_gravity"))
		{
			if (!HasGravitySwitch)
			{
				GD.Print("[Player] 重力切换能力未开启（检查器里的 HasGravitySwitch）");
			}
			else
			{
				isCentrifugal = !isCentrifugal;

				//按 E 只保留"沿新重力方向"的速度分量，横向速度丢掉。
				//无论在球壳上按还是在空中按，行为完全一致：
				//站在球壳上走着按 → 速度全是横向的 → 清零 → 笔直朝新重力方向掉
				//在空中朝球心掉着按 → 速度正好沿新轴 → 原样保留 → 靠新重力减速、反向
				Vector3 newUp = GetUpVector();
				Velocity = newUp * Velocity.Dot(newUp);

				GameEvents.EmitGravityChanged(isCentrifugal ? GravityMode.Centrifugal : GravityMode.Centripetal);

				//只有"从球心附近"切出去时径向才是零向量、没有方向可循，这时用视线补方向。
				//从球心朝哪射就落在哪个方向的球壳上 —— 策划说的"视角看向哪里就往哪里落"
				if (isCentrifugal && wasHovering)
					Velocity = -head.GlobalTransform.Basis.Z * LaunchSpeed;
			}
		}

		if (!isHovering)
			UpDirection = up;

		MoveAndSlide();
		UpdateLayerFromPosition();

		//瞄准检测（门本体射线会先打到门碰撞，再在同级找 DoorHandle）
		Interactable hit = null;
		if (interactRay.IsColliding())
			hit = FindInteractable(interactRay.GetCollider() as Node);
		targetInteractable = hit;

		if (targetInteractable != null)
		{
			hintLabel.Text = targetInteractable.GetPrompt();
			hintLabel.Visible = true;
		}
		else
		{
			hintLabel.Visible = false;
		}

		HudController.Instance?.SetAimingInteractable(targetInteractable != null);

		if (Input.IsActionJustPressed("place") && isAiming)
		{
			if (!GameManager.Instance.State.HasItem(ItemType.MagicWand))
				GD.Print("[Player] 还没有魔法棒，无法操作地块");
			else if (heldBlock == null)
				TryGrabBlock();
			else
				TryPlaceBlock();
		}

		//抓在手里的地块跟随玩家
		if (heldBlock != null)
			heldBlock.GlobalPosition = head.GlobalPosition + (-head.GlobalTransform.Basis.Z) * 2.5f;

		if (Input.IsActionJustPressed("interact") && targetInteractable != null)
			targetInteractable.Interact();
	}

	public override void _Input(InputEvent @event)
	{
		// 用 _Input 而不是 _UnhandledInput：HUD（准星等）在屏幕中心会吞掉鼠标事件
		if (DialogueUI.IsOpen)
			return;
		if (Input.MouseMode != Input.MouseModeEnum.Captured)
			return;
		if (@event is not InputEventMouseMotion motion)
			return;

		pendingYaw -= motion.Relative.X * MouseSensitivity;
		pitch -= motion.Relative.Y * MouseSensitivity;
		pitch = Mathf.Clamp(pitch, -1.5f, 1.5f);
	}

	/*private Vector3 GetGravityDirection()
	{
		return isCentrifugal ? Vector3.Down : Vector3.Up;
	}*/

	private void TryGrabBlock()
	{
		if (!aimRay.IsColliding())
			return;
		if (aimRay.GetCollider() is MovableBlock block)
		{
			//缺口空出来，并通知总线这块离开了
			if (block.CurrentSlot != null)
			{
				block.CurrentSlot.Clear();
				GameEvents.EmitBlockMoved(block.BlockIndex, false);
			}
			heldBlock = block;
			heldBlock.IsHeld = true;
			heldBlock.SetCollisionEnabled(false);
			GD.Print($"[Player] 抓起地块 {block.BlockIndex}");
		}
	}

	private void TryPlaceBlock()
	{
		if (!aimRay.IsColliding())
		{
			GD.Print("[Player] 放置失败：射线没打到任何东西");
			return;
		}

		if (aimRay.GetCollider() is not BlockSlot hitSlot)
		{
			GD.Print($"[Player] 放置失败：瞄准到的是 {(aimRay.GetCollider() as Node)?.Name}");
			return;
		}

		// 石堆三槽常叠在同一洞口：射线可能打到错误编号，按 BlockIndex 找同组可收的空槽
		BlockSlot slot = ResolvePlaceSlot(hitSlot, heldBlock);
		if (slot == null || !slot.Accepts(heldBlock))
		{
			GD.Print($"[Player] 放置失败：缺口只要 {hitSlot.RequiredBlockIndex}，手里是 {heldBlock.BlockIndex}");
			return;
		}

		slot.Place(heldBlock);
		GameEvents.EmitBlockMoved(heldBlock.BlockIndex, true);
		GD.Print($"[Player] 放置地块 {heldBlock.BlockIndex} → {slot.Name}（Required={slot.RequiredBlockIndex}）");
		heldBlock = null;
	}

	/// <summary>编号对齐即可放置；叠槽时优先落到 RequiredBlockIndex == BlockIndex 的空缺口。</summary>
	private static BlockSlot ResolvePlaceSlot(BlockSlot hit, MovableBlock block)
	{
		if (hit == null || block == null)
			return null;
		if (hit.Accepts(block))
			return hit;

		if (string.IsNullOrEmpty(hit.PuzzleGroup))
			return null;

		const float maxDistSq = 0.6f * 0.6f;
		BlockSlot best = null;
		float bestDist = maxDistSq;
		foreach (Node node in hit.GetTree().GetNodesInGroup("block_slot"))
		{
			if (node is not BlockSlot slot)
				continue;
			if (slot.PuzzleGroup != hit.PuzzleGroup)
				continue;
			if (!slot.Accepts(block))
				continue;
			float d = slot.GlobalPosition.DistanceSquaredTo(hit.GlobalPosition);
			if (d > bestDist)
				continue;
			bestDist = d;
			best = slot;
		}
		return best;
	}

	private static Interactable FindInteractable(Node node)
	{
		if (node is Interactable direct && direct.CanInteract())
			return direct;
		if (node == null)
			return null;
		Node parent = node.GetParent();
		if (parent == null)
			return null;
		foreach (Node child in parent.GetChildren())
		{
			if (child is Interactable sibling && sibling.CanInteract())
				return sibling;
		}
		return null;
	}

	//玩家相对球心的方向
	private Vector3 RadialOut()
	{
		return GlobalPosition.Normalized();
	}

	private Vector3 GetUpVector()
	{
		return isCentrifugal ? -RadialOut() : RadialOut();
	}

	private void ApplyLookAssist(ref Basis b, Vector3 up, float delta)
	{
		if (!_lookAssistTarget.HasValue)
			return;

		Vector3 eye = head.GlobalPosition;
		Vector3 to = _lookAssistTarget.Value - eye;
		if (to.LengthSquared() < 0.0001f)
			return;

		Vector3 desired = to.Normalized();
		Vector3 flatDesired = desired - up * desired.Dot(up);
		Vector3 flatCurrent = -b.Z;
		flatCurrent -= up * flatCurrent.Dot(up);
		if (flatDesired.LengthSquared() > 0.0001f && flatCurrent.LengthSquared() > 0.0001f)
		{
			flatDesired = flatDesired.Normalized();
			flatCurrent = flatCurrent.Normalized();
			float ang = Mathf.Atan2(flatCurrent.Cross(flatDesired).Dot(up), flatCurrent.Dot(flatDesired));
			float step = Mathf.Clamp(ang, -LookAssistSpeed * delta, LookAssistSpeed * delta);
			b = b.Rotated(up, step);
		}

		// 相对切平面的仰角 → 头部俯仰（负值抬头）
		float elev = Mathf.Asin(Mathf.Clamp(desired.Dot(up), -1f, 1f));
		float targetPitch = Mathf.Clamp(-elev, -1.5f, 1.5f);
		pitch = Mathf.MoveToward(pitch, targetPitch, LookAssistSpeed * delta);
	}
}

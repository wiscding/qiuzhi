using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 6.0f;
	[Export] public float MouseSensitivity = 0.002f;

	//测试用拥有重力切换开关
	[Export] public bool HasGravitySwitch = true;

	//离球心多近算"在核心附近"（只用于让速度收敛，必须小于小球的内半径）
	[Export] public float HoverRadius = 1.5f;
	//在核心附近时速度的每秒衰减系数（越小停得越快）
	[Export] public float HoverDamping = 0.002f;
	//从球心沿视线射出去时的初速度
	[Export] public float LaunchSpeed = 6.0f;

	private bool isHovering = false;

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

	public override void _Ready()
	{
		//gravityPivot = GetNode<Node3D>("GravityPivot");
		head = GetNode<Node3D>("Head");
		interactRay = GetNode<RayCast3D>("Head/Camera3D/InteractRay");
		aimRay = GetNode<RayCast3D>("Head/Camera3D/AimRay");
		hintLabel = GetNode<Label>("../UI/InteractHint");

		// 从开始菜单点进来时，按钮松手可能把鼠标模式顶掉；延后一帧再捕获
		CallDeferred(MethodName.CaptureMouseForLook);
	}

	private void CaptureMouseForLook()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(double delta)
	{
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

		//这一帧累积的鼠标转身量，绕本地上轴施加
		if (pendingYaw != 0f)
		{
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

		//瞄准检测
		Interactable hit = null;
		if (interactRay.IsColliding() && interactRay.GetCollider() is Interactable inter && inter.CanInteract())
			hit = inter;
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
				block.CurrentSlot.IsOccupied = false;
				block.CurrentSlot = null;
				GameEvents.EmitBlockMoved(block.BlockIndex, false);
			}
			heldBlock = block;
			heldBlock.IsHeld = true;
			heldBlock.SetCollisionEnabled(false);
			GD.Print("[Player] 抓起地块");
		}
	}

	private void TryPlaceBlock()
	{
		if (aimRay.IsColliding() && aimRay.GetCollider() is BlockSlot slot && !slot.IsOccupied)
		{
			heldBlock.GlobalPosition = slot.GlobalPosition;   //坐标吸附
			heldBlock.SetCollisionEnabled(true);              //恢复碰撞
			heldBlock.IsHeld = false;
			slot.IsOccupied = true;                           //缺口被占
			heldBlock.CurrentSlot = slot;                     //双向绑定
			GameEvents.EmitBlockMoved(heldBlock.BlockIndex, true);
			heldBlock = null;                                 //松手
			GD.Print("[Player] 放置地块");
		}
		else
		{
			if (aimRay.IsColliding())
				GD.Print($"[Player] 放置失败：瞄准到的是 {(aimRay.GetCollider() as Node)?.Name}");
			else
				GD.Print("[Player] 放置失败：射线没打到任何东西");
		}
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
}

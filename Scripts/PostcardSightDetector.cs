using Godot;

/// <summary>
/// 明信片视线检测：玩家处于 TriggerZone5（c_center）内时生效。
/// 成功条件：
/// 1) 视线与拼图平面法线夹角 ≤ MaxAngleDegrees
/// 2) 手持明信片并长按右键瞄准
/// 3) RequiredPuzzleGroup 已拼齐
/// </summary>
public partial class PostcardSightDetector : Node
{
	[Export] public NodePath PlanePath = new("../../Environment/CentripetalPattern");
	[Export] public NodePath CenterZonePath = new("../../Environment/TriggerZone5");
	[Export] public string RequiredPuzzleGroup = "tutorial_thirds";
	[Export] public float MaxAngleDegrees = 10f;
	[Export] public float HoldSeconds = 0.65f;

	private Node3D _plane;
	private Area3D _centerZone;
	private Camera3D _camera;
	private bool _fired;
	private float _hold;
	private bool _hintShown;

	public override void _Ready()
	{
		CallDeferred(MethodName.ResolveRefs);
		SetProcess(true);
	}

	private void ResolveRefs()
	{
		_plane = GetNodeOrNull<Node3D>(PlanePath);
		if (_plane == null)
			GD.PrintErr($"[PostcardSightDetector] 找不到拼图平面：{PlanePath}");

		_centerZone = GetNodeOrNull<Area3D>(CenterZonePath);
		if (_centerZone == null)
			GD.PrintErr($"[PostcardSightDetector] 找不到圆心触发区：{CenterZonePath}");

		var player = GetTree().GetFirstNodeInGroup("player") as Node;
		_camera = player?.GetNodeOrNull<Camera3D>("Head/Camera3D");
		if (_camera == null)
			GD.PrintErr("[PostcardSightDetector] 找不到玩家相机");
	}

	public override void _Process(double delta)
	{
		if (_fired || DialogueUI.IsOpen)
		{
			_hold = 0f;
			return;
		}

		if (_plane == null || _camera == null || _centerZone == null)
			ResolveRefs();
		if (_plane == null || _camera == null || _centerZone == null)
			return;

		if (!TryEvaluate(out float angleDeg, out string failReason))
		{
			_hold = 0f;
			if (!_hintShown && failReason == "ready_hint")
			{
				_hintShown = true;
				GameEvents.EmitObjectiveChanged("圆心举明信片（按2），长按右键对准拼图平面");
			}
			return;
		}

		_hold += (float)delta;
		if (_hold < HoldSeconds)
			return;

		_fired = true;
		_hold = 0f;
		GD.Print($"[PostcardSightDetector] 判定成功（TriggerZone5 内，夹角 {angleDeg:F1}°）→ 对话 18");
		GameEvents.EmitPostcardSightSucceeded();
	}

	private bool TryEvaluate(out float angleDeg, out string failReason)
	{
		angleDeg = 999f;
		failReason = "";

		var state = GameManager.Instance?.State;
		if (state == null)
		{
			failReason = "no_state";
			return false;
		}

		var player = GetTree().GetFirstNodeInGroup("player") as Node3D;
		if (player == null)
		{
			failReason = "no_player";
			return false;
		}

		// 圆心判定：必须在 TriggerZone5 重叠区内
		if (!_centerZone.OverlapsBody(player))
		{
			failReason = "not_center";
			return false;
		}

		bool puzzleDone = state.CompletedPuzzles.Contains(RequiredPuzzleGroup);
		bool hasCard = state.HasPostcard;
		if (!puzzleDone || !hasCard)
		{
			failReason = "not_ready";
			return false;
		}

		bool aiming = Input.IsActionPressed("aim");
		bool holdingCard = state.Equipped == EquippedTool.Postcard;
		if (!holdingCard || !aiming)
		{
			failReason = "ready_hint";
			return false;
		}

		Vector3 look = -_camera.GlobalTransform.Basis.Z.Normalized();
		Vector3 normal = ResolvePlaneNormalTowardCenter(_plane);
		float dot = Mathf.Clamp(look.Dot(normal), -1f, 1f);
		angleDeg = Mathf.RadToDeg(Mathf.Acos(dot));
		if (angleDeg > MaxAngleDegrees)
		{
			failReason = "angle";
			return false;
		}

		return true;
	}

	private static Vector3 ResolvePlaneNormalTowardCenter(Node3D plane)
	{
		Vector3 toCenter = -plane.GlobalPosition;
		if (toCenter.LengthSquared() < 0.0001f)
			toCenter = Vector3.Up;
		toCenter = toCenter.Normalized();

		Vector3 z = plane.GlobalTransform.Basis.Z.Normalized();
		Vector3 y = plane.GlobalTransform.Basis.Y.Normalized();
		Vector3 normal = Mathf.Abs(z.Dot(toCenter)) >= Mathf.Abs(y.Dot(toCenter)) ? z : y;
		if (normal.Dot(toCenter) < 0f)
			normal = -normal;
		return normal;
	}
}

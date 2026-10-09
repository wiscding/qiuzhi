using Godot;

/// <summary>
/// 给导入的 GLB 网格生成静态三角网格碰撞。球壳内侧行走需要背面碰撞。
/// </summary>
public partial class SceneMeshCollision : Node
{
	[Export] public NodePath MeshRoot = new("双球和新手关1");

	public override void _Ready()
	{
		Node root = GetParent()?.GetNodeOrNull(MeshRoot) ?? GetParent();
		if (root == null)
		{
			GD.PrintErr("[SceneMeshCollision] 找不到网格根节点");
			return;
		}

		int count = 0;
		Bake(root, ref count);
		GD.Print($"[SceneMeshCollision] 已为 {count} 个网格生成碰撞");
	}

	private static void Bake(Node node, ref int count)
	{
		foreach (Node child in node.GetChildren())
			Bake(child, ref count);

		if (node is not MeshInstance3D meshInstance || meshInstance.Mesh == null)
			return;
		if (meshInstance.GetNodeOrNull<StaticBody3D>("ImportedTrimeshBody") != null)
			return;

		ConcavePolygonShape3D shape = meshInstance.Mesh.CreateTrimeshShape();
		if (shape == null)
			return;

		shape.BackfaceCollision = true;

		var body = new StaticBody3D { Name = "ImportedTrimeshBody" };
		var collision = new CollisionShape3D { Name = "CollisionShape3D", Shape = shape };
		meshInstance.AddChild(body);
		body.AddChild(collision);
		count++;
	}
}

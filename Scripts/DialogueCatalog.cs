using System.Collections.Generic;
using Godot;

/// <summary>对话一览里的台词与分镜。路径对应 Art/Dialog。</summary>
public static class DialogueCatalog
{
	private const string Stage = "res://Art/Dialog/Stage/对话01_开场谈话_观察室_环境底层.png";
	private const string DocWelcome = "res://Art/Dialog/Portrait/对话01_博士_迎接学生_人物层.png";
	private const string DocTheory = "res://Art/Dialog/Portrait/对话01_博士_讲述观察理论_人物层.png";
	private const string DocRecall = "res://Art/Dialog/Portrait/对话01_博士_回忆前三位学生_人物层.png";
	private const string DocPressure = "res://Art/Dialog/Portrait/对话01_博士_温柔施压_人物层.png";
	private const string DocBack = "res://Art/Dialog/Portrait/对话01_博士_背身托付走出这里_人物层.png";
	private const string DocStand = "res://Art/Dialog/Portrait/对话01_博士_平静站立_人物层.png";
	private const string MeAsk = "res://Art/Dialog/Portrait/对话01_主角_委屈质问老师_人物层.png";
	private const string MeResolve = "res://Art/Dialog/Portrait/对话01与06_主角_下定决心尝试_人物层.png";
	private const string MeLook = "res://Art/Dialog/Portrait/对话02至04_主角_抬头观察门与按钮_人物层.png";
	private const string MeThink = "res://Art/Dialog/Portrait/对话05与08至10_主角_观察线索认真思考_人物层.png";
	private const string MeHappy = "res://Art/Dialog/Portrait/对话07_主角_通路打开短暂欣喜_人物层.png";
	private const string MeEnd = "res://Art/Dialog/Portrait/对话18_主角_认出教室释然回望_人物层.png";
	private const string AvMe = "res://Art/Dialog/UI/对话01_主角小头像_透明内容层.png";
	private const string AvDoc = "res://Art/Dialog/UI/对话01_博士小头像_透明内容层.png";

	public static bool TryGet(string dialogId, out List<DialogueLine> lines)
	{
		lines = Build(dialogId);
		return lines != null && lines.Count > 0;
	}

	private static List<DialogueLine> Build(string id)
	{
		return id switch
		{
			"dialog_1" => Dialog1(),
			"dialog_2" => OverlayMe(MeLook,
				"门，为什么开在天花板上？\n这里怎么会有双靴子，「重力转换靴」……不管了，试着用一用吧。"),
			"dialog_3" => OverlayMe(MeLook,
				"门没有把手，旁边倒有个按钮——也许这就是开门的办法。"),
			"dialog_4" => OverlayMe(MeLook,
				"只亮了一格——还差两个，而那两个从这儿根本够不到……哈，看来小靴子不是一次性道具。"),
			"dialog_5" => OverlayMe(MeThink,
				"通道就在那儿，被一块地块盖住了；旁边空出来的那个凹槽，大概就是它该去的地方。\n看看周围有什么能用的工具吧。"),
			"dialog_6" => OverlayMe(MeResolve,
				"按住右键瞄准那块盖住通道的地块，再按左键放下——老师说观察者能改动世界，那就让我试试。"),
			"dialog_7" => OverlayMe(MeHappy,
				"通了！不过，上面似乎还有一层？"),
			"dialog_8" => OverlayMe(MeThink,
				"出口被堵死了，堵得还挺讲究。\n前面那堆小地块……底下似乎压着东西？"),
			"dialog_9" => OverlayMe(MeThink,
				"拼上去之后，从里往外看竟然浮出了纹路——只有在一种重力状态下才有。为什么换个方向看，它才肯显形？\n先记下这件事。这种特性，后面一定用得上。"),
			"dialog_10" => Dialog10(),
			"dialog_18" => OverlayMe(MeEnd,
				"哈\n本以为会是什么宏大的课题，结果……\n居然只是当年授课的教室吗\n……\n我该回去了，也该去再见见你，老师。"),
			_ => null,
		};
	}

	private const string PostcardFront = "res://Art/Pentagon/puzzle_01.png";
	private const string PostcardFrame = "res://Art/Dialog/Postcard/postcard_frame.png";
	private const string PostcardIcon = "res://Art/Dialog/Postcard/icon_postcard.png";

	private static List<DialogueLine> Dialog10()
	{
		string front = FirstExisting(PostcardFront, PostcardFrame, PostcardIcon);
		string hole = FirstExisting(PostcardFrame, PostcardIcon);
		return new List<DialogueLine>
		{
			new("我", "第四位学生",
				"这张明信片，中间有个五边形的洞？",
				DialogueLayout.Overlay, MeThink, AvMe, true),
			new("", "",
				"（观察明信片正面）",
				DialogueLayout.FullscreenImage, front),
			new("我", "第四位学生",
				"图片的边缘……像是一间教室？",
				DialogueLayout.Overlay, MeThink, AvMe, true),
			new("", "",
				"（五边形挖空）",
				DialogueLayout.FullscreenImage, hole),
			new("", "",
				"唯有身处风暴的中心，才得以窥见流动的真实。\n\n「如果你能看到这张明信片的全貌，你就自由了」。",
				DialogueLayout.CardBack),
			new("我", "第四位学生",
				"「风暴的中心」……是指这个密室的中心吗？要看到全貌，就得站到他说的那个位置去。这里看不全。我上去。",
				DialogueLayout.Overlay, MeThink, AvMe, true),
		};
	}

	private static string FirstExisting(params string[] paths)
	{
		foreach (string path in paths)
		{
			if (ResourceLoaderExists(path))
				return path;
		}
		return paths.Length > 0 ? paths[^1] : "";
	}

	private static bool ResourceLoaderExists(string path) => ResourceLoader.Exists(path);

	private static List<DialogueLine> OverlayMe(string portrait, string text)
	{
		return new List<DialogueLine>
		{
			new("我", "第四位学生", text, DialogueLayout.Overlay, portrait, AvMe, true),
		};
	}

	private static List<DialogueLine> Dialog1()
	{
		return new List<DialogueLine>
		{
			new("老博士", "空间研究者",
				"你来晚了。不过没关系，我这里从来不看早晚。\n坐吧——如果你还找得到可以坐的地方。",
				DialogueLayout.DoctorCenter, DocWelcome),
			new("老博士", "空间研究者",
				"我教了一辈子空间。到最后我才明白，我教的其实是“观察”。你怎么看一个东西，比那个东西是什么，要紧得多。所以我不再讲课了，我造了这个地方。它比任何一间教室都诚实。",
				DialogueLayout.DoctorCenter, DocTheory),
			new("老博士", "空间研究者",
				"你是第四个来到这里的人。\n前三个也都是我的学生。第一个最有天赋，我一度以为他能替我算出答案；第二个最守规矩，他把每一步都做到了；第三个，他几乎看见了。",
				DialogueLayout.DoctorCenter, DocRecall),
			new("老博士", "空间研究者",
				"他们现在都不在了。但我不会告诉你他们错在哪里——告诉你了，这个实验就没有意义了。",
				DialogueLayout.DoctorCenter, DocPressure),
			new("老博士", "空间研究者",
				"替我做一件事：走出这里。顺便，替我证明那句话是对的——你以为的矛盾，只是你站错了地方。",
				DialogueLayout.DoctorBack, DocBack),
			new("老博士", "",
				"别怕，你们这些学生是我最宝贵的财富。\n老师不会害你的，老师怎么会害你呢？",
				DialogueLayout.BlackCenter),
			new("我", "第四位学生",
				"（又是这种话）\n为什么第四个偏偏是我呢？教授，我不是您最喜欢的学生吗，我的前途明明那么璀璨，为什么要我来做这只小白鼠？",
				DialogueLayout.PlayerAsk, DocStand, AvMe, true),
			new("我", "第四位学生",
				"……\n——算了，先出去吧。别的等出去再说。",
				DialogueLayout.PlayerCenter, MeResolve, AvMe, true),
		};
	}

	public static string StageBackgroundPath => Stage;
}

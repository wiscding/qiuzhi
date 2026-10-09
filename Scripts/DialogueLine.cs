public sealed class DialogueLine
{
	public string Speaker = "";
	public string Title = "";
	public string Text = "";
	public DialogueLayout Layout = DialogueLayout.Overlay;
	public string PortraitPath = "";
	public string AvatarPath = "";
	public bool ShowAvatar;

	public DialogueLine()
	{
	}

	public DialogueLine(string speaker, string title, string text, DialogueLayout layout,
		string portraitPath = "", string avatarPath = "", bool showAvatar = false)
	{
		Speaker = speaker;
		Title = title;
		Text = text;
		Layout = layout;
		PortraitPath = portraitPath;
		AvatarPath = avatarPath;
		ShowAvatar = showAvatar;
	}
}

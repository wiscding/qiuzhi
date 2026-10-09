public enum DialogueLayout
{
	/// <summary>叠在 3D 游戏画面上，只有底部对话条。</summary>
	Overlay,
	/// <summary>观察室底 + 博士居中立绘。</summary>
	DoctorCenter,
	/// <summary>观察室底 + 博士背影。</summary>
	DoctorBack,
	/// <summary>黑屏居中白字。</summary>
	BlackCenter,
	/// <summary>观察室底 + 右侧博士 + 左下主角小头像。</summary>
	PlayerAsk,
	/// <summary>观察室底 + 主角居中立绘。</summary>
	PlayerCenter,
	/// <summary>全屏明信片/插图（PortraitPath 为图）。</summary>
	FullscreenImage,
	/// <summary>白底居中手写字（明信片背面）。</summary>
	CardBack,
}

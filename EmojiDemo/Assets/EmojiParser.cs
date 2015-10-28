using FairyGUI;
using FairyGUI.Utils;

public class EmojiParser : UBBParser
{
	static EmojiParser _instance;
	public new static EmojiParser inst
	{
		get
		{
			if (_instance == null)
				_instance = new EmojiParser();
			return _instance;
		}
	}

	private static string[] TAGS = new string[]
		{ "88","am","bs","bz","ch","cool","dhq","dn","fd","gz","han","hx","hxiao","hxiu" };
	public EmojiParser ()
	{
		foreach (string ss in TAGS)
		{
			this.handlers[":"+ss.ToUpper()] = OnTag_Emoji;
		}
	}

	string OnTag_Emoji(string tagName, bool end, string attr)
	{
		return "<img src='" + UIPackage.GetItemURL("Demo", tagName.Substring(1).ToLower()) + "'/>";
	}
}
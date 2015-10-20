namespace FairyGUI
{
	public interface IMobileInputAdapter
	{
		string GetInput();
		void OpenKeyboard(string text, int keyboardType, bool autocorrection, bool multiline, bool secure, bool alert, string textPlaceholder);
		void CloseKeyboard();
	}
}

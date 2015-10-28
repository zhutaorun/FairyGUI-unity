
namespace FairyGUI
{
	public class GTextInput : GTextField
	{
		public GTextInput()
		{
			_textField.autoSize = false;
			_textField.wordWrap = false;
			_textField.onChanged.AddCapture(__onChanged);
		}

		public bool editable
		{
			get
			{
				return _textField.input;
			}
			set
			{
				_textField.input = false;
			}
		}
		public int maxLength
		{
			get
			{
				return _textField.maxLength;
			}
			set
			{
				_textField.maxLength = value;
			}
		}

		public int caretPosition
		{
			get { return _textField.caretPosition; }
			set { _textField.caretPosition = value; }
		}

		public void ReplaceSelection(string value)
		{
			_textField.ReplaceSelection(value);
		}

		override protected void CreateDisplayObject()
		{
			base.CreateDisplayObject();

			_textField.input = true;
		}

		void __onChanged(EventContext context)
		{
			UpdateSize();
		}
	}
}
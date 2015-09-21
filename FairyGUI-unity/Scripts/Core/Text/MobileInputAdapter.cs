using UnityEngine;

namespace FairyGUI
{
    public class MobileInputAdapter : IMobileInputAdapter
    {
        TouchScreenKeyboard _keyboard;

        public string GetInput()
        {
            if (_keyboard != null)
            {
                string s = _keyboard.text;

                if (_keyboard.done)
                    _keyboard = null;

                return s;
            }
            else
                return null;
        }

        public void OpenKeyboard(string text, int keyboardType, bool autocorrection, bool multiline, bool secure, bool alert, string textPlaceholder)
        {
            if (_keyboard != null)
                return;

            _keyboard = TouchScreenKeyboard.Open(text, (TouchScreenKeyboardType)keyboardType, autocorrection, multiline, secure, alert, textPlaceholder);
        }

        public void CloseKeyboard()
        {
            if (_keyboard != null)
            {
                _keyboard.active = false;
                _keyboard = null;
            }
        }
    }
}

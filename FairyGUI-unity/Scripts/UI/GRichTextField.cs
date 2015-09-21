using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    public class GRichTextField : GTextField
    {
        RichTextField _richTextField;

        public GRichTextField()
            : base()
        {
        }

        override protected void CreateDisplayObject()
        {
            _richTextField = new RichTextField();
            _richTextField.gOwner = this;
            displayObject = _richTextField;
        }

        override public string text
        {
            set
            {
                if (value == null)
                    value = "";
                _richTextField.width = this.width * GRoot.contentScaleFactor;
                if (_ubbEnabled)
                    _richTextField.htmlText = UBBParser.inst.Parse(value);
                else
                    _richTextField.htmlText = value;
                UpdateSize();
            }

            get
            {
                return _richTextField.text;
            }
        }

        override public bool displayAsPassword
        {
            get { return false; }
            set { }
        }

        override protected void UpdateAutoSize()
        {
            if (_widthAutoSize)
            {
                _richTextField.autoSize = true;
                _richTextField.wordWrap = false;
            }
            else
            {
                _richTextField.autoSize = false;
                _richTextField.wordWrap = true;
            }
            if (!underConstruct)
                UpdateSize();
        }

        private void UpdateSize()
        {
            if (_updatingSize)
                return;

            _updatingSize = true;

            _textWidth = Mathf.CeilToInt(_richTextField.textWidth);
            _textHeight = Mathf.CeilToInt(_richTextField.textHeight);

            float w, h;
            if (_widthAutoSize)
                w = _textWidth / GRoot.contentScaleFactor;
            else
                w = this.width;

            if (_heightAutoSize)
            {
                h = _textHeight / GRoot.contentScaleFactor;
                if (!_widthAutoSize)
                    _richTextField.height = _textHeight;
            }
            else
            {
                h = this.height;
                if (_textHeight > this.height * GRoot.contentScaleFactor)
                    _textHeight = Mathf.CeilToInt(this.height * GRoot.contentScaleFactor);
                _richTextField.height = h * GRoot.contentScaleFactor;
            }

            this.SetSize(Mathf.RoundToInt(w), Mathf.RoundToInt(h));
            DoAlign();

            _updatingSize = false;
        }

        override protected void UpdateTextFormat()
        {
            if (_textFormat.font == null || _textFormat.font.Length == 0)
            {
                TextFormat tf = _richTextField.textFormat;
                tf.CopyFrom(_textFormat);
                tf.font = UIConfig.defaultFont;
                _richTextField.textFormat = tf;
            }
            else
            {
                TextFormat tf = _richTextField.textFormat;
                tf.CopyFrom(_textFormat);
                _richTextField.textFormat = tf;
            }
            _richTextField.stroke = _stroke;
            _richTextField.strokeColor = _strokeColor;

            if (!underConstruct)
                UpdateSize();
        }

        override protected void DoAlign()
        {
            //not support
        }

        override protected void HandleSizeChanged()
        {
            if (!_updatingSize)
            {
                if (!_widthAutoSize)
                {
                    _richTextField.width = this.width * GRoot.contentScaleFactor;

                    float h = _richTextField.textHeight;
                    float h2 = this.height * GRoot.contentScaleFactor;
                    if (_heightAutoSize)
                    {
                        _richTextField.height = h;
                        this.height = Mathf.RoundToInt(h / GRoot.contentScaleFactor);
                    }
                    else
                        _richTextField.height = h2;
                }
                DoAlign();
            }
        }
    }
}

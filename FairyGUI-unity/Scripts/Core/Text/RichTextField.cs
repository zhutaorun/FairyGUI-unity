using UnityEngine;
using System.Collections.Generic;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class RichTextField : Container
    {
        public static IRichTextObjectFactory objectFactory;

        TextField _textField;

        public RichTextField()
        {
            _textField = new TextField();
            _textField.optimizeNotTouchable = false;
            _textField.objectContainer = this;
            AddChild(_textField);

            quadBatch = _textField.quadBatch;

            onClick.AddCapture(__click);
        }

        public string text
        {
            get { return _textField.text; }
            set { _textField.text = value; }
        }

        public string htmlText
        {
            get { return _textField.htmlText; }
            set { _textField.htmlText = value; }
        }

        public TextFormat textFormat
        {
            get { return _textField.textFormat; }
            set { _textField.textFormat = value; }
        }

        public AlignType align
        {
            get { return _textField.align; }
            set { _textField.align = value; }
        }

        public bool autoSize
        {
            get { return _textField.autoSize; }
            set { _textField.autoSize = value; }
        }

        public bool wordWrap
        {
            get { return _textField.wordWrap; }
            set { _textField.wordWrap = value; }
        }

        public bool stroke
        {
            get { return _textField.stroke; }
            set { _textField.stroke = value; }
        }

        public Color strokeColor
        {
            get { return _textField.strokeColor; }
            set { _textField.strokeColor = value; }
        }

        public float textWidth
        {
            get { return _textField.textWidth; }
        }

        public float textHeight
        {
            get { return _textField.textHeight; }
        }

        public override float width
        {
            get { return _textField.width; }
            set { _textField.width = value; }
        }

        public override float height
        {
            get { return _textField.height; }
            set { _textField.height = value; }
        }

        void __click(EventContext context)
        {
            Vector3 v = context.inputEvent.position;
            v = this.GlobalToLocal(v);

            HtmlElement_A link = _textField.GetLink(v);
            if (link != null)
            {
                this.DispatchEvent(onClickLink.type, link.href);
                context.StopPropagation();
            }
        }
    }
}

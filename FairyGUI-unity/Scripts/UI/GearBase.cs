using DG.Tweening;
using FairyGUI.Utils;

namespace FairyGUI
{
    abstract public class GearBase
    {
        public PageOptionSet pageSet { get; private set; }
        public bool tween;
        public Ease easeType;
        public float tweenTime;

        protected GObject _owner;
        protected Controller _controller;

        protected static char[] jointChar0 = new char[] { ',' };
        protected static char[] jointChar1 = new char[] { '|' };

        public GearBase(GObject owner)
        {
            _owner = owner;
            pageSet = new PageOptionSet();
            easeType = Ease.OutQuad;
            tweenTime = 0.3f;
        }

        public Controller controller
        {
            get
            {
                return _controller;
            }

            set
            {
                if (value != _controller)
                {
                    _controller = value;
                    pageSet.controller = value;
                    pageSet.Clear();
                    if (_controller != null)
                        Init();
                }
            }
        }

        public void Setup(XML xml)
        {
            string str;

            _controller = _owner.parent.GetController(xml.GetAttribute("controller"));
            if (_controller == null)
                return;

            Init();

            string[] pages = xml.GetAttributeArray("pages");
            if (pages != null)
            {
                foreach (string s in pages)
                    pageSet.AddById(s);
            }

            str = xml.GetAttribute("tween");
            if (str != null)
                tween = true;

            str = xml.GetAttribute("ease");
            if (str != null)
                easeType = FieldTypes.ParseEaseType(str);

            str = xml.GetAttribute("duration");
            if (str != null)
                tweenTime = float.Parse(str);

            str = xml.GetAttribute("values");
            string[] values = null;
            if (str != null)
                values = str.Split(jointChar1);

            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    str = values[i];
                    if (str != "-")
                        AddStatus(pages[i], str);
                }
            }
            str = xml.GetAttribute("default");
            if (str != null)
                AddStatus(null, str);
        }

        virtual protected bool connected
        {
            get
            {
                if (_controller != null && !pageSet.isEmpty)
                    return pageSet.ContainsId(_controller.selectedPageId);
                else
                    return false;
            }
        }

        abstract protected void AddStatus(string pageId, string value);
        abstract protected void Init();

        abstract public void Apply();
        abstract public void UpdateState();
    }
}

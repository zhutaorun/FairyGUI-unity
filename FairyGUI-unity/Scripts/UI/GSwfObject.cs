
namespace FairyGUI
{
    public class GSwfObject : GObject
    {
        Container _container;

        public GSwfObject()
        {
        }

        override protected void CreateDisplayObject()
        {
            _container = new Container();
            _container.gOwner = this;
            displayObject = _container;
        }
    }
}

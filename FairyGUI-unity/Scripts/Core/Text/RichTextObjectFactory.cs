using System.Collections.Generic;

namespace FairyGUI
{
    public class RichTextObjectFactory : IRichTextObjectFactory
    {
        public Stack<GLoader> pool;

        public RichTextObjectFactory()
        {
            pool = new Stack<GLoader>();
        }

        public DisplayObject CreateObject(string src, ref int width, ref int height)
        {
            GLoader loader;

            if (pool.Count > 0)
                loader = pool.Pop();
            else
            {
                loader = new GLoader();
                loader.fill = FillType.ScaleFree;
            }
            loader.url = src;

            PackageItem pi = UIPackage.GetItemByURL(src);
            if (width != 0)
                loader.width = width;
            else
            {
                if (pi != null)
                    width = pi.width;
                else
                    width = 20;
                loader.width = width;
            }

            if (height != 0)
                loader.height = height;
            else
            {
                if (pi != null)
                    height = pi.height;
                else
                    height = 20;
                loader.height = height;
            }

            return loader.displayObject;
        }

        public void FreeObject(DisplayObject obj)
        {
            GLoader loader = obj.gOwner as GLoader;
            if (loader != null)
            {
                loader.url = null;
                pool.Push(loader);
            }
        }
    }
}

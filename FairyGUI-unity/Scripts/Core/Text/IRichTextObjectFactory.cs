
namespace FairyGUI
{
    public interface IRichTextObjectFactory
    {
        DisplayObject CreateObject(string src, ref int width, ref int height);
        void FreeObject(DisplayObject obj);
    }
}

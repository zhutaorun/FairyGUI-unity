using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class PackageItem
    {
        public UIPackage owner;

        public PackageItemType type;
        public string id;
        public string name;
        public int width;
        public int height;
        public string file;
        public bool decoded;

        //image
        public Rect? scale9Grid;
        public bool scaleByTile;
        public NTexture texture;

        //movieclip
        public Vector2 pivot;
        public float interval;
        public float repeatDelay;
        public bool swing;
        public Frame[] frames;

        //componenet
        public XML componentData;

        //font
        public BitmapFont bitmapFont;

        //sound
        public AudioClip audioClip;

        //misc
        public byte[] binary;

        public object Load()
        {
            return owner.GetItemAsset(this);
        }
    }
}

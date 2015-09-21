using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FairyGUI.Utils;
using ICSharpCode.SharpZipLib.Zip;
using System.Text;

namespace FairyGUI
{
    public class UIPackage
    {
        public string id { get; private set; }
        public string name { get; private set; }

        internal static int _constructing;

        List<PackageItem> _items;
        Dictionary<string, PackageItem> _itemsById;
        Dictionary<string, PackageItem> _itemsByName;
        ZipFile _descPack;
        AssetBundle _resBundle;
        string _resourceNamePrefix;
        string _customId;

        class AtlasSprite
        {
            public string atlas;
            public Rect rect;
            public bool rotated;
        }
        Dictionary<string, AtlasSprite> _sprites;

        static Dictionary<string, UIPackage> _packageInstById = new Dictionary<string, UIPackage>();
        static Dictionary<string, UIPackage> _packageInstByName = new Dictionary<string, UIPackage>();
        static Dictionary<string, Dictionary<string, string>> _stringsSource;

        static char[] sep0 = new char[] { ',' };
        static char[] sep1 = new char[] { '\n' };
        static char[] sep2 = new char[] { ' ' };
        static char[] sep3 = new char[] { '=' };

        internal static string URL_PREFIX = "ui://";

        public UIPackage()
        {
            _items = new List<PackageItem>();
            _sprites = new Dictionary<string, AtlasSprite>();
        }

        public static UIPackage GetById(string id)
        {
            UIPackage pkg;
            if (_packageInstById.TryGetValue(id, out pkg))
                return pkg;
            else
                return null;
        }

        public static UIPackage GetByName(string name)
        {
            UIPackage pkg;
            if (_packageInstByName.TryGetValue(name, out pkg))
                return pkg;
            else
                return null;
        }

        public static UIPackage AddPackage(AssetBundle desc, AssetBundle res)
        {
            return AddPackage(desc, res, null);
        }

        public static UIPackage AddPackage(AssetBundle desc, AssetBundle res, string resourceNamePrefix)
        {
            byte[] bytes = ((TextAsset)desc.mainAsset).bytes;
            if (desc != res)
                desc.Unload(true);
            return AddPackage(bytes, res, resourceNamePrefix);
        }

        public static UIPackage AddPackage(byte[] desc, AssetBundle res)
        {
            return AddPackage(desc, res, null);
        }

        public static UIPackage AddPackage(byte[] desc, AssetBundle res, string resourceNamePrefix)
        {
            UIPackage pkg = new UIPackage();
            pkg.Create(desc, res, resourceNamePrefix);
            _packageInstById[pkg.id] = pkg;
            _packageInstByName[pkg.name] = pkg;
            return pkg;
        }

        public static UIPackage AddPackage(string descFilePath)
        {
            UIPackage pkg = new UIPackage();
            pkg.Create(descFilePath);
            _packageInstById[pkg.id] = pkg;
            _packageInstByName[pkg.name] = pkg;
            pkg.customId = descFilePath;

            return pkg;
        }

        public static void RemovePackage(string packageId)
        {
            UIPackage pkg = (UIPackage)_packageInstById[packageId];
            pkg.Dispose();
            _packageInstById.Remove(packageId);
            if (pkg._customId != null)
                _packageInstById.Remove(pkg._customId);
        }

        public static GObject CreateObject(string pkgName, string resName)
        {
            UIPackage pkg = GetByName(pkgName);
            if (pkg != null)
                return pkg.CreateObject(resName);
            else
                return null;
        }

        public static GObject CreateObject(string pkgName, string resName, System.Type userClass)
        {
            UIPackage pkg = GetByName(pkgName);
            if (pkg != null)
                return pkg.CreateObject(resName, userClass);
            else
                return null;
        }

        public static GObject CreateObjectFromURL(string url)
        {
            PackageItem pi = GetItemByURL(url);
            if (pi != null)
                return pi.owner.CreateObject(pi, null);
            else
                return null;
        }

        public static GObject CreateObjectFromURL(string url, System.Type userClass)
        {
            PackageItem pi = GetItemByURL(url);
            if (pi != null)
                return pi.owner.CreateObject(pi, userClass);
            else
                return null;
        }

        public static object GetItemAsset(string pkgName, string resName)
        {
            UIPackage pkg = GetByName(pkgName);
            if (pkg != null)
                return pkg.GetItemAsset(resName);
            else
                return null;
        }

        public static string GetItemURL(string pkgName, string resName)
        {
            UIPackage pkg = GetByName(pkgName);
            if (pkg == null)
                return null;

            PackageItem pi;
            if (!pkg._itemsByName.TryGetValue(resName, out pi))
                return null;

            return UIPackage.URL_PREFIX + pkg.id + pi.id;
        }

        public static PackageItem GetItemByURL(string url)
        {
            if (url.Length > 13)
            {
                string pkgId = url.Substring(5, 8);
                string srcId = url.Substring(13);
                UIPackage pkg = GetById(pkgId);
                if (pkg != null)
                    return pkg.GetItem(srcId);
            }
            return null;
        }

        public static object GetItemAssetByURL(string url)
        {
            PackageItem item = GetItemByURL(url);
            if (item == null)
                return null;

            return item.owner.GetItemAsset(item);
        }

        public static void SetStringsSource(XML source)
        {
            _stringsSource = new Dictionary<string, Dictionary<string, string>>();
            XMLList list = source.Elements("string");
            foreach (XML cxml in list)
            {
                string key = cxml.GetAttribute("name");
                string text = cxml.text;
                int i = key.IndexOf("-");
                if (i == -1)
                    continue;

                string key2 = key.Substring(0, i);
                string key3 = key.Substring(i + 1);
                Dictionary<string, string> col = _stringsSource[key2];
                if (col == null)
                {
                    col = new Dictionary<string, string>();
                    _stringsSource[key2] = col;
                }
                col[key3] = text;
            }
        }

        public string customId
        {
            get { return _customId; }
            set
            {
                if (_customId != null)
                    _packageInstById.Remove(_customId);
                _customId = value;
                if (_customId != null)
                    _packageInstById[_customId] = this;
            }
        }

        void Create(byte[] desc, AssetBundle res, string resourceNamePrefix)
        {
            _descPack = new ZipFile(new MemoryStream(desc));
            _resBundle = res;

            if (resourceNamePrefix != null && resourceNamePrefix.Length > 0)
                _resourceNamePrefix = resourceNamePrefix;
            else
                _resourceNamePrefix = "";

            LoadPackage();
        }

        void Create(string descFilePath)
        {
            TextAsset asset = (TextAsset)Resources.Load(descFilePath);
            if (asset == null)
                throw new Exception("FairyGUI: Cannot load ui package in '" + descFilePath + ",");

            _descPack = new ZipFile(new MemoryStream(asset.bytes));
            _resourceNamePrefix = descFilePath + "@";

            LoadPackage();
        }

        void LoadPackage()
        {
            string[] arr = null;
            string str;

            str = LoadString("sprites.bytes");
            arr = str.Split(sep1);
            int cnt = arr.Length;
            for (int i = 1; i < cnt; i++)
            {
                str = arr[i];
                if (str.Length == 0)
                    continue;

                string[] arr2 = str.Split(sep2);
                AtlasSprite sprite = new AtlasSprite();
                string itemId = arr2[0];
                int binIndex = int.Parse(arr2[1]);
                if (binIndex >= 0)
                    sprite.atlas = "atlas" + binIndex;
                else
                {
                    int pos = itemId.IndexOf("_");
                    if (pos == -1)
                        sprite.atlas = "atlas_" + itemId;
                    else
                        sprite.atlas = "atlas_" + itemId.Substring(0, pos);
                }
                sprite.rect.x = int.Parse(arr2[2]);
                sprite.rect.y = int.Parse(arr2[3]);
                sprite.rect.width = int.Parse(arr2[4]);
                sprite.rect.height = int.Parse(arr2[5]);
                sprite.rotated = arr2[6] == "1";
                _sprites[itemId] = sprite;
            }

            str = GetDesc("package.xml");
            XML xml = new XML(str);

            id = xml.GetAttribute("id");
            name = xml.GetAttribute("name");

            XML rxml = xml.GetNode("resources");
            if (rxml == null)
                throw new Exception("Invalid package xml");

            XMLList resources = rxml.Elements();

            _itemsById = new Dictionary<string, PackageItem>();
            _itemsByName = new Dictionary<string, PackageItem>(); ;
            PackageItem pi;

            foreach (XML cxml in resources)
            {
                pi = new PackageItem();
                pi.type = FieldTypes.ParsePackageItemType(cxml.name);
                pi.id = cxml.GetAttribute("id");
                pi.name = cxml.GetAttribute("name");
                pi.file = cxml.GetAttribute("file");
                str = cxml.GetAttribute("size");
                if (str != null)
                {
                    arr = str.Split(sep0);
                    pi.width = int.Parse(arr[0]);
                    pi.height = int.Parse(arr[1]);
                }
                switch (pi.type)
                {
                    case PackageItemType.Image:
                        {
                            string scale = cxml.GetAttribute("scale");
                            if (scale == "9grid")
                            {
                                arr = cxml.GetAttributeArray("scale9grid");
                                if (arr != null)
                                {
                                    Rect rect = new Rect();
                                    rect.x = int.Parse(arr[0]);
                                    rect.y = int.Parse(arr[1]);
                                    rect.width = int.Parse(arr[2]);
                                    rect.height = int.Parse(arr[3]);
                                    pi.scale9Grid = rect;
                                }
                            }
                            else if (scale == "tile")
                                pi.scaleByTile = true;
                            break;
                        }
                }
                pi.owner = this;
                _items.Add(pi);
                _itemsById[pi.id] = pi;
                if (pi.name != null)
                    _itemsByName[pi.name] = pi;
            }

            cnt = _items.Count;
            for (int i = 0; i < cnt; i++)
            {
                pi = _items[i];
                if (pi.type == PackageItemType.Font)
                {
                    pi.Load();
                    FontManager.RegisterFont(pi.bitmapFont, null);
                }
                else
                    GetItemAsset(pi);
            }

            if (_resBundle != null)
            {
                _resBundle.Unload(false);
                _resBundle = null;
            }
        }

        public void Dispose()
        {
            int cnt = _items.Count;
            for (int i = 0; i < cnt; i++)
            {
                PackageItem pi = _items[i];
                if (pi.texture != null)
                {
                    if (pi.texture.alphaTexture != null)
                        Texture.Destroy(pi.texture.alphaTexture);
                    pi.texture.Dispose();
                }
                else if (pi.audioClip != null)
                    AudioClip.Destroy(pi.audioClip);
                else if (pi.bitmapFont != null)
                    FontManager.UnregisterFont(pi.bitmapFont);
            }

            if (_resBundle != null)
                _resBundle.Unload(true);
        }

        public GObject CreateObject(string resName)
        {
            PackageItem pi;
            if (!_itemsByName.TryGetValue(resName, out pi))
            {
                Debug.LogError("FairyGUI: resource not found - " + resName + " in " + this.name);
                return null;
            }

            return CreateObject(pi, null);
        }

        public GObject CreateObject(string resName, System.Type userClass)
        {
            PackageItem pi;
            if (!_itemsByName.TryGetValue(resName, out pi))
            {
                Debug.LogError("FairyGUI: resource not found - " + resName + " in " + this.name);
                return null;
            }

            return CreateObject(pi, userClass);
        }

        public object GetItemAsset(string resName)
        {
            PackageItem pi;
            if (!_itemsByName.TryGetValue(resName, out pi))
            {
                Debug.LogError("FairyGUI: Resource not found - " + resName + " in " + this.name);
                return null;
            }

            return GetItemAsset(pi);
        }

        internal GObject CreateObject(PackageItem item, System.Type userClass)
        {
            GObject g = null;
            if (item.type == PackageItemType.Component)
            {
                if (userClass != null)
                    g = (GComponent)userClass.Assembly.CreateInstance(userClass.FullName);
                else
                    g = UIObjectFactory.NewObject(item);
            }
            else
                g = UIObjectFactory.NewObject(item);

            if (g == null)
                return null;

            _constructing++;
            g.ConstructFromResource(item);
            _constructing--;
            return g;
        }

        internal PackageItem GetItem(string itemId)
        {
            PackageItem pi;
            if (_itemsById.TryGetValue(itemId, out pi))
                return pi;
            else
                return null;
        }

        internal string GetDesc(string fileName)
        {
            ZipEntry entry = _descPack.GetEntry(fileName);
            if (entry == null)
                return null;

            byte[] buf = new byte[entry.Size];
            _descPack.GetInputStream(entry).Read(buf, 0, buf.Length);
            return Encoding.UTF8.GetString(buf);
        }

        internal object GetItemAsset(PackageItem item)
        {
            switch (item.type)
            {
                case PackageItemType.Image:
                    if (!item.decoded)
                    {
                        item.decoded = true;
                        AtlasSprite sprite;
                        if (_sprites.TryGetValue(item.id, out sprite))
                            item.texture = CreateSpriteTexture(sprite);
                        else
                            item.texture = NTexture.Empty;
                    }
                    return item.texture;

                case PackageItemType.Atlas:
                    if (!item.decoded)
                    {
                        item.decoded = true;
                        string fileName = (item.file != null && item.file.Length > 0) ? item.file : (item.id + ".png");
                        string filePath = _resourceNamePrefix + Path.GetFileNameWithoutExtension(fileName);

                        Texture2D tex;
                        if (_resBundle != null)
                            tex = (Texture2D)_resBundle.Load(filePath, typeof(Texture2D));
                        else
                            tex = (Texture2D)Resources.Load(filePath, typeof(Texture2D));
                        if (tex == null)
                        {
                            Debug.LogWarning("FairyGUI: texture '" + fileName + "' not found in " + this.name);
                            item.texture = NTexture.Empty;
                        }
                        else
                        {
                            if (tex.mipmapCount > 1)
                                Debug.LogWarning("FairyGUI: texture '" + fileName + "' in " + this.name + " is mipmaps enabled.");
                            item.texture = new NTexture(tex, (float)tex.width / item.width, (float)tex.height / item.height);

                            filePath = filePath + "!a";
                            if (_resBundle != null)
                                tex = (Texture2D)_resBundle.Load(filePath, typeof(Texture2D));
                            else
                                tex = (Texture2D)Resources.Load(filePath, typeof(Texture2D));
                            if (tex != null)
                                item.texture.alphaTexture = tex;
                        }
                    }
                    return item.texture;

                case PackageItemType.Sound:
                    if (!item.decoded)
                    {
                        item.decoded = true;
                        string fileName = _resourceNamePrefix + Path.GetFileNameWithoutExtension(item.file);
                        if (_resBundle != null)
                            item.audioClip = (AudioClip)_resBundle.Load(fileName, typeof(AudioClip));
                        else
                            item.audioClip = (AudioClip)Resources.Load(fileName, typeof(AudioClip));
                    }
                    return item.audioClip;

                case PackageItemType.Font:
                    if (!item.decoded)
                    {
                        item.decoded = true;
                        item.bitmapFont = LoadFont(item);
                    }
                    return item.bitmapFont;

                case PackageItemType.MovieClip:
                    if (!item.decoded)
                    {
                        item.decoded = true;
                        LoadMovieClip(item);
                    }
                    return item.frames;

                case PackageItemType.Component:
                    if (!item.decoded)
                    {
                        item.decoded = true;
                        string str = GetDesc(item.id + ".xml");
                        XML xml = new XML(str);
                        if (_stringsSource != null)
                        {
                            Dictionary<string, string> strings;
                            if (_stringsSource.TryGetValue(this.id + item.id, out strings))
                                TranslateComponent(xml, strings);
                        }
                        item.componentData = xml;
                    }
                    return item.componentData;

                default:
                    if (!item.decoded)
                    {
                        item.decoded = true;
                        item.binary = LoadBinary(item.file);
                    }
                    return item.binary;
            }
        }

        void TranslateComponent(XML xml, Dictionary<string, string> strings)
        {
            XML listNode = xml.GetNode("displayList");
            if (listNode == null)
                return;

            XMLList col = listNode.Elements();

            string ename, elementId, value;
            foreach (XML cxml in col)
            {
                ename = cxml.name;
                elementId = cxml.GetAttribute("id");
                if (cxml.hasAttribute("tooltips"))
                {
                    if (strings.TryGetValue(elementId + "-tips", out value))
                        cxml.SetAttribute("tooltips", value);
                }

                if (ename == "text" || ename == "richtext")
                {
                    if (strings.TryGetValue(elementId, out value))
                        cxml.SetAttribute("text", value);
                }
                else if (ename == "list")
                {
                    XMLList items = cxml.Elements("item");
                    int j = 0;
                    foreach (XML exml in items)
                    {
                        if (strings.TryGetValue(elementId + "-" + j, out value))
                            exml.SetAttribute("title", value);
                        j++;
                    }
                }
                else if (ename == "component")
                {
                    XML dxml = cxml.GetNode("Button");
                    if (dxml != null)
                    {
                        if (strings.TryGetValue(elementId, out value))
                            dxml.SetAttribute("title", value);
                        if (strings.TryGetValue(elementId + "-0", out value))
                            dxml.SetAttribute("selectedTitle", value);
                    }
                    else
                    {
                        dxml = cxml.GetNode("Label");
                        if (dxml != null)
                        {
                            if (strings.TryGetValue(elementId, out value))
                                dxml.SetAttribute("title", value);
                        }
                        else
                        {
                            dxml = cxml.GetNode("ComboBox");
                            if (dxml != null)
                            {
                                if (strings.TryGetValue(elementId, out value))
                                    dxml.SetAttribute("title", value);

                                XMLList items = dxml.Elements("item");
                                int j = 0;
                                foreach (XML exml in items)
                                {
                                    if (strings.TryGetValue(elementId + "-" + j, out value))
                                        exml.SetAttribute("title", value);
                                    j++;
                                }
                            }
                        }
                    }
                }
            }
        }

        NTexture CreateSpriteTexture(AtlasSprite sprite)
        {
            PackageItem atlasItem;
            if (_itemsById.TryGetValue(sprite.atlas, out atlasItem))
                return new NTexture((NTexture)GetItemAsset(atlasItem), sprite.rect);
            else
            {
                Debug.LogWarning("FairyGUI: " + sprite.atlas + " not found in " + this.name);
                return NTexture.Empty;
            }
        }

        byte[] LoadBinary(string fileName)
        {
            fileName = _resourceNamePrefix + Path.GetFileNameWithoutExtension(fileName);
            TextAsset ta;
            if (_resBundle != null)
                ta = ((TextAsset)_resBundle.Load(fileName, typeof(TextAsset)));
            else
                ta = ((TextAsset)Resources.Load(fileName, typeof(TextAsset)));
            if (ta == null)
                return null;
            else
                return ta.bytes;
        }

        string LoadString(string fileName)
        {
            byte[] data = LoadBinary(fileName);
            return Encoding.UTF8.GetString(data);
        }

        void LoadMovieClip(PackageItem item)
        {
            string str = GetDesc(item.id + ".xml");
            XML xml = new XML(str);
            string[] arr = null;

            arr = xml.GetAttributeArray("pivot");
            if (arr != null)
            {
                item.pivot.x = int.Parse(arr[0]);
                item.pivot.y = int.Parse(arr[1]);
            }
            str = xml.GetAttribute("interval");
            if (str != null)
                item.interval = float.Parse(str) / 1000f;
            item.swing = xml.GetAttributeBool("swing", false);
            str = xml.GetAttribute("repeatDelay");
            if (str != null)
                item.repeatDelay = float.Parse(str) / 1000f;
            int frameCount = xml.GetAttributeInt("frameCount");
            item.frames = new Frame[frameCount];

            XMLList frameNodes = xml.GetNode("frames").Elements();

            int i = 0;
            foreach (XML frameNode in frameNodes)
            {
                Frame frame = new Frame();
                arr = frameNode.GetAttributeArray("rect");
                frame.rect = new Rect(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]), int.Parse(arr[3]));
                str = frameNode.GetAttribute("addDelay");
                if (str != null)
                    frame.addDelay = float.Parse(str) / 1000f;

                AtlasSprite sprite;
                if (_sprites.TryGetValue(item.id + "_" + i, out sprite))
                    frame.texture = CreateSpriteTexture(sprite);
                item.frames[i] = frame;
                i++;
            }
        }

        BitmapFont LoadFont(PackageItem item)
        {
            BitmapFont font = new BitmapFont(UIPackage.URL_PREFIX + this.id + item.id);

            string str = GetDesc(item.id + ".fnt");
            string[] arr = str.Split(sep1);
            int cnt = arr.Length;
            Dictionary<string, string> kv = new Dictionary<string, string>();
            NTexture mainTexture = null;
            Vector2 atlasOffset = new Vector2();
            bool ttf = false;
            int lineHeight = 0;
            int xadvance = 0;

            for (int i = 0; i < cnt; i++)
            {
                str = arr[i];
                if (str.Length == 0)
                    continue;

                str = str.Trim();

                string[] arr2 = str.Split(sep2, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 1; j < arr2.Length; j++)
                {
                    string[] arr3 = arr2[j].Split(sep3, StringSplitOptions.RemoveEmptyEntries);
                    kv[arr3[0]] = arr3[1];
                }

                str = arr2[0];
                if (str == "char")
                {
                    BitmapFont.BMGlyph bg = new BitmapFont.BMGlyph();
                    if (kv.TryGetValue("x", out str))
                        bg.x = int.Parse(str);
                    if (kv.TryGetValue("y", out str))
                        bg.y = int.Parse(str);
                    if (kv.TryGetValue("xoffset", out str))
                        bg.offsetX = int.Parse(str);
                    if (kv.TryGetValue("yoffset", out str))
                        bg.offsetY = int.Parse(str);
                    if (kv.TryGetValue("width", out str))
                        bg.width = int.Parse(str);
                    if (kv.TryGetValue("height", out str))
                        bg.height = int.Parse(str);
                    if (kv.TryGetValue("xadvance", out str))
                        bg.advance = int.Parse(str);
                    if (kv.TryGetValue("chnl", out str))
                    {
                        bg.channel = int.Parse(str);
                        if (bg.channel == 15)
                            bg.channel = 4;
                        else if (bg.channel == 1)
                            bg.channel = 3;
                        else if (bg.channel == 2)
                            bg.channel = 2;
                        else
                            bg.channel = 1;
                    }

                    if (!ttf)
                    {
                        if (kv.TryGetValue("img", out str))
                        {
                            PackageItem charImg;
                            if (_itemsById.TryGetValue(str, out charImg))
                            {
                                charImg.Load();
                                bg.uvRect = charImg.texture.uvRect;
                                if (mainTexture == null)
                                    mainTexture = charImg.texture.root;
                                bg.width = charImg.texture.width;
                                bg.height = charImg.texture.height;
                            }
                        }
                    }
                    else
                    {
                        Rect region = new Rect(bg.x + atlasOffset.x, bg.y + atlasOffset.y, bg.width, bg.height);
                        bg.uvRect = new Rect(region.x / mainTexture.width, 1 - region.yMax / mainTexture.height,
                            region.width / mainTexture.width, region.height / mainTexture.height);
                    }

                    if (ttf)
                        bg.lineHeight = lineHeight;
                    else
                    {
                        if (bg.advance == 0)
                        {
                            if (xadvance == 0)
                                bg.advance = bg.offsetX + bg.width;
                            else
                                bg.advance = xadvance;
                        }

                        bg.lineHeight = bg.offsetY < 0 ? bg.height : (bg.offsetY + bg.height);
                        if (bg.lineHeight < lineHeight)
                            bg.lineHeight = lineHeight;
                    }

                    int ch = int.Parse(kv["id"]);
                    font.AddChar((char)ch, bg);
                }
                else if (str == "info")
                {
                    if (kv.TryGetValue("face", out str))
                    {
                        ttf = true;

                        AtlasSprite sprite;
                        if (_sprites.TryGetValue(item.id, out sprite))
                        {
                            atlasOffset = new Vector2(sprite.rect.x, sprite.rect.y);
                            PackageItem atlasItem = _itemsById[sprite.atlas];
                            mainTexture = (NTexture)GetItemAsset(atlasItem);
                        }
                    }
                }
                else if (str == "common")
                {
                    if (kv.TryGetValue("lineHeight", out str))
                        lineHeight = int.Parse(str);
                    if (kv.TryGetValue("xadvance", out str))
                        xadvance = int.Parse(str);
                }
            }

            font.hasChannel = ttf;
            font.canTint = ttf;
            font.lineHeight = lineHeight;
            font.mainTexture = mainTexture;
            if (!ttf)
            {
                if (mainTexture != null && mainTexture.root.alphaTexture != null)
                    font.shader = ShaderConfig.combinedImageShader;
                else
                    font.shader = ShaderConfig.imageShader;
            }
            return font;
        }
    }
}

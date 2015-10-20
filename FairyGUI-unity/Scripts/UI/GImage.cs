using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
	public class GImage : GObject, IColorGear
	{
		public GearColor gearColor { get; private set; }

		Image _content;

		public GImage()
		{
			gearColor = new GearColor(this);
		}

		override protected void CreateDisplayObject()
		{
			_content = new Image();
			_content.gOwner = this;
			displayObject = _content;
		}

		public Color color
		{
			get { return _content.color; }
			set
			{
				_content.color = value;
				if (gearColor.controller != null)
					gearColor.UpdateState();
			}
		}

		public FlipType flip
		{
			get { return _content.flip; }
			set { _content.flip = value; }
		}

		override public void ConstructFromResource(PackageItem pkgItem)
		{
			_packageItem = pkgItem;
			sourceWidth = _packageItem.width;
			sourceHeight = _packageItem.height;
			initWidth = sourceWidth;
			initHeight = sourceHeight;
			_content.scale9Grid = _packageItem.scale9Grid;
			_content.scaleByTile = _packageItem.scaleByTile;

			_packageItem.Load();

			_content.texture = _packageItem.texture;

			SetSize(sourceWidth, sourceHeight);
		}

		override protected void HandleSizeChanged()
		{
			_content.SetScale(this.width / sourceWidth * this.scaleX * GRoot.contentScaleFactor, this.height / sourceHeight * this.scaleY * GRoot.contentScaleFactor);
		}

		override public void HandleControllerChanged(Controller c)
		{
			base.HandleControllerChanged(c);

			if (gearColor.controller == c)
				gearColor.Apply();
		}

		override public void Setup_BeforeAdd(XML xml)
		{
			base.Setup_BeforeAdd(xml);

			string str;
			str = xml.GetAttribute("color");
			if (str != null)
				this.color = ToolSet.ConvertFromHtmlColor(str);

			str = xml.GetAttribute("flip");
			if (str != null)
				_content.flip = FieldTypes.ParseFlipType(str);
		}

		override public void Setup_AfterAdd(XML xml)
		{
			base.Setup_AfterAdd(xml);

			XML cxml = xml.GetNode("gearColor");
			if (cxml != null)
				gearColor.Setup(cxml);
		}
	}
}

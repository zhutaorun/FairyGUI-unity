using UnityEngine;
using FairyGUI.Utils;

namespace FairyGUI
{
	public class GMovieClip : GObject, IAnimationGear, IColorGear
	{
		public GearAnimation gearAnimation { get; private set; }
		public EventListener onPlayEnd { get; private set; }
		public GearColor gearColor { get; private set; }

		MovieClip _content;

		public GMovieClip()
		{
			gearAnimation = new GearAnimation(this);
			gearColor = new GearColor(this);

			onPlayEnd = new EventListener(this, "onPlayEnd");
		}

		override protected void CreateDisplayObject()
		{
			_content = new MovieClip();
			_content.gOwner = this;
			displayObject = _content;
		}

		public bool playing
		{
			get { return _content.playing; }
			set
			{
				if (_content.playing != value)
				{
					_content.playing = value;
					if (gearAnimation.controller != null)
						gearAnimation.UpdateState();
				}
			}
		}

		public int frame
		{
			get { return _content.currentFrame; }
			set
			{
				if (_content.currentFrame != value)
				{
					_content.currentFrame = value;
					if (gearAnimation.controller != null)
						gearAnimation.UpdateState();
				}
			}
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

		//从start帧开始，播放到end帧（-1表示结尾），重复times次（0表示无限循环），循环结束后，停止在endAt帧（-1表示参数end）
		public void SetPlaySettings(int start, int end, int times, int endAt)
		{
			((MovieClip)displayObject).SetPlaySettings(start, end, times, endAt);
		}

		override public void HandleControllerChanged(Controller c)
		{
			base.HandleControllerChanged(c);
			if (gearAnimation.controller == c)
				gearAnimation.Apply();
			if (gearColor.controller == c)
				gearColor.Apply();
		}

		override public void ConstructFromResource(PackageItem pkgItem)
		{
			_packageItem = pkgItem;

			sourceWidth = _packageItem.width;
			sourceHeight = _packageItem.height;
			initWidth = sourceWidth;
			initHeight = sourceHeight;

			_packageItem.Load();
			_content.interval = _packageItem.interval;
			_content.frames = _packageItem.frames;
			_content.boundsRect = new Rect(0, 0, sourceWidth, sourceHeight);

			SetSize(sourceWidth, sourceHeight);
		}

		override protected void HandleSizeChanged()
		{
			displayObject.SetScale(this.width / sourceWidth * this.scaleX * GRoot.contentScaleFactor,
				this.height / sourceHeight * this.scaleY * GRoot.contentScaleFactor);
		}

		override public void Setup_BeforeAdd(XML xml)
		{
			base.Setup_BeforeAdd(xml);

			string str;

			str = xml.GetAttribute("frame");
			if (str != null)
				_content.currentFrame = int.Parse(str);
			_content.playing = xml.GetAttributeBool("playing", true);

			str = xml.GetAttribute("color");
			if (str != null)
				this.color = ToolSet.ConvertFromHtmlColor(str);
		}

		override public void Setup_AfterAdd(XML xml)
		{
			base.Setup_AfterAdd(xml);

			XML cxml = xml.GetNode("gearAni");
			if (cxml != null)
				gearAnimation.Setup(cxml);

			cxml = xml.GetNode("gearColor");
			if (cxml != null)
				gearColor.Setup(cxml);
		}
	}
}

using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
	public struct Frame
	{
		public Rect rect;
		public float addDelay;
		public NTexture texture;
	}

	public class MovieClip : Image
	{
		public float interval;
		public bool swing;
		public float repeatDelay;

		public int frameCount { get; private set; }
		public PlayState playState;

		public EventListener onPlayEnd { get; private set; }

		Frame[] _frames;
		int _currentFrame;
		bool _playing;
		int _start;
		int _end;
		int _times;
		int _endAt;
		int _status; //0-none, 1-next loop, 2-ending, 3-ended

		public MovieClip()
		{
			playState = new PlayState();
			interval = 0.1f;
			_playing = true;

			onPlayEnd = new EventListener(this, "onPlayEnd");
		}

		public Frame[] frames
		{
			get { return _frames; }
			set
			{
				_frames = value;
				if (_frames != null)
					frameCount = _frames.Length;
				else
					frameCount = 0;
				NTexture t = null;
				for (int i = 0; i < frameCount; i++)
				{
					Frame frame = frames[i];
					if (frame.texture != null)
					{
						t = frame.texture;
						break;
					}
				}
				SetPlaySettings();
				if (t != null)
				{
					quadBatch.texture = t.root;

					if (t.root.alphaTexture != null)
					{
						quadBatch.shader = ShaderConfig.GetGrayedVersion(ShaderConfig.combinedImageShader,
							ShaderConfig.IsGrayedVersion(quadBatch.shader));
					}
					else
					{
						quadBatch.shader = ShaderConfig.GetGrayedVersion(ShaderConfig.imageShader,
							ShaderConfig.IsGrayedVersion(quadBatch.shader));
					}
				}
				_needRebuild = true;
				InvalidateBatchingState();
			}
		}

		public Rect boundsRect
		{
			get { return contentRect; }
			set
			{
				contentRect = value;
			}
		}

		public bool playing
		{
			get { return _playing; }
			set { _playing = value; }
		}

		public int currentFrame
		{
			get { return _currentFrame; }
			set
			{
				if (_currentFrame != value)
				{
					_currentFrame = value;
					playState.currrentFrame = value;
					quadBatch.SetQuadAlpha(_currentFrame, 1, 1f, 0f);
				}
			}
		}

		override protected void OverridedScale()
		{
			_needRebuild = true;
		}

		public void SetPlaySettings()
		{
			SetPlaySettings(0, -1, 0, -1);
		}

		//从start帧开始，播放到end帧（-1表示结尾），重复times次（0表示无限循环），循环结束后，停止在endAt帧（-1表示参数end）
		public void SetPlaySettings(int start, int end, int times, int endAt)
		{
			_start = start;
			_end = end;
			if (_end == -1)
				_end = frameCount - 1;
			_times = times;
			_endAt = endAt;
			if (_endAt == -1)
				_endAt = _end;
			this.currentFrame = start;
			_status = 0;
		}

		protected override void UpdateTexture()
		{
			//quadBatch.texture = _texture;
			//we dont need rebuild, for this texture is altas
		}

		public override void Update(UpdateContext context, float parentAlpha)
		{
			if (_needRebuild)
				Rebuild();

			if (_playing && frameCount != 0 && _status != 3)
			{
				playState.Update(this, context);
				if (_currentFrame != playState.currrentFrame)
				{
					if (_status == 1)
					{
						_currentFrame = _start;
						playState.currrentFrame = _currentFrame;
						_status = 0;
					}
					else if (_status == 2)
					{
						_currentFrame = _endAt;
						playState.currrentFrame = _currentFrame;
						_status = 3;
						Stage.inst.onPostUpdate.Add(__playEnd);
					}
					else
					{
						_currentFrame = playState.currrentFrame;
						if (_currentFrame == _end)
						{
							if (_times > 0)
							{
								_times--;
								if (_times == 0)
									_status = 2;
								else
									_status = 1;
							}
						}
					}
					quadBatch.SetQuadAlpha(_currentFrame, 1, 1f, 0f);
				}
			}
			quadBatch.Update(context, alpha * parentAlpha);
		}

		private void __playEnd()
		{
			Stage.inst.onPostUpdate.Remove(__playEnd);
			onPlayEnd.Call();
		}

		protected override void Rebuild()
		{
			_needRebuild = false;

			if (frameCount == 0)
			{
				quadBatch.enabled = false;
				return;
			}

			quadBatch.enabled = true;
			Vector3[] verts = new Vector3[frameCount * 4];
			Vector2[] uv = new Vector2[frameCount * 4];
			Rect EmptyRect = new Rect(0, 0, 0, 0);
			for (int i = 0; i < frameCount; i++)
			{
				Frame frame = frames[i];
				if (frame.texture != null)
				{
					QuadBatch.FillVertsOfQuad(verts, i * 4, frame.rect, this.scaleX, this.scaleY);
					QuadBatch.FillUVOfQuad(uv, i * 4, frame.texture.uvRect);
				}
				else
				{
					QuadBatch.FillVertsOfQuad(verts, i * 4, EmptyRect);
					QuadBatch.FillUVOfQuad(uv, i * 4, EmptyRect);
				}
			}
			quadBatch.Fill(verts, uv, _color);
			quadBatch.SetQuadAlpha(_currentFrame, 1, 1f, 0f);
		}
	}
}

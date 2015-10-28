using UnityEngine;
using FairyGUI;
using System.IO;

public class MyGLoader : GLoader
{
	protected override void LoadExternal()
	{
		IconManager.inst.LoadIcon(this.url, OnLoadSuccess, OnLoadFail);
	}

	void OnLoadSuccess(NTexture texture)
	{
		if (string.IsNullOrEmpty(this.url))
			return;

		this.onExternalLoadSuccess(texture);
	}

	void OnLoadFail(string error)
	{
		Debug.Log("load " + this.url + " failed: " + error);
		this.onExternalLoadFailed();
	}

	protected override void FreeExternal(NTexture texture)
	{
		texture.refCount--;
	}
}

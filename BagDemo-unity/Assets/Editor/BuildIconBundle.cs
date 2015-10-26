using UnityEngine;
using UnityEditor;
using System.IO;

public class ExportAssetBundles
{
	[MenuItem("FairyGUI demo/Build Icon Bundles")]
	public static void BuildIcons()
	{
#if UNITY_5
		for (int i = 0; i < 10; i++)
		{
			AssetImporter.GetAtPath("Assets/Sources/icons/i" + i + ".png").assetBundleName = "i" + i + ".ab";
		}
		BuildPipeline.BuildAssetBundles(Path.Combine(Application.streamingAssetsPath, "icons"), BuildAssetBundleOptions.None, BuildTarget.Android);
#else
		for (int i = 0; i < 10; i++)
		{
			Object obj = AssetDatabase.LoadAssetAtPath("Assets/Sources/icons/i"+i+".png", typeof(Object));
			BuildPipeline.BuildAssetBundle(obj, null, Path.Combine(Application.streamingAssetsPath, "icons/i" + i + ".ab"), 
				BuildAssetBundleOptions.CollectDependencies, BuildTarget.Android);
		}
		AssetDatabase.Refresh();
#endif
	}
}
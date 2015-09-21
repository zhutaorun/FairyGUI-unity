using UnityEngine;

namespace FairyGUI
{
    public class ShaderConfig
    {
        public delegate Shader GetFunction(string name);

        public static GetFunction Get = Shader.Find;

        public static string imageShader = "FairyGUI/Image";
        public static string textShader = "FairyGUI/Text";
        public static string textBrighterShader = "FairyGUI/Text Brighter";
        public static string bmFontShader = "FairyGUI/BMFont";
        public static string combinedImageShader = "FairyGUI/Combined Image";

        public static string alphaClipShaderSuffix = " (AlphaClip)";
        public static string softClipShaderSuffix = " (SoftClip)";
        public static string grayedShaderSuffix = " Grayed";

        public static string GetGrayedVersion(string shader, bool grayed)
        {
            int i = shader.IndexOf(grayedShaderSuffix);
            if (grayed)
            {
                if (i == -1)
                    return shader + grayedShaderSuffix;
            }
            else
            {
                if (i != -1)
                    return shader.Substring(0, i);
            }

            return shader;
        }

        public static bool IsGrayedVersion(string shader)
        {
            return shader.IndexOf(grayedShaderSuffix) != -1;
        }
    }
}

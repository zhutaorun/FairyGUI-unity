using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    public class MaterialManager
    {
        public Material sharedMaterial { get; private set; }

        NTexture _owner;
        List<Material> _clippedMaterials;
        uint _updateSeq;
        uint _clipId;
        int _nextIndex;

        List<Material> _softClippedMaterials;
        uint _softUpdateSeq;
        uint _softClipId;
        int _softNextIndex;

        public static MaterialManager GetInstance(NTexture texture, string shaderName)
        {
            NTexture rootTexture = texture.root;
            if (rootTexture.materialManagers == null)
                rootTexture.materialManagers = new Dictionary<string, MaterialManager>();

            MaterialManager mm;
            if (!rootTexture.materialManagers.TryGetValue(shaderName, out mm))
            {
                mm = new MaterialManager(rootTexture);
                rootTexture.materialManagers.Add(shaderName, mm);
            }

            if (mm.sharedMaterial == null)
            {
                Shader shader = ShaderConfig.Get(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning("FairyGUI: shader not found: " + shaderName);
                    shader = Shader.Find("Transparent/Diffuse");
                }
                mm.sharedMaterial = new Material(shader);
                mm.sharedMaterial.mainTexture = rootTexture.nativeTexture;
                if (rootTexture.alphaTexture != null)
                    mm.sharedMaterial.SetTexture("_AlphaTex", rootTexture.alphaTexture);
            }

            return mm;
        }

        public MaterialManager(NTexture owner)
        {
            _owner = owner;
        }

        public Material GetContextMaterial(UpdateContext context)
        {
            if (!context.clipped)
                return sharedMaterial;

            Material mat;
            if (context.clipInfo.soft)
            {
                if (context.workCount != _softUpdateSeq)
                {
                    _softUpdateSeq = context.workCount;
                    _softClipId = context.clipInfo.clipId;
                    _softNextIndex = 0;
                }
                else if (_softClipId != context.clipInfo.clipId)
                {
                    _softNextIndex++;
                    _softClipId = context.clipInfo.clipId;
                }

                if (_softClippedMaterials == null)
                    _softClippedMaterials = new List<Material>();

                if (_softNextIndex < _softClippedMaterials.Count)
                    mat = _softClippedMaterials[_softNextIndex];
                else
                {
                    Shader shader = ShaderConfig.Get(sharedMaterial.shader.name + ShaderConfig.softClipShaderSuffix);
                    if (shader == null)
                    {
                        Debug.LogWarning("FairyGUI: " + sharedMaterial.shader.name + " doesn't have a soft clipped shader version for clipping");
                        shader = sharedMaterial.shader;
                    }

                    mat = new Material(shader);
                    mat.mainTexture = sharedMaterial.mainTexture;
                    if (_owner.alphaTexture != null)
                        mat.SetTexture("_AlphaTex", _owner.alphaTexture);
                    _softClippedMaterials.Add(mat);
                }

                mat.mainTextureOffset = context.clipInfo.offset;
                mat.mainTextureScale = context.clipInfo.scale;
                mat.SetVector("_ClipSharpness", context.clipInfo.softness);
            }
            else
            {
                if (context.workCount != _updateSeq)
                {
                    _updateSeq = context.workCount;
                    _clipId = context.clipInfo.clipId;
                    _nextIndex = 0;
                }
                else if (_clipId != context.clipInfo.clipId)
                {
                    _nextIndex++;
                    _clipId = context.clipInfo.clipId;
                }

                if (_clippedMaterials == null)
                    _clippedMaterials = new List<Material>();

                if (_nextIndex < _clippedMaterials.Count)
                    mat = _clippedMaterials[_nextIndex];
                else
                {
                    Shader shader = ShaderConfig.Get(sharedMaterial.shader.name + ShaderConfig.alphaClipShaderSuffix);
                    if (shader == null)
                    {
                        Debug.LogWarning("FairyGUI: " + sharedMaterial.shader.name + " doesn't have a clipped shader version for clipping");
                        shader = sharedMaterial.shader;
                    }

                    mat = new Material(shader);
                    mat.mainTexture = sharedMaterial.mainTexture;
                    if (_owner.alphaTexture != null)
                        mat.SetTexture("_AlphaTex", _owner.alphaTexture);
                    _clippedMaterials.Add(mat);
                }

                mat.mainTextureOffset = context.clipInfo.offset;
                mat.mainTextureScale = context.clipInfo.scale;
            }

            return mat;
        }

        public void Dispose()
        {
            if (sharedMaterial != null)
            {
                Material.Destroy(sharedMaterial);
                sharedMaterial = null;
            }

            if (_clippedMaterials != null)
            {
                foreach (Material mat in _clippedMaterials)
                    Material.Destroy(mat);
            }

            if (_softClippedMaterials != null)
            {
                foreach (Material mat in _softClippedMaterials)
                    Material.Destroy(mat);
            }
        }
    }
}

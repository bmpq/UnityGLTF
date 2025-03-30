using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF
{
    public static class TextureConverter
    {
        public static Texture2D Convert(Texture inputTexture, Material mat)
        {
            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(inputTexture, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            convertedTexture.name = inputTexture.name;

            return convertedTexture;
        }

        public static Texture2D ConvertAlbedoSpecGlosToSpecGloss(Texture inputTextureAlbedoSpec, Texture inputTextureGloss)
        {
            Material mat = new Material(Shader.Find("Hidden/AlbedoSpecGlosToSpecGloss"));
            mat.SetTexture("_AlbedoSpecTex", inputTextureAlbedoSpec);
            mat.SetTexture("_GlossinessTex", inputTextureGloss);

            bool sRGBWrite = GL.sRGBWrite;
            GL.sRGBWrite = false;
            RenderTexture temporary = RenderTexture.GetTemporary(inputTextureAlbedoSpec.width, inputTextureAlbedoSpec.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            Graphics.Blit(inputTextureAlbedoSpec, temporary, mat);

            Texture2D convertedTexture = temporary.ToTexture2D();
            convertedTexture.name = ReplaceLastWord(inputTextureAlbedoSpec.name, '_', "SPECGLOS");

            RenderTexture.ReleaseTemporary(temporary);
            GL.sRGBWrite = sRGBWrite;

            return convertedTexture;
        }

        static Texture2D ToTexture2D(this RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            return tex;
        }

        public static string ReplaceLastWord(this string input, char separator, string replacement)
        {
            int lastIndex = input.LastIndexOf(separator);
            if (lastIndex == -1)
            {
                return replacement;
            }
            return input.Substring(0, lastIndex + 1) + replacement;
        }

        public static Texture2D CreateSolidColorTexture(int width, int height, float r, float g, float b, float a)
        {
            Texture2D texture = new Texture2D(width, height);

            Color[] pixels = new Color[width * height];
            Color color = new Color(r, g, b, a);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        public static Texture2D CreateSolidColorTexture(int width, int height, float c, float a)
        {
            return CreateSolidColorTexture(width, height, c, c, c, a);
        }
    }
}

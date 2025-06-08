#if UNITY_EDITOR
using UnityEngine;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Dynamicweb.WebP;
public static class Texture2DToWebPConverter
{
    public static byte[] EncodeToWEBP(this Texture2D texture, int quality = 90)
    {
        if (texture == null)
        {
            Debug.LogError("WebP Converter: Input texture is null.");
            return null;
        }

        if (!texture.isReadable)
        {
            Debug.LogError($"WebP Converter: Texture '{texture.name}' is not readable. Please enable 'Read/Write' in its import settings.");
            return null;
        }

        try
        {
            using (Bitmap bmp = Texture2DToBitmap(texture))
            {
                byte[] webpBytes;
                webpBytes = Dynamicweb.WebP.Encoder.Encode(bmp, quality);

                if (webpBytes == null || webpBytes.Length == 0)
                {
                    Debug.LogError("WebP Converter: Encoding failed, returned empty byte array.");
                    return null;
                }

                Debug.Log($"Successfully converted to WebP");
                return webpBytes;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WebP Converter: An error occurred during conversion. Error: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    private static Bitmap Texture2DToBitmap(Texture2D texture)
    {
        Bitmap bmp = new Bitmap(texture.width, texture.height, PixelFormat.Format32bppArgb);

        BitmapData bmpData = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.WriteOnly,
            bmp.PixelFormat);

        Color32[] pixels = texture.GetPixels32();

        byte[] bytePixels = new byte[pixels.Length * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            bytePixels[i * 4] = pixels[i].b;        // Blue
            bytePixels[i * 4 + 1] = pixels[i].g;    // Green
            bytePixels[i * 4 + 2] = pixels[i].r;    // Red
            bytePixels[i * 4 + 3] = pixels[i].a;    // Alpha
        }

        System.Runtime.InteropServices.Marshal.Copy(bytePixels, 0, bmpData.Scan0, bytePixels.Length);

        bmp.UnlockBits(bmpData);

        return bmp;
    }
}
#endif

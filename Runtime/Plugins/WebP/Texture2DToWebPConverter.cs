using UnityEngine;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System;

#if DYNAMICWEB_WEBP
using Dynamicweb.WebP;
#elif HAVE_WEBP
using unity.libwebp;
using WebP;
#endif

public static class Texture2DToWebPConverter
{
#if DYNAMICWEB_WEBP
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

        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

        return bmp;
    }
#elif HAVE_WEBP

    public static unsafe byte[] EncodeToWEBPFlippedY(this Texture2D lTexture2D, float lQuality, out Error lError)
    {
        lError = Error.Success;

        if (lQuality < -1)
        {
            lQuality = -1;
        }
        if (lQuality > 100)
        {
            lQuality = 100;
        }

        Color32[] lRawColorData = lTexture2D.GetPixels32();
        int lWidth = lTexture2D.width;
        int lHeight = lTexture2D.height;
        int stride = 4 * lWidth;

        GCHandle lPinnedArray = GCHandle.Alloc(lRawColorData, GCHandleType.Pinned);
        IntPtr lRawDataPtr = lPinnedArray.AddrOfPinnedObject();

        IntPtr lFlippedDataPtr = lRawDataPtr + (lHeight - 1) * stride;
        int lNegativeStride = -stride;

        IntPtr lResult = new IntPtr();
        byte** pResult = (byte**)&lResult;
        byte[] lOutputBuffer = null;

        try
        {
            int lLength;

            if (lQuality == -1)
            {
                lLength = (int)NativeLibwebp.WebPEncodeLosslessRGBA((byte*)lFlippedDataPtr, lWidth, lHeight, lNegativeStride, pResult);
            }
            else
            {
                lLength = (int)NativeLibwebp.WebPEncodeRGBA((byte*)lFlippedDataPtr, lWidth, lHeight, lNegativeStride, lQuality, pResult);
            }

            if (lLength == 0)
            {
                lError = Error.InvalidHeader;
                throw new Exception("WebP encode failed! Returned 0 bytes.");
            }

            lOutputBuffer = new byte[lLength];
            Marshal.Copy(lResult, lOutputBuffer, 0, lLength);
        }
        catch (Exception e)
        {
            lError = Error.InvalidHeader;
            Debug.LogError($"WebP Encoding Exception: {e.Message}");
            return null;
        }
        finally
        {
            NativeLibwebp.WebPSafeFree(*pResult);
            lPinnedArray.Free();
        }

        return lOutputBuffer;
    }
#endif
}

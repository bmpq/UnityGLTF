using UnityEngine;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System;

#if NETPYOUNG_UNITY_WEBP
using unity.libwebp;
using WebP;
#endif

public static class Texture2DToWebPConverter
{
#if NETPYOUNG_UNITY_WEBP

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

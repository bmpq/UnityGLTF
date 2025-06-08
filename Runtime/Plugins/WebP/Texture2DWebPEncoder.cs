using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Webp;
using Aspose.Imaging.ImageOptions;
using System.IO;
using UnityEngine;

public static class Texture2DWebPEncoder
{
    public static byte[] EncodeToWEBP(this Texture2D sourceTexture, int quality)
    {
        if (sourceTexture == null)
        {
            Debug.LogError("Source Texture is not assigned!");
            return null;
        }

        string savePath = Path.Combine(Application.persistentDataPath, sourceTexture.name + ".webp");

        try
        {
            byte[] pngBytes = sourceTexture.EncodeToPNG();

            using (MemoryStream inStream = new MemoryStream(pngBytes))
            {
                using (Image image = Image.Load(inStream))
                {
                    Debug.Log("Aspose: Image loaded successfully from memory stream.");

                    WebPOptions options = new WebPOptions
                    {
                        Quality = quality,
                        Lossless = quality == 100,
                    };
                    Debug.Log($"Aspose: Set WebP options. Quality: {options.Quality}, Lossless: {options.Lossless}");

                    using (MemoryStream outStream = new MemoryStream())
                    {
                        image.Save(outStream, options);
                        Debug.Log("Aspose: Image saved to output memory stream.");

                        return outStream.ToArray();
                    }
                }
            }
        }
        catch (Aspose.Imaging.CoreExceptions.ImageLoadException imgEx)
        {
            Debug.LogError($"Aspose Image Load Failed: {imgEx.Message}. Ensure the input data is a valid PNG/JPG.");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"An error occurred during WebP export: {ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }
}
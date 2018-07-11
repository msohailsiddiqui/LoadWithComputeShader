using UnityEngine;
using System.Collections;
using FreeImageAPI;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.Profiling;

public class FreeImageManager : Singleton<FreeImageManager>
{
    public enum ChannelsPerMap
    {
        R,
        RGB,
        RGBA
    }
	static bool enableLogging = true;
    protected FreeImageManager() { } // guarantee this will be always a singleton only - can't use the constructor!

    byte[] bytes = null;

    void Awake()
    {
        if (enableLogging)
        {
            Debug.Log("FreeImage.IsAvailable:: " + FreeImageAPI.FreeImage.IsAvailable());
            Debug.Log("FreeImage.Version:: "+FreeImageAPI.FreeImageEngine.Version);
        }
        FreeImageAPI.FreeImageEngine.Message += FreeImageEngine_Message;
        bytes = new byte[1073741824];

    }

    // Hook up messages to be output to the log
    private void FreeImageEngine_Message(FREE_IMAGE_FORMAT fif, string message)
    {
        Debug.LogError("Internal Error: " + fif.ToString() + "\nMessage: " + message);
    }

    public void Test()
    {

    }

    public Material AToRMaterial;
    public Material PremultiplyMaterial;
    //public Texture2D tex;
    public RenderTexture LoadImage(string path, bool isLinear = false, bool isGrayscale = false, bool doMipMaps = false, bool forceGC=false, bool premultiplyAlpha = false)
    {
        // default bits per channel
        //uint origBPP = outBPP = 0;
        bool successfullyLoadedRaw = false;
        int width = 0, height = 0;
        TextureFormat formatToLoad= TextureFormat.ARGB32;
        RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
        bool forceGrayscaleAfterTexture2D = false;

        //System.Threading.Thread newThread = new System.Threading.Thread(() =>
        {
            var loadType = System.IO.Path.GetExtension(path);
            FREE_IMAGE_LOAD_FLAGS loadFlags = FREE_IMAGE_LOAD_FLAGS.DEFAULT;
            switch (loadType)
            {
                case ".png":
                    loadFlags = FREE_IMAGE_LOAD_FLAGS.PNG_IGNOREGAMMA;
                    break;

                case ".jpg":
                case ".jpeg":
                    loadFlags = FREE_IMAGE_LOAD_FLAGS.JPEG_ACCURATE;
                    break;
            }
            // Format is stored in 'format' on successfull load.
            FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_UNKNOWN;
            FIBITMAP dib;
            bool isModifiedEXR = false;
            char yChar = 'Y';
            byte yByte = Convert.ToByte(yChar);
            char rChar = 'R';
            byte rByte = Convert.ToByte(rChar);
            //byte[] byteArray = File.ReadAllBytes(path);
            FileStream stream = null;
            if (Path.GetExtension(path).ToLower() == ".exr")
            {
                stream = new FileStream(path, FileMode.Open);

                stream.Position = 66;
                isModifiedEXR = (stream.ReadByte() == rByte);
                if (isModifiedEXR)
                {
                    Debug.Log("<color=blue>*** This is a modified EXR </color>");
                    //byteArray[66] = yByte;
                    stream.Position = 66;
                    stream.WriteByte(yByte);
                    stream.Position = 0;
                }
            }
#if UNITY_STANDALONE_OSX
			    if (stream == null)
				    stream = new FileStream(path, FileMode.Open);

			    dib = FreeImage.LoadFromStream(stream, loadFlags, ref format);
#else
            dib = FreeImage.LoadEx(path, loadFlags, ref format);
            Debug.Log("Used Heap Size After FreeImage.LoadEx: " + Profiler.GetMonoUsedSizeLong() / 1024 / 1024);
#endif
            if (stream != null)
            {
                stream.Dispose();
                GC.Collect();
                Debug.Log("Used Heap Size After stream.Dispose: " + Profiler.GetMonoUsedSizeLong() / 1024 / 1024);
            }

            if (isModifiedEXR)
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    fs.Position = 66;
                    fs.WriteByte(rByte);
                    fs.Position = 0;
                }
            }
            rtFormat = RenderTextureFormat.ARGB32;
            try
            {
                // Error handling
                if (dib.IsNull)
                    return null;

                FREE_IMAGE_TYPE origInputType = FreeImage.GetImageType(dib);

                //Debug.Log("DIB for :" + path);
                // read bits per channel of loaded image
                uint origBPP = FreeImage.GetBPP(dib);
                var header = FreeImage.GetInfoHeaderEx(dib);
                //Debug.Log("original BPP:" + origBPP);
                //Debug.Log("origInputType:" + origInputType);

                // check here if we need to convert single channel textures to RGB or vice versa based on source input texture type and destination type expected
                FREE_IMAGE_TYPE destType = FREE_IMAGE_TYPE.FIT_UNKNOWN;
                switch (origInputType)
                {
                    case FREE_IMAGE_TYPE.FIT_UINT16:
                        if (!isGrayscale)
                        {
                            destType = FREE_IMAGE_TYPE.FIT_RGBAF;
                        }
                        else
                            destType = FREE_IMAGE_TYPE.FIT_FLOAT;
                        break;
                    case FREE_IMAGE_TYPE.FIT_RGBF:
                    case FREE_IMAGE_TYPE.FIT_RGBAF:
                        destType = isGrayscale ? FREE_IMAGE_TYPE.FIT_FLOAT : FREE_IMAGE_TYPE.FIT_RGBAF;
                        break;

                    case FREE_IMAGE_TYPE.FIT_RGB16:
                    case FREE_IMAGE_TYPE.FIT_RGBA16:
                        destType = isGrayscale ? FREE_IMAGE_TYPE.FIT_FLOAT : FREE_IMAGE_TYPE.FIT_RGBAF;
                        break;

                    case FREE_IMAGE_TYPE.FIT_BITMAP:
                        if (isGrayscale)
                        {
                            if (Mathf.IsPowerOfTwo(header.biWidth) && Mathf.IsPowerOfTwo(header.biHeight))
                            {
                                if (!premultiplyAlpha)
                                {
                                   dib = FreeImage.ConvertToGreyscale(dib);
                                }
                            }
                            else
                            {
                                //int w = Mathf.NextPowerOfTwo(header.biWidth);
                                //int h = Mathf.NextPowerOfTwo(header.biHeight);
                                //FIBITMAP bitmap2 = FreeImage.Allocate(w, h, 8);
                                //FreeImage.Paste(bitmap2, dib, 0, 0, 255);
                                //FreeImage.UnloadEx(ref dib);
                                //dib = bitmap2;

                                forceGrayscaleAfterTexture2D = true;
                                dib = FreeImage.ConvertTo32Bits(dib);
                            }
                        }
                        else
                        {
                            dib = FreeImage.ConvertTo32Bits(dib);
                        }
                        destType = FREE_IMAGE_TYPE.FIT_BITMAP;

                        break;
                }

                //// premultiply if need be
                //if (premultiplyAlpha)
                //    FreeImage.PreMultiplyWithAlpha(dib);

                // convert to destination expected type
                if (destType != FREE_IMAGE_TYPE.FIT_UNKNOWN && origInputType != destType)
                {
                    Debug.Log("Trying to convert from:" + origInputType+ ", to:"+destType);
                    dib = FreeImage.ConvertToType(dib, destType, false);
                }
                //GC.Collect();
                Debug.Log("Used Heap Size After FreeImage.ConvertToType: " + Profiler.GetMonoUsedSizeLong() / 1024 / 1024);
                //if (isModifiedEXR && origInputType == FREE_IMAGE_TYPE.FIT_FLOAT)

                width = (int)FreeImageAPI.FreeImage.GetWidth(dib);
                height = (int)FreeImageAPI.FreeImage.GetHeight(dib);
                uint bpp = FreeImage.GetBPP(dib);
                int pitch = (int)FreeImage.GetPitch(dib);
                long byteSize = pitch * height;
                Debug.Log("byteSize: " + byteSize);
                FREE_IMAGE_TYPE inputType = FreeImage.GetImageType(dib);

                if (doMipMaps)
                    byteSize = (long)(byteSize * 1.6666f);

                //bytes = new byte[byteSize];
                FreeImage.ConvertToRawBits(bytes, dib, pitch, bpp, 0, 0, 0, false);

                Debug.Log("Used Heap Size After FreeImage.ConvertToRawBits: " + Profiler.GetMonoUsedSizeLong() / 1024 / 1024);


                FreeImage.UnloadEx(ref dib);
                //GC.Collect();
                //Debug.Log("Used Heap Size After FreeImage.UnloadEx: " + Profiler.GetMonoUsedSizeLong() / 1024 / 1024);
                // choose texture format
                formatToLoad = TextureFormat.ARGB32;

                Debug.Log("inputType:" + inputType);
                switch (inputType)
                {
                    case FREE_IMAGE_TYPE.FIT_FLOAT:
                        formatToLoad = TextureFormat.RFloat;
                        if (origInputType == FREE_IMAGE_TYPE.FIT_UINT16)
                        {
                            rtFormat = RenderTextureFormat.RHalf;
                        }
                        else
                        {
                            rtFormat = RenderTextureFormat.RFloat;
                        }
                        break;
                    case FREE_IMAGE_TYPE.FIT_UINT16:
                        formatToLoad = TextureFormat.RHalf;
                        rtFormat = RenderTextureFormat.RHalf;
                        break;
                    case FREE_IMAGE_TYPE.FIT_RGBA16:
                        formatToLoad = TextureFormat.RGBAHalf;
                        rtFormat = RenderTextureFormat.ARGBHalf;
                        isLinear = true;
                        break;
                    case FREE_IMAGE_TYPE.FIT_RGBAF:
                        formatToLoad = TextureFormat.RGBAFloat;

                        if (origInputType == FREE_IMAGE_TYPE.FIT_RGBA16 || origInputType == FREE_IMAGE_TYPE.FIT_RGB16)
                        {
                            rtFormat = RenderTextureFormat.ARGBHalf;
                        }
                        else
                        {
                            rtFormat = RenderTextureFormat.ARGBFloat;
                        }
                        isLinear = true;
                        break;

                    case FREE_IMAGE_TYPE.FIT_BITMAP:
                        //Iterate over all scanlines

                        switch (bpp)
                        {
                            case 8:

                                {
                                    formatToLoad = TextureFormat.Alpha8;
                                    rtFormat = RenderTextureFormat.R8;
                                }
                                break;
                            case 16:
                                formatToLoad = TextureFormat.RGBA4444;
                                rtFormat = RenderTextureFormat.ARGB4444;
                                break;
                            case 24:
                                if (FreeImage.IsLittleEndian())
                                {
                                    int length = bytes.Length;
                                    // make sure it's a multiple of 3
                                    int factor = length / 3;
                                    int adjustedLength = factor * 3;
                                    // convert back to RGB
                                    for (int i = 0; i < adjustedLength; i += 3)
                                    {
                                        // convert BGR to RGB
                                        var r = bytes[i];
                                        bytes[i] = bytes[i + 2];
                                        bytes[i + 2] = r;

                                    }
                                }
                                formatToLoad = TextureFormat.RGB24;
                                rtFormat = RenderTextureFormat.ARGB32;
                                break;

                            case 32:
                                if (forceGrayscaleAfterTexture2D)
                                {
                                    formatToLoad = TextureFormat.ARGB32;
                                    rtFormat = RenderTextureFormat.R8;
                                }
                                else
                                {
                                    if (FreeImage.IsLittleEndian())
                                    {
                                        int length = bytes.Length;
                                        // make sure it's a multiple of 4
                                        int factor = length / 4;
                                        int adjustedLength = factor * 4;
                                        for (int j = 0; j < adjustedLength; j += 4)
                                        {
                                            // convert BGRA to ARGB
                                            var a = bytes[j];
                                            var r = bytes[j + 1];
                                            bytes[j] = bytes[j + 3];
                                            bytes[j + 1] = bytes[j + 2];
                                            bytes[j + 2] = r;
                                            bytes[j + 3] = a;
                                        }
                                    }
                                    formatToLoad = TextureFormat.ARGB32;
                                    rtFormat = RenderTextureFormat.ARGB32;
                                }
                                break;
                        }
                        break;
                }
                successfullyLoadedRaw = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Exception: " + ex.Message);
            }
        }
        //);
        //newThread.IsBackground = true;
        //newThread.Start();
        //newThread.Join();
        //outBPP = origBPP;
        if (successfullyLoadedRaw)
        {
            RenderTexture temp = LoadRawToTexture2D(bytes, width, height, formatToLoad, rtFormat, doMipMaps, isLinear, forceGC, premultiplyAlpha);
            //GC.Collect();
            Debug.Log("Used Heap Size After LoadRawToTexture2D: " + Profiler.GetMonoUsedSizeLong() / 1024 / 1024);

            return temp;
        }
        return null;
    }

    private void LogRenderTextureRedRange(RenderTexture tex)
    {
        //float bright = 0;
        //float dark = 0;
        //TextureHelper.DisplacementRange(tex, ref dark, ref bright);
        //Debug.Log(string.Format("Red dark point: {0}, Red bright point: {1}", dark, bright));
    }

    private RenderTexture LoadRawToTexture2D(byte[] bytes, int width, int height, TextureFormat formatToLoad, RenderTextureFormat rtFormat, bool doMipMaps, bool isLinear, bool forceGC, bool premultiplyAlpha)
    {
        // Load Byte data into a Texture2D
        Debug.Log("Tex2DFormat: " + formatToLoad);
        var tex = new Texture2D(width, height, formatToLoad, mipmap: doMipMaps, linear: isLinear);
        tex.LoadRawTextureData(bytes);
        tex.Apply(doMipMaps, true);

        // Blit to RenderTexture
        RenderTexture tempRT = new RenderTexture(tex.width, tex.height, 0, rtFormat, isLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
        tempRT.useMipMap = doMipMaps;
        tempRT.wrapMode = TextureWrapMode.Repeat;
        Debug.Log("RTFormat: " + rtFormat);
        if (rtFormat == RenderTextureFormat.R8)
        {
            Graphics.Blit(tex, tempRT, AToRMaterial);
        }
        else
        {
            if (premultiplyAlpha)
                Graphics.Blit(tex, tempRT, PremultiplyMaterial);
            else
                Graphics.Blit(tex, tempRT);
        }

        // Always unload bitmap
        //tex = null;
        Destroy(tex);
        bytes = null;
        //One or both of these helps keeping the memory down and not get crashes on OpenGL when loading many big layers
        if (forceGC)
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }
//#if UNITY_EDITOR
//        LogRenderTextureRedRange(tempRT);
//#endif

        return tempRT;
    }

    // public Texture2D testTexture;
    public void SaveImage(Texture2D texture, string imagePath, ChannelsPerMap exportChannels, bool convertTo16 = false, bool waitForThread = false)
    {
      //  testTexture = texture;
        var format = texture.format;
        FREE_IMAGE_SAVE_FLAGS saveFlags = FREE_IMAGE_SAVE_FLAGS.DEFAULT;
        FREE_IMAGE_FORMAT destFormat = FREE_IMAGE_FORMAT.FIF_BMP;
        var saveType = System.IO.Path.GetExtension(imagePath);
        switch (saveType)
        {
            case ".png":
                destFormat = FREE_IMAGE_FORMAT.FIF_PNG;
                saveFlags = FREE_IMAGE_SAVE_FLAGS.PNG_Z_BEST_SPEED;
                if (format == TextureFormat.RGBAFloat && !convertTo16)
                {
                    Debug.LogError("Can't save HDR image as PNG");
                    return;
                    //dib = FreeImage.TmoDrago03(dib, 1, 1);
                }
                break;

            case ".exr":
                destFormat = FREE_IMAGE_FORMAT.FIF_EXR;
                saveFlags = FREE_IMAGE_SAVE_FLAGS.EXR_FLOAT | FREE_IMAGE_SAVE_FLAGS.EXR_PIZ;
                break;

            case ".tif":
            case ".tiff":
                destFormat = FREE_IMAGE_FORMAT.FIF_TIFF;
                saveFlags = FREE_IMAGE_SAVE_FLAGS.TIFF_LZW;
                break;

            case ".tga":
                destFormat = FREE_IMAGE_FORMAT.FIF_TARGA;
                saveFlags = FREE_IMAGE_SAVE_FLAGS.EXR_NONE;         // same value as TARGA_SAVE_RLE (not present in FreeImage.NET for some reason)
                if (format == TextureFormat.RGBAFloat)
                {
                    Debug.LogError("Can't save HDR image as TGA");
                    return;
                }
                break;

            case ".psd":
                destFormat = FREE_IMAGE_FORMAT.FIF_PSD;
                break;

            case ".jpg":
            case ".jpeg":
                destFormat = FREE_IMAGE_FORMAT.FIF_JPEG;
                saveFlags = FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYSUPERB | FREE_IMAGE_SAVE_FLAGS.JPEG_SUBSAMPLING_420 | FREE_IMAGE_SAVE_FLAGS.JPEG_OPTIMIZE;
                break;

        }
        Debug.Log("destFormat: " + destFormat);

        //int bppDest = 0;
        //int bppSource = 0;
        var rawBytes = texture.GetRawTextureData();
        
        Debug.Log("texture2d.width, texture2d.height, format:" + texture.width + ", " + texture.height + " , " + format);

        int texwidth = texture.width;
        int texheight = texture.height;
        Destroy(texture);
        Resources.UnloadUnusedAssets();
        
        //Create new thread then save image to file
        if (waitForThread || texwidth >= 8096)
        {
            Debug.Log("Saving image synchronously.");
            SaveHelper(format, rawBytes, texwidth, texheight, convertTo16, exportChannels, imagePath, destFormat, saveFlags);
        }
        else
        {
            new System.Threading.Thread(() =>
            {
                Debug.Log("Saving image asynchronously with threads.");
                SaveHelper(format, rawBytes, texwidth, texheight, convertTo16, exportChannels, imagePath, destFormat, saveFlags);
            }).Start();
        }
            //if (waitForThread)
            //{
            //    newThread.Join();
            //}
        
       
    }

    private void SaveHelper(TextureFormat format, byte[] rawBytes, int texwidth, int texheight, bool convertTo16,
                        ChannelsPerMap exportChannels, string imagePath, FREE_IMAGE_FORMAT destFormat, FREE_IMAGE_SAVE_FLAGS saveFlags)
    {
        int bytesPerPixel = 4;
        FREE_IMAGE_TYPE imageType = FREE_IMAGE_TYPE.FIT_BITMAP;

        switch (format)
        {
            case TextureFormat.RGBAHalf:
                imageType = FREE_IMAGE_TYPE.FIT_RGBA16;
                bytesPerPixel = 8;
                break;
            case TextureFormat.RGBAFloat:
                imageType = FREE_IMAGE_TYPE.FIT_RGBAF;
                bytesPerPixel = 16;
                break;

            case TextureFormat.ARGB32:
                imageType = FREE_IMAGE_TYPE.FIT_BITMAP;
                bytesPerPixel = 4;
                //tex.GetPixels32();
                //ConvertBGRAtoARGBScanline(dib);
                // convert back to ARGB
                if (FreeImage.IsLittleEndian())
                {
                    for (int j = 0; j < rawBytes.Length; j += 4)
                    {
                        // convert BGRA to ARGB
                        var a = rawBytes[j];
                        var r = rawBytes[j + 1];
                        rawBytes[j] = rawBytes[j + 3];
                        rawBytes[j + 1] = rawBytes[j + 2];
                        rawBytes[j + 2] = r;
                        rawBytes[j + 3] = a;
                    }
                }
                break;

            case TextureFormat.RGB24:
                imageType = FREE_IMAGE_TYPE.FIT_BITMAP;
                bytesPerPixel = 3;
                if (FreeImage.IsLittleEndian())
                {
                    // convert back to RGB
                    for (int i = 0; i < rawBytes.Length; i += 3)
                    {
                        // convert BGR to RGB
                        var r = rawBytes[i];
                        rawBytes[i] = rawBytes[i + 2];
                        rawBytes[i + 2] = r;

                    }
                }
                break;

        }



        FIBITMAP dib = FreeImage.ConvertFromRawBits(rawBytes, imageType, texwidth, texheight, texwidth * bytesPerPixel, (uint)bytesPerPixel * 8, 0, 0, 0, false);

        if (dib.IsNull)
        {
            Debug.LogError("Dib is NULL!!!");
        }
        rawBytes = null;
        GC.Collect();
        if (convertTo16)
        {
            dib = FreeImage.ConvertToType(dib, FREE_IMAGE_TYPE.FIT_RGBA16, false);
            format = TextureFormat.RGBAHalf;
        }

        switch (exportChannels)
        {
            case ChannelsPerMap.RGB:
                // remove alpha channel
                switch (format)
                {
                    case TextureFormat.RGBAFloat:
                        dib = FreeImage.ConvertToRGBF(dib);
                        break;

                    case TextureFormat.RGBAHalf:
                        dib = FreeImage.ConvertToType(dib, FREE_IMAGE_TYPE.FIT_RGB16, false);
                        break;

                    case TextureFormat.ARGB32:
                        dib = FreeImage.ConvertTo24Bits(dib);
                        break;
                }
                break;

            case ChannelsPerMap.R:
                dib = FreeImage.GetChannel(dib, FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
                break;

            // if already RGBA don't need to do any conversion
            default:
                break;

        }

        try
        {
            using (FileStream saveStream = new FileStream(imagePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                Debug.Log("FreeImage::FileSaveSuccess: " + imagePath + " :" + FreeImage.SaveToStream(ref dib, saveStream, destFormat, saveFlags, true));
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            //progressBar.DoneProgress();
            FreeImage.UnloadEx(ref dib);
            throw;
        }
        //if (progressBar != null)
        //{
        //    UnityThreadHelper.Dispatcher.Dispatch(() =>
        //    {
        //        progressBar.Increment();
        //    });
        //}
    }
}

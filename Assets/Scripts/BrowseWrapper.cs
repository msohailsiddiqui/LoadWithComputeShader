using SFB;
using System.Collections;
using System.IO;
using UnityEngine.Events;

#if UNITY_EDITOR_OSX
using UnityEditor;
#endif

public class BrowseWrapper : Singleton<BrowseWrapper>
{
    //BrowseManager is wrapping filebrowsing for OSX and Windows, triggering a callback function with a string parameter
    //Using CustomBrowser for Win
    //Using native browser for OSX

    protected BrowseWrapper() { } // guarantee this will be always a singleton only - can't use the constructor!
    public enum Type {Folder,FileOpen,FileSave}
    private UnityAction<string> callback;
    private string requiredPath;

    private readonly ExtensionFilter[] ImageExtensions = new ExtensionFilter[]
                        {
                            new ExtensionFilter("Images", "png", "jpg", "jpeg", "tif", "tiff", "exr", "tga" ),
                            new ExtensionFilter("PNG", "png"),
                            new ExtensionFilter("JPEG", "jpg", "jpeg"),
                            new ExtensionFilter("TIF", "tif", "tiff"),
                            new ExtensionFilter("EXR", "exr"),
                            new ExtensionFilter("TGA", "tga")
                        };
    private readonly ExtensionFilter[] LicenseExtension = new ExtensionFilter[] { new ExtensionFilter("License File", "lic") };
    private readonly ExtensionFilter[] MeshExtensions = new ExtensionFilter[]
                        {
                            new ExtensionFilter("Mesh Files", "obj", "dae", "3ds", "fbx", "m3ds", "blend"),
                            new ExtensionFilter("OBJ", "obj"),
                            new ExtensionFilter("FBX", "fbx"),
                            new ExtensionFilter("DAE", "dae"),
                            new ExtensionFilter("3ds", "3ds"),
                            new ExtensionFilter("m3ds", "m3ds"),
                            new ExtensionFilter("blend", "blend"),
                        };
    private readonly ExtensionFilter[] ZipFiles = new ExtensionFilter[] { new ExtensionFilter("Megascans Zip Files", "zip") };
    private readonly ExtensionFilter[] SkyboxFiles = new ExtensionFilter[] { new ExtensionFilter("Custom Skyboxes", "hdr", "exr") };

    private readonly ExtensionFilter[] AllFiles = new ExtensionFilter[] { new ExtensionFilter("All Files", "*") };

    public enum ExtensionFilterPreset
    {
        Images,
        Meshes,
        License,
        MegascansZips,
        Skyboxes,
        Custom,
    }

    // new standalone browser
    public void Browse(UnityAction<string> callbackFunction, Type browseType, string browserTitle, string root = "", ExtensionFilterPreset extensionFilterPreset=ExtensionFilterPreset.Custom, ExtensionFilter[] customExtensionFilters=null, bool multiSelect=false)
    {
        var extensionFilters = customExtensionFilters;
        // setup extension filters
        switch(extensionFilterPreset)
        {
            case ExtensionFilterPreset.Images:
                extensionFilters = ImageExtensions;
                break;

            case ExtensionFilterPreset.Meshes:
                extensionFilters = MeshExtensions;
                break;

            case ExtensionFilterPreset.License:
                extensionFilters = LicenseExtension;
                break;

            case ExtensionFilterPreset.MegascansZips:
                extensionFilters = ZipFiles;
                break;

            case ExtensionFilterPreset.Skyboxes:
                extensionFilters = SkyboxFiles;
                break;

            case ExtensionFilterPreset.Custom:
            default:
                if (extensionFilters == null)
                    extensionFilters = AllFiles;
                break;
        }
        string[] paths = new string[] { "" };
        switch (browseType)
        {
            case Type.FileOpen:
                paths = StandaloneFileBrowser.OpenFilePanel(browserTitle, root, extensionFilters, multiSelect);
                break;

            case Type.Folder:
                paths = StandaloneFileBrowser.OpenFolderPanel(browserTitle, root, multiSelect);
                break;

            // placeholder implementation as this is not used up till now
            case Type.FileSave:
                string savePath = StandaloneFileBrowser.SaveFilePanel(browserTitle, root, "defaultSave", extensionFilters);
                paths = new string[] { savePath };
                break;
        }

        // Run callback for all returned paths
        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		path = UnityEngine.WWW.UnEscapeURL(path.Replace("file://", ""));
#endif
            if (callbackFunction != null && !string.IsNullOrEmpty(path))
                callbackFunction(path);
        }
    }


}

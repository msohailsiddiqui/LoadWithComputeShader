using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadSaveMain : MonoBehaviour
{
    public GameObject quadPrefab;
    public List<string> filesToLoad;
    private List<GameObject> quadObjs;
    private List<Material> quadMaterials;
    private List<RenderTexture> loadedMaps;

    private RenderTexture loadedMap;
    private GameObject singleQuad;
    private Material singleQuadMat;

    //private string filePath1 = "D:\\SOS\\Work\\Documents\\test case scans\\clover grass\\QvmG7sfy50yXazkr9awV3g_4K_Diffuse.exr";
    //private string filePath2 = "D:\\SOS\\Work\\Documents\\test case scans\\Grass_rmnkhgp0\\rmnkhgp_4K_Albedo.exr";
    //private string filePath3 = "D:\\SOS\\Work\\Documents\\test case scans\\Ground_Forest_sbhit3p0\\sbhit3p_4K_Albedo.exr";
    //private string filePath4 = "D:\\SOS\\Work\\Documents\\test case scans\\Ground_Grass_pjwfo0\\pjwfo_4K_Albedo.exr";
    //private string filePath5 = "D:\\SOS\\Work\\Documents\\test case scans\\Soil_Mulch_sfwnbgba\\sfwnbgba_8K_Albedo.exr";
    //private string filePath6 = "D:\\SOS\\Work\\Documents\\test case scans\\Soil_Mulch_sfwnbgba\\sfwnbgba_8K_Normal.exr";

    private int mapToLoadIndex;

    // Use this for initialization
    void Start()
    {
        FreeImageManager.Instance.Test();

        quadObjs = new List<GameObject>();
        quadMaterials = new List<Material>();
        loadedMaps = new List<RenderTexture>();

        int xIncrement = 2;
        int yIncrement = 0;
        int index = 0;

        foreach (string file in filesToLoad)
        {
            if(!string.IsNullOrEmpty( file))
            {
                GameObject tempObj = GameObject.Instantiate(quadPrefab);
                if(xIncrement > 6)
                {
                    xIncrement = 0;
                    yIncrement+=2;
                }
                tempObj.transform.position = new Vector3(tempObj.transform.position.x + xIncrement, tempObj.transform.position.y - yIncrement, tempObj.transform.position.z);
                xIncrement += 2;
                tempObj.name = "Map" + index;
                index++;
                if(tempObj != null)
                {
                    quadObjs.Add(tempObj);
                    Renderer tempRenderer = tempObj.GetComponent<Renderer>();
                    if(tempRenderer != null)
                    {
                        tempRenderer.material = new Material(Shader.Find("Standard"));
                        quadMaterials.Add(tempRenderer.material);
                    }
                }
            }
        }

        singleQuad = GameObject.Instantiate(quadPrefab);
        if(singleQuad != null)
        {
            Renderer tempRenderer = singleQuad.GetComponent<Renderer>();
            if (tempRenderer != null)
            {
                singleQuadMat = tempRenderer.material;
            }
            
        }

        mapToLoadIndex = 0;
    }

    public void CreateRT8K()
    {
        // This function was to test if creating a render texture alone contributes to the used heap
        // Yes it does
        loadedMap = new RenderTexture(8192, 8192, 0, RenderTextureFormat.ARGBFloat);
        loadedMap.Create();
    }
   
    public void LoadImage()
    {
        BrowseWrapper.Instance.Browse(callbackFunction: LoadImageFromDisk,
            browseType: BrowseWrapper.Type.FileOpen, browserTitle: "Choose image to load",
            root: "", extensionFilterPreset: BrowseWrapper.ExtensionFilterPreset.Images, multiSelect: true);
    }

    public void UnloadImage()
    {
        if(loadedMap != null )
        {
            if(singleQuadMat != null)
            {
                singleQuadMat.mainTexture = null;
            }
            loadedMap.Release();
            Destroy(loadedMap);
        }
    }

    public void UnloadAllMaps()
    {
        if (loadedMaps.Count > 0)
        {
            foreach(RenderTexture rt in loadedMaps)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
            foreach(Material mat in quadMaterials)
            {
                mat.mainTexture = null;
            }
        }
    }

    private void LoadImageFromDisk(string filePath)
    {
        loadedMap = FreeImageManager.Instance.LoadImage(filePath, isLinear: false);
        if (loadedMap != null && singleQuadMat != null)
        {
            singleQuadMat.mainTexture = loadedMap;
        }
    }

    public void LoadMultipleImagesFromDiskOneAtATime()
    {
        if (filesToLoad.Count > 0 && filesToLoad.Count >= mapToLoadIndex && !string.IsNullOrEmpty(filesToLoad[mapToLoadIndex]))
        {
            if (loadedMaps.Count >= mapToLoadIndex)
            {
                if (loadedMaps.Count > mapToLoadIndex)
                {
                    if (loadedMaps[mapToLoadIndex] != null)
                    {
                        loadedMaps[mapToLoadIndex].Release();
                        Destroy(loadedMaps[mapToLoadIndex]);
                        loadedMaps[mapToLoadIndex] = null;
                    }
                    loadedMaps[mapToLoadIndex] = FreeImageManager.Instance.LoadImage(filesToLoad[mapToLoadIndex], isLinear: false);
                    quadMaterials[mapToLoadIndex].mainTexture = loadedMaps[mapToLoadIndex];
                }
                else
                {
                    loadedMaps.Add(FreeImageManager.Instance.LoadImage(filesToLoad[mapToLoadIndex], isLinear: false));
                    quadMaterials[mapToLoadIndex].mainTexture = loadedMaps[mapToLoadIndex];
                }
                mapToLoadIndex++;
            }
            if (mapToLoadIndex >= filesToLoad.Count)
            {
                mapToLoadIndex = 0;
            }
        }
    }

    public void LoadMultipleImagesFromDisk()
    {
        int index = 0;
        foreach (string file in filesToLoad)
        {
            if (!string.IsNullOrEmpty(filesToLoad[index]))
            {
                if (loadedMaps.Count > index)
                {
                    if (loadedMaps[index] != null)
                    {
                        loadedMaps[index].Release();
                        Destroy(loadedMaps[index]);
                        loadedMaps[index] = null;
                    }
                    loadedMaps[index] = FreeImageManager.Instance.LoadImage(filesToLoad[index], isLinear: false);
                    quadMaterials[index].mainTexture = loadedMaps[index];
                }
                else
                {
                    loadedMaps.Add(FreeImageManager.Instance.LoadImage(filesToLoad[index], isLinear: false));
                    quadMaterials[index].mainTexture = loadedMaps[index];
                }
                index++;
            }
        }
        mapToLoadIndex = index;
    }

    public void CollectGarbage()
    {
        System.GC.Collect();
    }

    private void OnApplicationQuit()
    {
        UnloadImage();
        UnloadAllMaps();
    }
}

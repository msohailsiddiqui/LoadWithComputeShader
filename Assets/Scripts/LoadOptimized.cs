using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

public class LoadOptimized : MonoBehaviour
{
    public GameObject quadPrefab;
    public Text memoryInfoText;
    private List<GameObject> quadObjs;
    private List<Material> quadMaterials;
    private List<RenderTexture> loadedMaps;
    private int xIncrement = 0;
    private int yIncrement = 0;
    private int numLoadedMaps = 0;
    private string memoryInfoString;

    private float loadStartTime;
    private float totalLoadTime;

    // Use this for initialization
    void Start()
    {
        FreeImageManager.Instance.Test();

        quadObjs = new List<GameObject>();
        quadMaterials = new List<Material>();
        loadedMaps = new List<RenderTexture>();
        UpdateMemoryInfo();
    }
    void UpdateMemoryInfo()
    {
        memoryInfoString = "MonoUsedSize: " + Get2FString(Profiler.GetMonoUsedSizeLong()) + 
            "\nMonoHeapSize: " + Get2FString(Profiler.GetMonoHeapSizeLong())
                    + "\nTempAllocatorSize: " + Get2FString(Profiler.GetTempAllocatorSize()) 
                    + "\nTotalAllocatedMemory: " + Get2FString(Profiler.GetTotalAllocatedMemoryLong())
                + "\nTotalReservedMemory: " + Get2FString(Profiler.GetTotalReservedMemoryLong()) 
                + "\nTotalUnusedReservedMemory: " + Get2FString(Profiler.GetTotalUnusedReservedMemoryLong())
                + "\n UsedHeapSize: " + Get2FString(Profiler.usedHeapSizeLong);
        if(memoryInfoText != null)
        {
            memoryInfoText.text = memoryInfoString;
        }
    }

    string Get2FString(long value)
    {
        return (value / 1024.0f / 1024.0f).ToString("F2");
    }

    private void OnDestroy()
    {
        UnloadAllMaps();
    }

    public void LoadImage()
    {
        BrowseWrapper.Instance.Browse(callbackFunction: LoadImageFromDisk,
            browseType: BrowseWrapper.Type.FileOpen, browserTitle: "Choose image to load",
            root: "", extensionFilterPreset: BrowseWrapper.ExtensionFilterPreset.Images, multiSelect: true);
    }

    public void UnloadAllMaps()
    {
        if (loadedMaps.Count > 0)
        {
            foreach (RenderTexture rt in loadedMaps)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
            loadedMaps.Clear();
            foreach (Material mat in quadMaterials)
            {
                mat.mainTexture = null;
            }
            quadMaterials.Clear();
            foreach(GameObject go in quadObjs)
            {
                Destroy(go);
            }
            quadObjs.Clear();
            numLoadedMaps = 0;
        }

        UpdateMemoryInfo();
    }

    private void LoadImageFromDisk(string filePath)
    {
        loadStartTime = Time.realtimeSinceStartup;
        // First try to load the image
        RenderTexture rtHandle = FreeImageManager.Instance.LoadImage(filePath, isLinear: false);
        if (rtHandle != null)
        {
            //First Create a new quad to show the Image
            GameObject tempObj = GameObject.Instantiate(quadPrefab);
            if (xIncrement > 6)
            {
                xIncrement = 0;
                yIncrement += 2;
            }
            tempObj.transform.position = new Vector3(tempObj.transform.position.x + xIncrement, tempObj.transform.position.y - yIncrement, tempObj.transform.position.z);
            xIncrement += 2;
            tempObj.name = "Map" + numLoadedMaps;
            if (tempObj != null)
            {
                quadObjs.Add(tempObj);
                Renderer tempRenderer = tempObj.GetComponent<Renderer>();
                if (tempRenderer != null)
                {
                    tempRenderer.material = new Material(Shader.Find("Standard"));
                    quadMaterials.Add(tempRenderer.material);
                }

                TextMesh text = tempObj.GetComponentInChildren<TextMesh>();
                totalLoadTime = Time.realtimeSinceStartup - loadStartTime;
                if(text != null)
                {

                    text.text = totalLoadTime.ToString("F2");
                }
            }

            quadMaterials[numLoadedMaps].mainTexture = rtHandle;
            loadedMaps.Add(rtHandle);
            numLoadedMaps++;
        }
        UpdateMemoryInfo();

    }

    public void CollectGarbage()
    {
        System.GC.Collect();
        UpdateMemoryInfo();
    }

    private void OnApplicationQuit()
    {
        UnloadAllMaps();
    }
}

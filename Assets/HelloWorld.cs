using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HelloWorld : MonoBehaviour
{
    public SampleScriptableObject Config;
    // Start is called before the first frame update
    void Start()
    {
        string assetBundlePath = Path.Combine(Application.dataPath, "StreamingAssets", "1.2.0", "af780706b3a7696827b8c5f6462f0e98");
        AssetBundle sampleAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        AssetBundleManifest manifest = sampleAssetBundle.LoadAsset<AssetBundleManifest>(nameof(AssetBundleManifest));

        foreach(string dependName in manifest.GetAllDependencies(sampleAssetBundle.name))
        {
            Debug.Log($"DependName{dependName}");
        }
        foreach (string name in sampleAssetBundle.GetAllAssetNames())
        {
            Debug.Log(name);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

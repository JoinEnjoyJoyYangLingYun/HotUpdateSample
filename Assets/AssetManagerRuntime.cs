using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class BuildInfo
{
    public int BuildVersion;
    public Dictionary<string, ulong> FileNames = new Dictionary<string, ulong>();
    public ulong FileTotalSize;
}
/// <summary>
/// ��Ӧ��EditorWindow�����ϴ������
/// </summary>
public class AssetPackage
{
    public PackageInfo Package;
    public string PackageName { get { return Package.PackageName; } }

    /// <summary>
    /// ��ǰ�����Ѽ��س�����Դ
    /// </summary>
    public Dictionary<string, Object> LoadedAssets = new Dictionary<string, Object>(); 

    public T LoadAsset<T>(string assetName) where T: Object
    {
        T assetObject = default;

        foreach(AssetInfo info in Package.assetInfos)
        {
            if (info.AssetName == assetName)
            {
                if (LoadedAssets.Keys.Contains(assetName))
                {
                    return LoadedAssets[assetName] as T;
                }

                foreach (string dependAssetBundle in AssetManagerRuntime.instance.MainBundleManifest.GetAllDependencies(info.AssetBundleName))
                {
                    string dependPath = Path.Combine(AssetManagerRuntime.instance.AssetBundleLoadPath, dependAssetBundle);

                    AssetBundle.LoadFromFile(dependPath);
                }

                string assetBundlePath = Path.Combine(AssetManagerRuntime.instance.AssetBundleLoadPath, info.AssetBundleName);

                AssetBundle bundle = AssetBundle.LoadFromFile(assetBundlePath);

                assetObject = bundle.LoadAsset<T>(assetName);

            }
        }
        if (assetObject == null)
        {
            Debug.LogError($"û���ҵ�{assetName}");
        }
        return assetObject;
    }
}

public class AssetManagerRuntime 
{
    /// <summary>
    /// ��ǰ��ĵ�������
    /// 
    /// </summary>
    public static AssetManagerRuntime instance;

    /// <summary>
    /// AB������ģʽ
    /// </summary>
    AssetBundlePattern CurrentPattern;

    /// <summary>
    /// AB�����ؼ���·��
    /// </summary>
    public string AssetBundleLoadPath;

    /// <summary>
    /// ������Դ·��
    /// </summary>
    public string LocalAssetPath;

    /// <summary>
    /// Asset���ص�ַ
    /// </summary>
    public string DownloadPath;

    /// <summary>
    /// ������Դ�汾
    /// </summary>
    public int LocalAssetVersion;

    /// <summary>
    /// Զ����Դ�汾
    /// </summary>
    public int RemoteAssetVersion;

    /// <summary>
    /// �������а����б��ļ�
    /// </summary>
    List<string> LocalAllPackages;


    /// <summary>
    /// �Ѽ��صİ��б�,�����ظ�����
    /// </summary>
    Dictionary<string, AssetPackage> LoadedPackages = new Dictionary<string, AssetPackage>();

    /// <summary>
    /// ������Manifest
    /// </summary>
    public AssetBundleManifest MainBundleManifest;

    public const string LocalAssetFolderName = "LocalAssets";

    /// <summary>
    /// HTTP��Դ��������ַ
    /// </summary>
    public string HTTPAddress = "http://192.168.31.236:8080/";

    public static void AssetManagerInit(AssetBundlePattern assetBundlePattern)
    {
        if (instance == null)
        {
            instance = new AssetManagerRuntime();
        }
        instance.CurrentPattern = assetBundlePattern;

        instance.CheckAssetBundlePath();
        instance.CheckLocalAssetVersion();
        instance.CheckAssetBundleLoadPath();

    }


    void CheckAssetBundlePath()
    {
        switch (CurrentPattern)
        {
            case AssetBundlePattern.EditorSimulation:
                //AssetBundleLoadPath = Path.Combine(Application.persistentDataPath,LocalAssetFolderName);
                break;
            case AssetBundlePattern.Local:
                LocalAssetPath = Path.Combine(Application.streamingAssetsPath, LocalAssetFolderName);
                break;
            case AssetBundlePattern.Remote:
                DownloadPath = Path.Combine(Application.persistentDataPath, "DownloadAsset");
                if (!Directory.Exists(DownloadPath))
                {
                    Directory.CreateDirectory(DownloadPath);
                }
                LocalAssetPath = Path.Combine(Application.persistentDataPath,LocalAssetFolderName);
                break;
        }
        if (!Directory.Exists(LocalAssetPath))
        {
            Directory.CreateDirectory(LocalAssetPath);
        }
    }
     void CheckLocalAssetVersion()
    {
        string versionFilePath = Path.Combine(LocalAssetPath, "LocalVersion.version");

        if (!File.Exists(versionFilePath))
        {
            LocalAssetVersion = 100;
            File.WriteAllText(versionFilePath, LocalAssetVersion.ToString());
            return;
        }

        LocalAssetVersion = int.Parse(File.ReadAllText(versionFilePath));
    }
    void CheckAssetBundleLoadPath()
    {
        switch (CurrentPattern)
        {
            case AssetBundlePattern.EditorSimulation:
                //AssetBundleLoadPath = Path.Combine(Application.persistentDataPath, "AssetBundles");
                break;
            case AssetBundlePattern.Local:
                //AssetBundleLoadPath = Path.Combine(LocalAssetPath, LocalAssetVersion.ToString());
                break;
            case AssetBundlePattern.Remote:
                //AssetBundleLoadPath = Path.Combine(LocalAssetPath, LocalAssetVersion.ToString());
                break;
        }
        AssetBundleLoadPath = Path.Combine(LocalAssetPath, LocalAssetVersion.ToString());
        if (!Directory.Exists(AssetBundleLoadPath))
        {
            Directory.CreateDirectory(AssetBundleLoadPath);
        }
    }


    public void UpdateLocalAssetVersion()
    {
        LocalAssetVersion = RemoteAssetVersion;
        string versionFilePath = Path.Combine(LocalAssetPath, "LocalVersion.version");
        File.WriteAllText(versionFilePath, LocalAssetVersion.ToString());
        CheckAssetBundleLoadPath();
        Debug.Log($"���ذ汾���½���,��ǰ�汾Ϊ{LocalAssetVersion}");
    }

    /// <summary>
    /// ����Package����
    /// </summary>
    /// <param name="PackageName"> ��Ҫ���صİ��� </param>
    /// <returns></returns>
    public AssetPackage LoadPackage(string PackageName)
    {
        if (LocalAllPackages == null)
        {
            string packageListPath = Path.Combine(AssetBundleLoadPath, "AllPackages");
            string packageListString = File.ReadAllText(packageListPath);

            LocalAllPackages = JsonConvert.DeserializeObject<List<string>>(packageListString);
        }

        if (!LocalAllPackages.Contains(PackageName))
        {
            Debug.LogError($"���ز�����{PackageName}��");
        }

        if (MainBundleManifest == null)
        {
            string mainBundlePath = Path.Combine(AssetBundleLoadPath, "BuildOutput");

            AssetBundle mainBundle = AssetBundle.LoadFromFile(mainBundlePath);
            MainBundleManifest = mainBundle.LoadAsset<AssetBundleManifest>(nameof(AssetBundleManifest));
        }

        if (LoadedPackages.Keys.Contains(PackageName))
        {
            Debug.LogWarning($"{PackageName}���Ѽ���");
            return LoadedPackages[PackageName];
        }

        AssetPackage package = new AssetPackage();
        string packagePath = Path.Combine(AssetBundleLoadPath, PackageName);
        string packageString = File.ReadAllText(packagePath);
        package.Package = JsonConvert.DeserializeObject<PackageInfo>(packageString);

        LoadedPackages.Add(PackageName,package);

        foreach(string dependPackageName in package.Package.PackageDependencies)
        {
            LoadPackage(dependPackageName);
        }
        return package;
    }
}

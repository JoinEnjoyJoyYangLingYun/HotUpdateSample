using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AssetBundleLoad : MonoBehaviour
{
    public AssetBundlePattern LoadPattern;

    AssetBundle SphereAssetBundle;
    AssetBundle CubeAssetBundle;
    GameObject SampleObject;

    public Button LoadAssetBundleButton;
    public Button LoadAssetButton;
    public Button UnloadAssetBundleFalseButton;
    public Button UnloadAssetBundleTrueButton;


    string AssetBundleLoadPath;

    //public static string AssetBundleName = "AssetBundles";
    //public static string ObjectBundleName = "sampleassetbundle.ab";

    string RemoteAssetBundlePath;

    string FileURL;
    string DownloadVersionPath;


    // Start is called before the first frame update
    void Start()
    {
        AssetManagerRuntime.AssetManagerInit(LoadPattern);
        if (LoadPattern == AssetBundlePattern.Remote)
        {
            StartCoroutine(GetRemoteBuildVersion());

        }
    }

    Downloader SampleDownloader;

    private void OnProgress(float prg, long currLength, long totalLength)
    {
        Debug.LogFormat("下载进度{0:0.00}%,{1}M/{2}M", (prg * 100), currLength * 1.0f / 1024 / 1024,
            totalLength * 1.0f / 1024 / 1024);
    }
    

    DownloadInfo CurrentDownloadInfo;
    private void OnCompleted(string fileName,string msg)
    {
        Debug.Log($"{fileName}{msg}");
        if (!CurrentDownloadInfo.DownloadedFileNames.Contains(fileName))
        {
            CurrentDownloadInfo.DownloadedFileNames.Add(fileName);
            string downloadInfoString = JsonConvert.SerializeObject(CurrentDownloadInfo);
            string downloadInfoPath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, "LocalDownloadInfo");
            File.WriteAllText(downloadInfoPath, downloadInfoString);
        }
        switch (fileName)
        {
            case "AllPackages":
                CreatePackageDownloadList();
                break;
            case "AssetBundleHashs":
                CreateAssetBundleDownloadList();
                break;
        }
    }


    private void OnError(ErrorCode code, string msg)
    {
    }
    void LoadAssetTest()
    {
        //从本地包列表中加载一个包
        AssetPackage package = AssetManagerRuntime.instance.LoadPackage("A");

        Debug.Log(package.PackageName);

        GameObject obj = package.LoadAsset<GameObject>("Assets/Scenes/Sphere.prefab");
        Instantiate(obj);
    }


    IEnumerator DownloadAssetBundle(List<string> fileNames, Action callBack = null)
    {
        foreach (string fileName in fileNames)
        {
            //因为Hash列表中的文件名是由文件大小和文件名构成,用下划线_进行划分
            //所以要从下划线后一位开始获取AssetBundle的具体名称
            string assetBundleName = fileName;
            if (fileName.Contains("_"))
            {
                int startIndex = fileName.IndexOf("_") + 1;
                assetBundleName = fileName.Substring(startIndex);
            }
            string fileDownloadPath = Path.Combine(AssetManagerRuntime.instance.HTTPAddress, "BuildOutput", AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), assetBundleName);

            UnityWebRequest request = UnityWebRequest.Get(fileDownloadPath);
            request.SendWebRequest();
            while (!request.isDone)
            {
                yield return null;
            }
            if (!string.IsNullOrEmpty(request.error))
            {
                Debug.Log(request.error);
                yield break;
            }
            string fileSavePath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), assetBundleName);
            File.WriteAllBytes(fileSavePath, request.downloadHandler.data);
            Debug.Log($"AssetBundle下载完成{fileName}");
        }
        callBack?.Invoke();
        yield return null;
    }

    void CopyDownloadAssetsToLocalFile()
    {
        string downloadVersionFilePath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString());

        DirectoryInfo directoryInfo = new DirectoryInfo(downloadVersionFilePath);

        string localAssetVersionPath = Path.Combine(AssetManagerRuntime.instance.LocalAssetPath, AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString());
        directoryInfo.MoveTo(localAssetVersionPath);
    }

    public void CreateDownloadList()
    {
        string downloadInfoPath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, "LocalDownloadInfo");
        CurrentDownloadInfo = null;
        if (File.Exists(downloadInfoPath))
        {
            string downloadInfoString= File.ReadAllText(downloadInfoPath);
            CurrentDownloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(downloadInfoString);
        }
        else
        {
            CurrentDownloadInfo = new DownloadInfo();
        }

        if (CurrentDownloadInfo.DownloadedFileNames.Contains("AllPackages"))
        {
            OnCompleted("AllPackages", "本地已存在");
        }
        else
        {
            string filePath = Path.Combine(FileURL, "AllPackages");
            string fileSavePath = Path.Combine(DownloadVersionPath, "AllPackages");
            Downloader downloader = new Downloader(filePath, fileSavePath, OnCompleted, OnProgress, OnError);
            downloader.Start();
        }    
    }

    /// <summary>
    /// 对比不同版本的哈希表
    /// 在服务器端和客户端上使用时可以获得需要下载的AssetBundle列表
    /// </summary>
    /// <param name="oldHashTable"></param>
    /// <param name="newHashTable"></param>
    /// <returns></returns>
    public AssetBundleVersionDiffrence ContrastAssetBundleHashTable(string[] oldHashTable, string[] newHashTable)
    {
        AssetBundleVersionDiffrence diffrence = new AssetBundleVersionDiffrence();
        diffrence.AdditionAssetBundles = new List<string>();
        diffrence.ReducedAssetBundles = new List<string>();
        //如果老的Hash列表中,有新Hash列表不包含的包,说明是需要移除的包
        foreach (string assetHash in oldHashTable)
        {
            if (!newHashTable.Contains(assetHash))
            {
                diffrence.ReducedAssetBundles.Add(assetHash);
            }
        }

        //如果新的Hash表中,有老的Hash表不包含的包,说明是新增的包
        foreach (string assetHash in newHashTable)
        {
            if (!oldHashTable.Contains(assetHash))
            {
                diffrence.AdditionAssetBundles.Add(assetHash);
            }
        }

        return diffrence;
    }

    void CreateAssetBundleDownloadList()
    {
        //本地表读取路径
        string assetBundleHashsLoadPath = Path.Combine(AssetManagerRuntime.instance.AssetBundleLoadPath, "AssetBundleHashs");

        string assetBundleHashsString = "";
        string[] localAssetBundleHashs = null;
        if (File.Exists(assetBundleHashsLoadPath))
        {
            assetBundleHashsString = File.ReadAllText(assetBundleHashsLoadPath);
            localAssetBundleHashs = JsonConvert.DeserializeObject<string[]>(assetBundleHashsString);
        }

        //远端表读取路径
        assetBundleHashsLoadPath = Path.Combine(DownloadVersionPath, "AssetBundleHashs");
        string[] remoteAssetBundleHashs = null;
        if (File.Exists(assetBundleHashsLoadPath))
        {
            assetBundleHashsString = File.ReadAllText(assetBundleHashsLoadPath);
            remoteAssetBundleHashs = JsonConvert.DeserializeObject<string[]>(assetBundleHashsString);
        }

        if (remoteAssetBundleHashs == null)
        {
            Debug.LogError("远端表读取失败,请查看文件是否存在");
            return;
        }

        List<string> assetBundleNames = null;
        if (localAssetBundleHashs == null)
        {
            Debug.LogWarning("本地表读取失败,直接下载远端资源");
            assetBundleNames = remoteAssetBundleHashs.ToList();
        }
        else
        {
            AssetBundleVersionDiffrence versionDiffrence = ContrastAssetBundleHashTable(localAssetBundleHashs, remoteAssetBundleHashs);
            assetBundleNames = versionDiffrence.AdditionAssetBundles;
        }
        assetBundleNames.Add("BuildOutput");
        foreach (string assetBundleName in assetBundleNames)
        {
            //因为Hash列表中的文件名是由文件大小和文件名构成,用下划线_进行划分
            //所以要从下划线后一位开始获取AssetBundle的具体名称
            string fileName = null;
            if (assetBundleName.Contains("_"))
            {
                int startIndex = assetBundleName.IndexOf("_") + 1;
                fileName = assetBundleName.Substring(startIndex);
            }
            else
            {
                fileName = assetBundleName;
            }
            if (!CurrentDownloadInfo.DownloadedFileNames.Contains(fileName))
            {
                string filePath = Path.Combine(FileURL, fileName);
                string fileSavePath = Path.Combine(DownloadVersionPath, fileName);
                Downloader downloader = new Downloader(filePath, fileSavePath, OnCompleted, OnProgress, OnError);
                downloader.Start();
            }
            else
            {
                OnCompleted(fileName, "本地已存在");
            }
        }
    }

    void CreatePackageDownloadList()
    {
        string downloadPackagePath = Path.Combine(DownloadVersionPath, "AllPackages");
        string filePath = "";
        Downloader downloader = null;
        string downloadPackageString= File.ReadAllText(downloadPackagePath);
        List<string> packageList = JsonConvert.DeserializeObject<List<string>>(downloadPackageString);
        foreach (string packageName in packageList)
        {
            if (!CurrentDownloadInfo.DownloadedFileNames.Contains(packageName))
            {
                filePath = Path.Combine(FileURL, packageName);
                string fileSavePath = Path.Combine(DownloadVersionPath, packageName);
                downloader = new Downloader(filePath, fileSavePath, OnCompleted, OnProgress, OnError);
                downloader.Start();
            }
            else
            {
                OnCompleted(packageName, "本地已存在");
            }
        }
        if (!CurrentDownloadInfo.DownloadedFileNames.Contains("AssetBundleHashs"))
        {
            filePath = Path.Combine(FileURL, "AssetBundleHashs");
            string fileSavePath = Path.Combine(DownloadVersionPath, "AssetBundleHashs");
            downloader = new Downloader(filePath, fileSavePath, OnCompleted, OnProgress, OnError);
            downloader.Start();
        }
        else
        {
            OnCompleted("AssetBundleHashs", "本地已存在");
        }
    }

    /// <summary>
    /// 获取远端Package列表
    /// </summary>
    /// <returns></returns>
    IEnumerator GetRemotePackages()
    {
        string remotePackagePath = Path.Combine(AssetManagerRuntime.instance.HTTPAddress, "BuildOutput", AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), "AllPackages");

        UnityWebRequest request = UnityWebRequest.Get(remotePackagePath);
        request.SendWebRequest();
        while (!request.isDone)
        {
            yield return null;
        }
        if (!string.IsNullOrEmpty(request.error))
        {
            Debug.Log(request.error);
            yield break;
        }

        string allPackages = request.downloadHandler.text;

        string downloadPackagePath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), "AllPackages");
        //将远端服务器的Package列表保存到本地下载路径,以便后续使用
        File.WriteAllText(downloadPackagePath, allPackages);
        Debug.Log($"Package下载完成{downloadPackagePath}");


        List<string> packageList = JsonConvert.DeserializeObject<List<string>>(allPackages);

        foreach (string packageName in packageList)
        {
            remotePackagePath = Path.Combine(AssetManagerRuntime.instance.HTTPAddress, "BuildOutput", AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), packageName);
            request = UnityWebRequest.Get(remotePackagePath);
            request.SendWebRequest();
            while (!request.isDone)
            {
                yield return null;
            }
            if (!string.IsNullOrEmpty(request.error))
            {
                Debug.Log(request.error);
                yield break;
            }
            string packageString = request.downloadHandler.text;
            downloadPackagePath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), packageName);
            //将远端服务器的Package列表保存到本地下载路径,以便后续使用
            File.WriteAllText(downloadPackagePath, packageString);
            Debug.Log($"Package下载完成{downloadPackagePath}");
        }
        StartCoroutine(GetRemoteAssetBundleHashs());
    }

    IEnumerator GetRemoteAssetBundleHashs()
    {
        string remoteAssetBundleHashPath = Path.Combine(AssetManagerRuntime.instance.HTTPAddress, "BuildOutput", AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), "AssetBundleHashs");

        UnityWebRequest request = UnityWebRequest.Get(remoteAssetBundleHashPath);
        request.SendWebRequest();
        while (!request.isDone)
        {
            yield return null;
        }
        if (!string.IsNullOrEmpty(request.error))
        {
            Debug.Log(request.error);
            yield break;
        }
        string hashString = request.downloadHandler.text;
        string hashPath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString(), "AssetBundleHashs");

        File.WriteAllText(hashPath, hashString);
        Debug.Log($"AssetBundleHashs下载完成{hashPath}");
        //CreateDownloadList();
    }

    BuildInfo RemoteBuildInfo;
    IEnumerator GetRemoteBuildVersion()
    {
        string remoteFileVersionPath = Path.Combine(AssetManagerRuntime.instance.HTTPAddress, "BuildOutput", "BuildVersion.version");

        UnityWebRequest request = UnityWebRequest.Get(remoteFileVersionPath);
        request.timeout = 5;
        request.SendWebRequest();

        while (!request.isDone)
        {
            yield return null;
        }
        if (!string.IsNullOrEmpty(request.error))
        {
            Debug.Log(request.error);
            yield break;
        }
        int version = int.Parse(request.downloadHandler.text);
        Debug.Log($"远端版本为{version}");
        if (AssetManagerRuntime.instance.LocalAssetVersion == version)
        {
            Debug.Log("版本一致无需更新");
            LoadAssetTest();
            yield break;
        }
        else
        {
            AssetManagerRuntime.instance.RemoteAssetVersion = version;
            FileURL = Path.Combine(AssetManagerRuntime.instance.HTTPAddress, "BuildOutput", AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString());
            DownloadVersionPath = Path.Combine(AssetManagerRuntime.instance.DownloadPath, AssetManagerRuntime.instance.RemoteAssetVersion.ToSafeString());
            if (!Directory.Exists(DownloadVersionPath))
            {
                Directory.CreateDirectory(DownloadVersionPath);
            }

            string remoteBuildInfoPath = Path.Combine(FileURL, "BuildInfo");
            request = UnityWebRequest.Get(remoteBuildInfoPath);
            request.timeout = 5;
            request.SendWebRequest();

            while (!request.isDone)
            {
                yield return null;
            }
            if (!string.IsNullOrEmpty(request.error))
            {
                Debug.Log(request.error);
                yield break;
            }

            string remoteBuildInfoString = request.downloadHandler.text;
            RemoteBuildInfo = JsonConvert.DeserializeObject<BuildInfo>(remoteBuildInfoString);
            Debug.Log($"本次更新最大大小{RemoteBuildInfo.FileTotalSize}");
            CreateDownloadList();
        }
    }
    IEnumerator DownloadFile(string fileName, bool isSaveFile, Action callBack)
    {
        string remotePath = Path.Combine(AssetManagerRuntime.instance.HTTPAddress);

        string mainBundlePath = Path.Combine(remotePath, fileName);

        UnityWebRequest webRequest = UnityWebRequest.Get(mainBundlePath);

        webRequest.SendWebRequest();

        while (!webRequest.isDone)
        {
            Debug.Log(webRequest.downloadedBytes);
            Debug.Log(webRequest.downloadProgress);
            yield return null;
        }
        Debug.Log(webRequest.downloadHandler.data.Length);
        if (!Directory.Exists(AssetBundleLoadPath))
        {
            Directory.CreateDirectory(AssetBundleLoadPath);
        }

        if (isSaveFile)
        {
            string assetBundleDownloadPath = Path.Combine(AssetBundleLoadPath, fileName);

            StartCoroutine(SaveFile(assetBundleDownloadPath, webRequest.downloadHandler.data, callBack));
        }
    }

    IEnumerator SaveFile(string filePath, byte[] bytes, Action callBack)
    {
        FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate); ;
        yield return fileStream.WriteAsync(bytes, 0, bytes.Length);
        //刷新,关闭,释放 文件流对象
        fileStream.Flush();
        fileStream.Close();
        fileStream.Dispose();

        Debug.Log($"{filePath}保存完毕");

        callBack?.Invoke();
        yield return null;
    }


    void LoadAssetBundle()
    {
        string assetBundlePath = Path.Combine(AssetBundleLoadPath);
        //加载主包
        AssetBundle mainAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        AssetBundleManifest manifest = mainAssetBundle.LoadAsset<AssetBundleManifest>(nameof(AssetBundleManifest));


        //遍历所有直接或间接依赖包
        foreach (string depName in manifest.GetAllDependencies("0"))
        {
            Debug.Log(depName);
            assetBundlePath = Path.Combine(AssetBundleLoadPath, depName);
            //加载依赖包
            AssetBundle.LoadFromFile(assetBundlePath);
        }
        //加载资源AB包
        assetBundlePath = Path.Combine(AssetBundleLoadPath, "0");
        CubeAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        assetBundlePath = Path.Combine(AssetBundleLoadPath, "1");
        SphereAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
    }




    void UnloadAssetBundle(bool isTrue)
    {
        Debug.Log(isTrue);
        Destroy(SampleObject);
        //SampleAssetBundle.Unload(isTrue);

        //这会导致报错
        //Resources.UnloadAsset(SampleObject);

        Resources.UnloadUnusedAssets();
    }

    // Update is called once per frame
    void Update()
    {

    }
}

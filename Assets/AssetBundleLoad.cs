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
        Debug.LogFormat("���ؽ���{0:0.00}%,{1}M/{2}M", (prg * 100), currLength * 1.0f / 1024 / 1024,
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
        //�ӱ��ذ��б��м���һ����
        AssetPackage package = AssetManagerRuntime.instance.LoadPackage("A");

        Debug.Log(package.PackageName);

        GameObject obj = package.LoadAsset<GameObject>("Assets/Scenes/Sphere.prefab");
        Instantiate(obj);
    }


    IEnumerator DownloadAssetBundle(List<string> fileNames, Action callBack = null)
    {
        foreach (string fileName in fileNames)
        {
            //��ΪHash�б��е��ļ��������ļ���С���ļ�������,���»���_���л���
            //����Ҫ���»��ߺ�һλ��ʼ��ȡAssetBundle�ľ�������
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
            Debug.Log($"AssetBundle�������{fileName}");
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
            OnCompleted("AllPackages", "�����Ѵ���");
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
    /// �ԱȲ�ͬ�汾�Ĺ�ϣ��
    /// �ڷ������˺Ϳͻ�����ʹ��ʱ���Ի����Ҫ���ص�AssetBundle�б�
    /// </summary>
    /// <param name="oldHashTable"></param>
    /// <param name="newHashTable"></param>
    /// <returns></returns>
    public AssetBundleVersionDiffrence ContrastAssetBundleHashTable(string[] oldHashTable, string[] newHashTable)
    {
        AssetBundleVersionDiffrence diffrence = new AssetBundleVersionDiffrence();
        diffrence.AdditionAssetBundles = new List<string>();
        diffrence.ReducedAssetBundles = new List<string>();
        //����ϵ�Hash�б���,����Hash�б������İ�,˵������Ҫ�Ƴ��İ�
        foreach (string assetHash in oldHashTable)
        {
            if (!newHashTable.Contains(assetHash))
            {
                diffrence.ReducedAssetBundles.Add(assetHash);
            }
        }

        //����µ�Hash����,���ϵ�Hash�������İ�,˵���������İ�
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
        //���ر��ȡ·��
        string assetBundleHashsLoadPath = Path.Combine(AssetManagerRuntime.instance.AssetBundleLoadPath, "AssetBundleHashs");

        string assetBundleHashsString = "";
        string[] localAssetBundleHashs = null;
        if (File.Exists(assetBundleHashsLoadPath))
        {
            assetBundleHashsString = File.ReadAllText(assetBundleHashsLoadPath);
            localAssetBundleHashs = JsonConvert.DeserializeObject<string[]>(assetBundleHashsString);
        }

        //Զ�˱��ȡ·��
        assetBundleHashsLoadPath = Path.Combine(DownloadVersionPath, "AssetBundleHashs");
        string[] remoteAssetBundleHashs = null;
        if (File.Exists(assetBundleHashsLoadPath))
        {
            assetBundleHashsString = File.ReadAllText(assetBundleHashsLoadPath);
            remoteAssetBundleHashs = JsonConvert.DeserializeObject<string[]>(assetBundleHashsString);
        }

        if (remoteAssetBundleHashs == null)
        {
            Debug.LogError("Զ�˱��ȡʧ��,��鿴�ļ��Ƿ����");
            return;
        }

        List<string> assetBundleNames = null;
        if (localAssetBundleHashs == null)
        {
            Debug.LogWarning("���ر��ȡʧ��,ֱ������Զ����Դ");
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
            //��ΪHash�б��е��ļ��������ļ���С���ļ�������,���»���_���л���
            //����Ҫ���»��ߺ�һλ��ʼ��ȡAssetBundle�ľ�������
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
                OnCompleted(fileName, "�����Ѵ���");
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
                OnCompleted(packageName, "�����Ѵ���");
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
            OnCompleted("AssetBundleHashs", "�����Ѵ���");
        }
    }

    /// <summary>
    /// ��ȡԶ��Package�б�
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
        //��Զ�˷�������Package�б��浽��������·��,�Ա����ʹ��
        File.WriteAllText(downloadPackagePath, allPackages);
        Debug.Log($"Package�������{downloadPackagePath}");


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
            //��Զ�˷�������Package�б��浽��������·��,�Ա����ʹ��
            File.WriteAllText(downloadPackagePath, packageString);
            Debug.Log($"Package�������{downloadPackagePath}");
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
        Debug.Log($"AssetBundleHashs�������{hashPath}");
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
        Debug.Log($"Զ�˰汾Ϊ{version}");
        if (AssetManagerRuntime.instance.LocalAssetVersion == version)
        {
            Debug.Log("�汾һ���������");
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
            Debug.Log($"���θ�������С{RemoteBuildInfo.FileTotalSize}");
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
        //ˢ��,�ر�,�ͷ� �ļ�������
        fileStream.Flush();
        fileStream.Close();
        fileStream.Dispose();

        Debug.Log($"{filePath}�������");

        callBack?.Invoke();
        yield return null;
    }


    void LoadAssetBundle()
    {
        string assetBundlePath = Path.Combine(AssetBundleLoadPath);
        //��������
        AssetBundle mainAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        AssetBundleManifest manifest = mainAssetBundle.LoadAsset<AssetBundleManifest>(nameof(AssetBundleManifest));


        //��������ֱ�ӻ���������
        foreach (string depName in manifest.GetAllDependencies("0"))
        {
            Debug.Log(depName);
            assetBundlePath = Path.Combine(AssetBundleLoadPath, depName);
            //����������
            AssetBundle.LoadFromFile(assetBundlePath);
        }
        //������ԴAB��
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

        //��ᵼ�±���
        //Resources.UnloadAsset(SampleObject);

        Resources.UnloadUnusedAssets();
    }

    // Update is called once per frame
    void Update()
    {

    }
}

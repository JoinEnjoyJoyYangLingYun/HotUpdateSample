using Codice.Client.Common.TreeGrouper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditorInternal.VR;
using UnityEngine;


public class AssetBundleEdge
{
    /// <summary>
    /// ���������ڱ��������ϵ��Node
    /// һ��Node�������ö��Node
    /// ���NodeҲ��������һ��Node
    /// </summary>
    public List<AssetBundleNode> Nodes = new List<AssetBundleNode>();
}
public class AssetBundleNode
{
    /// <summary>
    /// ÿ��Node��Ӧ��ֻ��һ��Asset
    /// �˴���string���͸ĳ�GUIDҲ��һ����
    /// </summary>
    public string assetName;

    /// <summary>
    /// ����SourceAsset�����,�������SourceAsset��ΪĬ��ֵ
    /// </summary>
    public int SourceIndex = -1;

    /// <summary>
    /// �������ø���Դ��SourceAsset���������
    /// </summary>
    public List<int> SourceIndices;

    /// <summary>
    /// ���ڱ�ﵱǰ��Դ�����ĸ�Package
    /// �����ж��SourceAssetӵ��ͬһ��PackageName
    /// </summary>
    public string PackageName;

    /// <summary>
    /// ���ڱ�ﵱǰ��Դ���ĸ�Package������
    /// </summary>
    public List<string> PackageNames = new List<string>();

    /// <summary>
    /// ��ǰNode�����õ�Nodes
    /// </summary>
    public AssetBundleEdge OutEdge;
    /// <summary>
    /// ���õ�ǰNode��Nodes
    /// </summary>
    public AssetBundleEdge InEdge;


}


public class AssetManagerEditor
{


    public static AssetManagerConfigScirptableObject AssetManagerConfig;





    /// <summary>
    /// ����������ݵ����·��
    /// </summary>
    public static string BuildOutputPath;

    /// <summary>
    /// ����ĳ���汾��AssetBundle�����·��
    /// </summary>
    public static string AssetBundleOutputPath;


    public static void SaveConfigToJSON()
    {

        if (AssetManagerConfig != null)
        {
            string configString = JsonUtility.ToJson(AssetManagerConfig);

            string outputPath = Path.Combine(Application.dataPath, "Editor/AssetManagerConfig.json");
            File.WriteAllText(outputPath, configString);

            AssetDatabase.Refresh();
        }
    }

    public static void ReadConfigFromJSON()
    {
        string configPath = Path.Combine(Application.dataPath, "Editor/AssetManagerConfig.json");

        string configString = File.ReadAllText(configPath);

        JsonUtility.FromJsonOverwrite(configString, AssetManagerConfig);

    }

    public static void LoadAssetManagerConfig(AssetManagerEditorWindow window)
    {
        if (AssetManagerConfig == null)
        {
            AssetManagerConfig = AssetDatabase.LoadAssetAtPath<AssetManagerConfigScirptableObject>("Assets/Editor/AssetManagerConfig.asset");
            window.VersionString = AssetManagerConfig.AssetBundleToolVersion.ToString();
            for (int i = window.VersionString.Length - 1; i >= 1; i--)
            {
                window.VersionString = window.VersionString.Insert(i, ".");
            }
        }
    }



    public static void LoadAssetManagerWindowConfig(AssetManagerEditorWindow window)
    {
        if (window.WindowConfig == null)
        {
            window.WindowConfig = AssetDatabase.LoadAssetAtPath<AssetManagerEditorWindowConfig>("Assets/Editor/AssetManagerWindowConfig.asset");
        }
    }

    [MenuItem("AssetManager/CreateConfigFile")]
    public static void CreateNewConfigScriptableObject()
    {
        AssetManagerConfigScirptableObject assetManagerConfig = ScriptableObject.CreateInstance<AssetManagerConfigScirptableObject>();

        AssetDatabase.CreateAsset(assetManagerConfig, "Assets/AssetManagerConfig.asset");

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
    }

    public static void GetFolderAllAssets()
    {
        //if (AssetManagerConfig.AssetBundleDirectory == null)
        //{
        //    return;
        //}
        //string directoryPath = AssetDatabase.GetAssetPath(AssetManagerConfig.AssetBundleDirectory);
        //AssetManagerConfig.CurrentAllAssets = FindAllAssetPathFromFolder(directoryPath);
        //AssetManagerConfig.CurrentSelectedAssets = new bool[AssetManagerConfig.CurrentAllAssets.Count];
    }

    const string AssetBundleOutputFolderName="BuildOutput";
    public static void CheckOutputPath()
    {
        switch (AssetManagerConfig.AssetBundleOutputPattern)
        {
            case AssetBundlePattern.EditorSimulation:
                //OutputPath = Path.Combine(Application.persistentDataPath, "AssetBundles");
                break;
            case AssetBundlePattern.Local:
                BuildOutputPath = Path.Combine(Application.streamingAssetsPath, AssetBundleOutputFolderName);
                break;
            case AssetBundlePattern.Remote:
                BuildOutputPath = Path.Combine(Application.persistentDataPath, AssetBundleOutputFolderName);
                break;
        }
        if (!Directory.Exists(BuildOutputPath))
        {
            Directory.CreateDirectory(BuildOutputPath);
        }

        AssetBundleOutputPath = Path.Combine(BuildOutputPath, AssetManagerConfig.CurrentBuildVersion.ToString());
        if (!Directory.Exists(AssetBundleOutputPath))
        {
            Directory.CreateDirectory(AssetBundleOutputPath);
        }
        Debug.Log(BuildOutputPath);
    }

    /// <summary>
    /// �ԱȲ�ͬ�汾�Ĺ�ϣ��
    /// �ڷ������˺Ϳͻ�����ʹ��ʱ���Ի����Ҫ���ص�AssetBundle�б�
    /// </summary>
    /// <param name="oldHashTable"></param>
    /// <param name="newHashTable"></param>
    /// <returns></returns>
    public static AssetBundleVersionDiffrence ContrastAssetBundleHashTable(string[] oldHashTable, string[] newHashTable)
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

    static string ComputeAssetBundleSizeToMD5(string assetBundlePath)
    {

        MD5 md5 = MD5.Create();
        FileInfo fileInfo = new FileInfo(assetBundlePath);
        byte[] buffer = Encoding.ASCII.GetBytes(fileInfo.Length.ToString());
        md5.TransformBlock(buffer, 0, buffer.Length, null, 0);
        md5.TransformFinalBlock(new byte[0], 0, 0);
        return BytesToHexString(md5.Hash);
    }

    static string ComputeAssetSetSignature(IEnumerable<string> assetNames)
    {
        var assetGuids = assetNames.Select(AssetDatabase.AssetPathToGUID);

        MD5 md5 = MD5.Create();

        foreach (string assetGuid in assetGuids.OrderBy(x => x))
        {
            byte[] buffer = Encoding.ASCII.GetBytes(assetGuid);

            md5.TransformBlock(buffer, 0, buffer.Length, null, 0);
        }

        md5.TransformFinalBlock(new byte[0], 0, 0);

        return BytesToHexString(md5.Hash);
    }

    static string BytesToHexString(byte[] bytes)
    {
        StringBuilder byteString = new StringBuilder();
        foreach (byte aByte in bytes)
        {
            byteString.Append(aByte.ToString("x2"));
        }
        return byteString.ToString();
    }

    [MenuItem("AssetManager/BuildAssetBundle")]
    // Start is called before the first frame update
    public static void BuildAssetBundle()
    {
        CheckOutputPath();
        BuildPipeline.BuildAssetBundles(BuildOutputPath,
                                        BuildAssetBundleOptions.None,
                                        BuildTarget.StandaloneWindows);
    }

    public static List<string> GetSelectAssetNames()
    {
        List<string> selectedAssetNames = new List<string>();

        foreach (var info in AssetManagerConfig.EditorPackageInfos)
        {
            foreach (var asset in info.Assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);

                selectedAssetNames.Add(assetPath);
            }
        }
        return selectedAssetNames;
    }

    public static void GetAssetsFromPath()
    {
        //����ѡ���Asset,����ΪSourceAsset
        List<string> selectedAssetNames = GetSelectAssetNames();

        string[] assetsDeps = AssetDatabase.GetDependencies(selectedAssetNames.ToArray(), true);
        foreach (string assetName in assetsDeps)
        {
            Debug.Log(assetName);
        }
    }
    public static void BuildAssetBundleFromDirectedGraph()
    {
        AssetManagerConfig.CurrentBuildVersion++;
        CheckOutputPath();

        List<AssetBundleNode> allNodes = new List<AssetBundleNode>();

        int sourceIndex = 0;

        Dictionary<string, PackageInfo> packageInfoDic = new Dictionary<string, PackageInfo>();

        for (int pakcageInfoIndex = 0; pakcageInfoIndex < AssetManagerConfig.EditorPackageInfos.Count; pakcageInfoIndex++)
        {
            PackageInfo info = new PackageInfo();

            PackageInfoEditor editorInfo = AssetManagerConfig.EditorPackageInfos[pakcageInfoIndex];

            info.IsSourcePackage = true;
            info.PackageName = editorInfo.PackageName;

            packageInfoDic.Add(editorInfo.PackageName, info);

            foreach (var asset in AssetManagerConfig.EditorPackageInfos[pakcageInfoIndex].Assets)
            {
                AssetBundleNode currentNode = null;
                string assetName= AssetDatabase.GetAssetPath(asset);
                //���һ��SourceAsset��֮ǰ�Ѿ�������DerivedAsset��ӹ�
                //��ôֱ��ʹ����ͬ��Node
                foreach (AssetBundleNode node in allNodes)
                {
                    if (node.assetName == assetName)
                    {
                        currentNode = node;
                        currentNode.PackageName = info.PackageName;
                        break;
                    }
                }
                //���򴴽��µ�Node
                if (currentNode == null)
                {
                    currentNode = new AssetBundleNode();

                    currentNode.PackageName = info.PackageName;
                    currentNode.PackageNames.Add(currentNode.PackageName);

                    currentNode.SourceIndex = sourceIndex;
                    currentNode.SourceIndices = new List<int>() { currentNode.SourceIndex };
                    currentNode.InEdge = new AssetBundleEdge();
                    currentNode.assetName = assetName;

                    allNodes.Add(currentNode);
                }

                GetNodesFromDependencies(currentNode, allNodes);
                sourceIndex++;
            }
        }



        Dictionary<List<int>, List<AssetBundleNode>> assetbundleNodeDic = new Dictionary<List<int>, List<AssetBundleNode>>();
        foreach (var node in allNodes)
        {
            StringBuilder packageNameString = new StringBuilder();

            //�������Ϊ�գ�������һ��DerivedAsset
            //��ô�ͱ��������˸�DerivedAsset��Package�������䵽һ���µİ���
            if (string.IsNullOrEmpty(node.PackageName))
            {
                //������������õ���Դ�����䵽һ���µİ���
                for (int i = 0; i < node.PackageNames.Count; i++)
                {
                    packageNameString.Append(node.PackageNames[i]);
                    if (i < node.PackageNames.Count - 1)
                    {
                        packageNameString.Append("_");
                    }
                }
                string packageName = packageNameString.ToString();
                node.PackageName = packageName;

                if (!packageInfoDic.Keys.Contains(packageName))
                {
                    PackageInfo dependInfo = new PackageInfo();
                    dependInfo.PackageName = packageName;
                    dependInfo.IsSourcePackage = false;
                    packageInfoDic.Add(dependInfo.PackageName, dependInfo);
                }
            }

            bool isSourceIndexEquals = false;
            List<int> keyList = new List<int>();
            foreach (List<int> key in assetbundleNodeDic.Keys)
            {
                isSourceIndexEquals = node.SourceIndices.Count == key.Count && node.SourceIndices.All(p => key.Any(k => k.Equals(p)));

                if (isSourceIndexEquals)
                {
                    keyList = key;
                }
            }
            if (!isSourceIndexEquals)
            {
                keyList = node.SourceIndices;
                assetbundleNodeDic.Add(keyList, new List<AssetBundleNode>());
            }
            assetbundleNodeDic[keyList].Add(node);
        }


        AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[assetbundleNodeDic.Count];

        int buildIndex = 0;
        foreach (var key in assetbundleNodeDic.Keys)
        {
            List<string> assetNames = new List<string>();

            foreach (AssetBundleNode node in assetbundleNodeDic[key])
            {
                assetNames.Add(node.assetName);

                //��Ϊ��ѭ����������е�Node
                //�������һ��node��PackageNames��Ψһ�����Ҳ���һ��SourcePackage����ΪPackageNames�еĶ�ӦPackage��ӶԸ�Package������
                foreach (string dependPackageName in node.PackageNames)
                {
                    if (packageInfoDic.Keys.Contains(dependPackageName))
                    {
                        if (!packageInfoDic[dependPackageName].PackageDependencies.Contains(node.PackageName) && !string.Equals(node.PackageName, packageInfoDic[dependPackageName].PackageName))
                        {
                            packageInfoDic[dependPackageName].PackageDependencies.Add(node.PackageName);
                        }
                    }
                }
            }

            assetBundleBuilds[buildIndex].assetBundleName = ComputeAssetSetSignature(assetNames);


            assetBundleBuilds[buildIndex].assetNames = assetNames.ToArray();

            foreach (AssetBundleNode node in assetbundleNodeDic[key])
            {
                AssetInfo info = new AssetInfo();
                info.AssetName = node.assetName;
                info.AssetBundleName = assetBundleBuilds[buildIndex].assetBundleName;
                packageInfoDic[node.PackageName].assetInfos.Add(info);
            }

            buildIndex++;
        }

        BuildPipeline.BuildAssetBundles(BuildOutputPath, assetBundleBuilds, CheckIncrementalMode(),
                    BuildTarget.StandaloneWindows);

        string versionFilePath = Path.Combine(BuildOutputPath, "BuildVersion.version");
        File.WriteAllText(versionFilePath, AssetManagerConfig.CurrentBuildVersion.ToString());

        BuildAssetBundleHashTable(assetBundleBuilds);

        BuildPackageTable(packageInfoDic);

        CopyAssetBundleToVersionFolder();

        CreateBuildInfo();

        AssetDatabase.Refresh();
    }

    static void CreateBuildInfo()
    {
        BuildInfo currentBuildInfo = new BuildInfo();
        currentBuildInfo.BuildVersion = AssetManagerConfig.CurrentBuildVersion;
        
        DirectoryInfo directoryInfo = new DirectoryInfo(AssetBundleOutputPath);
        FileInfo[] fileInfos = directoryInfo.GetFiles();
        foreach (FileInfo fileInfo in fileInfos)
        {
            currentBuildInfo.FileNames.Add(fileInfo.Name, (ulong)fileInfo.Length);
            currentBuildInfo.FileTotalSize += (ulong)fileInfo.Length;
        }

        string buildInfoSavePath = Path.Combine(AssetBundleOutputPath, "BuildInfo");
        string buildInfoString = JsonConvert.SerializeObject(currentBuildInfo);

        File.WriteAllText(buildInfoSavePath, buildInfoString);
    }
    static void BuildPackageTable(Dictionary<string, PackageInfo> packages)
    {
        string allPackageListName = "AllPackages";
        string allPackageListPath= Path.Combine(AssetBundleOutputPath, allPackageListName);

        string AllPackagesJSON = JsonConvert.SerializeObject(packages.Keys);

        File.WriteAllText(allPackageListPath, AllPackagesJSON);

        foreach(PackageInfo package in packages.Values)
        {
            string packageListPath = Path.Combine(AssetBundleOutputPath, package.PackageName);

            string packagesJSON = JsonConvert.SerializeObject(package);

            File.WriteAllText(packageListPath, packagesJSON);
        }
    }

    static void CopyAssetBundleToVersionFolder()
    {

        string[] assetNames = ReadAssetBundleHashTable(AssetBundleOutputPath);

        //��������
        string mainBundleOriginPath = Path.Combine(BuildOutputPath, "BuildOutput");
        string mainBundleVersionPath = Path.Combine(AssetBundleOutputPath, "BuildOutput");
        File.Copy(mainBundleOriginPath, mainBundleVersionPath,true);

        foreach (var assetName in assetNames)
        {
            string assetHashName = assetName.Substring(assetName.IndexOf("_") + 1);

            string assetOriginPath = Path.Combine(BuildOutputPath, assetHashName);

            string assetVersionPath = Path.Combine(AssetBundleOutputPath, assetHashName);
            File.Copy(assetOriginPath, assetVersionPath,true);
        }

        switch (AssetManagerConfig.AssetBundleOutputPattern)
        {
            case AssetBundlePattern.Local:
                DirectoryInfo directoryInfo = new DirectoryInfo(AssetBundleOutputPath);
                FileInfo[] fileInfos = directoryInfo.GetFiles();

                string localAssetVersionPath = Path.Combine(Application.streamingAssetsPath, "LocalAssets", AssetManagerConfig.CurrentBuildVersion.ToString());

                if (!Directory.Exists(localAssetVersionPath))
                {
                    Directory.CreateDirectory(localAssetVersionPath);
                }

                string versionFilePath = Path.Combine(BuildOutputPath, "BuildVersion.version");
                string versionFileLocalPath = Path.Combine(Application.streamingAssetsPath, "LocalAssets", "LocalVersion.version");
                File.Copy(versionFilePath, versionFileLocalPath, true);
                foreach (FileInfo file in fileInfos)
                {
                    string localAssetPath = Path.Combine(localAssetVersionPath, file.Name);
                    file.CopyTo(localAssetPath, true);
                }
                break;
        }
    }

    static BuildAssetBundleOptions CheckIncrementalMode()
    {
        BuildAssetBundleOptions option = BuildAssetBundleOptions.None;

        switch (AssetManagerConfig.InCrementalMode)
        {
            case IncrementalBuildMode.None:
                option = BuildAssetBundleOptions.None;
                break;
            case IncrementalBuildMode.UseIncrementalBuild:
                option = BuildAssetBundleOptions.DeterministicAssetBundle;
                break;
            case IncrementalBuildMode.ForcusRebuild:
                option = BuildAssetBundleOptions.ForceRebuildAssetBundle;
                break;
        }
        return option;
    }

    static string[] ReadAssetBundleHashTable(string assetBundlePath)
    {
        string HashTablePath = Path.Combine(assetBundlePath, "AssetBundleHashs");
        string HashTableString = File.ReadAllText(HashTablePath);
        string[] HashTable = JsonConvert.DeserializeObject<string[]>(HashTableString);

        return HashTable;
    }

    static AssetBundleBuild[] GetVersionDiffrence(AssetBundleBuild[] currentBuilds)
    {
        string[] currentHashTable = BuildAssetBundleHashTable(currentBuilds);

        AssetManagerConfig.CurrentBuildVersion--;
        CheckOutputPath();

        string[] lasetVerionHashTable = ReadAssetBundleHashTable(BuildOutputPath);

        AssetBundleVersionDiffrence diffrence = ContrastAssetBundleHashTable(lasetVerionHashTable, currentHashTable);


        if (diffrence.AdditionAssetBundles.Count > 0)
        {
            foreach (var additionAssetBundle in diffrence.AdditionAssetBundles)
            {
                Debug.Log(additionAssetBundle);
            }
        }
        if (diffrence.ReducedAssetBundles.Count > 0)
        {
            foreach (var reducedAssetBundle in diffrence.ReducedAssetBundles)
            {
                Debug.Log(reducedAssetBundle);
            }
        }

        return currentBuilds;
    }

    public static string[] BuildAssetBundleHashTable(AssetBundleBuild[] assetBundleBuilds)
    {

        string[] assetBundleHashs = new string[assetBundleBuilds.Length];
        for (int i = 0; i < assetBundleBuilds.Length; i++)
        {
            string assetBundlePath = Path.Combine(BuildOutputPath, assetBundleBuilds[i].assetBundleName);
            FileInfo fileinfo = new FileInfo(assetBundlePath);
            assetBundleHashs[i] = $"{fileinfo.Length}_{assetBundleBuilds[i].assetBundleName}";
        }
        string hashString = JsonConvert.SerializeObject(assetBundleHashs);
        string hashFilePath = Path.Combine(AssetBundleOutputPath, "AssetBundleHashs");
        File.WriteAllText(hashFilePath, hashString);
        return assetBundleHashs;
    }


    /// <summary>
    /// Ƕ�׺���,����ʹ����������Node
    /// </summary>
    /// <param name="lastNode"></param>�ϲ�Node,���ӦΪSourceAsset
    /// <param name="currentAllNodes"></param>��ǰ���е�Node
    public static void GetNodesFromDependencies(AssetBundleNode lastNode, List<AssetBundleNode> currentAllNodes)
    {
        string[] assetNames = AssetDatabase.GetDependencies(lastNode.assetName, false);
        if (assetNames.Length > 0)
        {
            lastNode.OutEdge = new AssetBundleEdge();
        }
        foreach (string assetName in assetNames)
        {
            //Ϊ�˱���������м��ص�����Ҫ����Դ,����������Ҫ����Դ�����ƽ����ж�
            if (!IsValidExtentionName(assetName))
            {
                continue;
            }
            AssetBundleNode currentNode = null;
            foreach (AssetBundleNode existingNode in currentAllNodes)
            {
                if (existingNode.assetName == assetName)
                {
                    currentNode = existingNode;
                    break;
                }
            }
            if (currentNode == null)
            {
                currentNode = new AssetBundleNode();
                currentNode.assetName = assetName;
                currentNode.InEdge = new AssetBundleEdge();
                currentNode.SourceIndices = new List<int>();
                currentAllNodes.Add(currentNode);
            }

            currentNode.InEdge.Nodes.Add(lastNode);
            lastNode.OutEdge.Nodes.Add(currentNode);

            //����Ҳ��������ͼ���д���
            if (!string.IsNullOrEmpty(lastNode.PackageName))
            {
                if (!currentNode.PackageNames.Contains(lastNode.PackageName))
                {
                    currentNode.PackageNames.Add(lastNode.PackageName);
                }
            }
            else
            {
                foreach (string packageName in lastNode.PackageNames)
                {
                    if (!currentNode.PackageNames.Contains(packageName))
                    {
                        currentNode.PackageNames.Add(packageName);
                    }
                }
            }

            if (lastNode.SourceIndex >= 0)
            {
                if (!currentNode.SourceIndices.Contains(lastNode.SourceIndex))
                {
                    currentNode.SourceIndices.Add(lastNode.SourceIndex);
                }
            }
            else
            {
                foreach (int index in lastNode.SourceIndices)
                {
                    if (!currentNode.SourceIndices.Contains(index))
                    {
                        currentNode.SourceIndices.Add(index);
                    }
                }
            }
            lastNode.OutEdge.Nodes.Add(currentNode);


            //�������»�ȡ����������Node
            GetNodesFromDependencies(currentNode, currentAllNodes);
        }
    }

    public static void BuildAssetBundleFromSets()
    {
        CheckOutputPath();
        //����ͨ��Editorѡ���Asset,����ΪSourceAsset
        List<string> selectedAssetNames = GetSelectAssetNames();

        //����SourceAsset��������,����ΪDerivedAsset
        //��DerivedAsset�ļ��������ɵ��б�,Ҳ���ǲ���2�еļ����б�L
        List<List<GUID>> selectedAssetDependencies = new List<List<GUID>>();

        //����ÿ��SourceAsset������
        foreach (string selectedAssetName in selectedAssetNames)
        {
            string[] assetDeps = AssetDatabase.GetDependencies(selectedAssetName, true);

            List<GUID> assetGUIDs = new List<GUID>();

            foreach (string assetPath in assetDeps)
            {
                if (assetPath.Contains(".cs"))
                {
                    continue;
                }
                GUID assetGUID = AssetDatabase.GUIDFromAssetPath(assetPath);
                assetGUIDs.Add(assetGUID);
                Debug.Log(assetGUID);
            }

            selectedAssetDependencies.Add(assetGUIDs);
        }

        for (int i = 0; i < selectedAssetDependencies.Count; i++)
        {
            int nextCount = i + 1;
            if (nextCount >= selectedAssetDependencies.Count)
            {
                break;
            }
            for (int j = 0; j <= i; j++)
            {
                List<GUID> newDerivedAssets = ContrastDependenceFromGUID(selectedAssetDependencies[j], selectedAssetDependencies[nextCount]);
                if (newDerivedAssets.Count > 0)
                {
                    selectedAssetDependencies.Add(newDerivedAssets);
                }
            }
        }



        AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[selectedAssetDependencies.Count];

        for (int i = 0; i < assetBundleBuilds.Length; i++)
        {
            assetBundleBuilds[i].assetBundleName = i.ToString();
            string[] assetNames = new string[selectedAssetDependencies[i].Count];
            for (int j = 0; j < assetNames.Length; j++)
            {
                string assetName = AssetDatabase.GUIDToAssetPath(selectedAssetDependencies[i][j].ToString());
                assetNames[j] = assetName;
            }
            assetBundleBuilds[i].assetNames = assetNames;
        }

        BuildPipeline.BuildAssetBundles(BuildOutputPath, assetBundleBuilds, BuildAssetBundleOptions.None,
                    BuildTarget.StandaloneWindows);

        AssetDatabase.Refresh();
    }

    public static List<GUID> ContrastDependenceFromGUID(List<GUID> depsA, List<GUID> depsB)
    {
        List<GUID> newDerivedAssets = new List<GUID>();

        foreach (GUID assetGUID in depsA)
        {
            if (depsB.Contains(assetGUID))
            {
                newDerivedAssets.Add(assetGUID);
            }
        }

        foreach (GUID derivedAssetGUID in newDerivedAssets)
        {
            if (depsA.Contains(derivedAssetGUID))
            {
                depsA.Remove(derivedAssetGUID);
            }
            if (depsB.Contains(derivedAssetGUID))
            {
                depsB.Remove(derivedAssetGUID);
            }
        }
        Debug.Log(newDerivedAssets.Count);
        return newDerivedAssets;
    }

    public static void BuildAssetBundleFromEditorWindow()
    {
        CheckOutputPath();
        List<string> selectedAssetNames = GetSelectAssetNames();
        AssetBundleBuild[] builds = new AssetBundleBuild[selectedAssetNames.Count];

        for (int i = 0; i < builds.Length; i++)
        {
            //�����Asset�����һ��AssetBundle
            //assetBundleName��Ҫ����İ���
            //string directoryPath = AssetDatabase.GetAssetPath(AssetManagerEditor.AssetManagerConfig.AssetBundleDirectory);
            //string assetName = selectedAssetNames[i].Replace($"{directoryPath}/", string.Empty);
            //assetName = assetName.Replace(".prefab", string.Empty);
            //builds[i].assetBundleName = assetName;

            string[] assetNames = new string[] { selectedAssetNames[i] };
            //assetNameʵ����ָ������Դ��·��
            builds[i].assetNames = assetNames;

        }
        BuildPipeline.BuildAssetBundles(BuildOutputPath, builds, BuildAssetBundleOptions.None,
                            BuildTarget.StandaloneWindows);

        AssetDatabase.Refresh();
    }



    /// <summary>
    /// �����ťʱ,���һ���ն���
    /// </summary>
    public static void AddAssetBundleInfo()
    {
        AssetManagerConfig.EditorPackageInfos.Add(new PackageInfoEditor());
    }

    public static void RemoveAssetBundleInfo(PackageInfoEditor info)
    {
        if (AssetManagerConfig.EditorPackageInfos.Contains(info))
        {
            AssetManagerConfig.EditorPackageInfos.Remove(info);
        }
    }

    public static void AddAsset(PackageInfoEditor info)
    {
        info.Assets.Add(null);
    }

    public static void RemoveAsset(PackageInfoEditor info, UnityEngine.Object assetObject)
    {
        if (info.Assets.Contains(assetObject))
        {
            info.Assets.Remove(assetObject);
        }
    }


    public static void BuildAssetBundleFromFolder()
    {
        CheckOutputPath();
        GetFolderAllAssets();

        AssetBundleBuild[] builds = new AssetBundleBuild[1];

        //�����Asset�����һ��AssetBundle
        //assetBundleName��Ҫ����İ���
        //builds[0].assetBundleName = AssetBundleLoad.ObjectBundleName;
        //assetNameʵ����ָ������Դ��·��
        //builds[0].assetNames = AssetManagerConfig.CurrentAllAssets.ToArray();


        BuildPipeline.BuildAssetBundles(BuildOutputPath, builds, BuildAssetBundleOptions.None,
                            BuildTarget.StandaloneWindows);

        AssetDatabase.Refresh();

    }

    /// <summary>
    /// �ж��ļ�������չ���Ƿ���Ч
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    static bool IsValidExtentionName(string fileName)
    {
        bool isValid = true;
        foreach (string invalidName in AssetManagerConfig.InvalidExtentionName)
        {
            if (fileName.Contains(invalidName))
            {
                isValid = false;
                return isValid;
            }
        }
        return isValid;
    }

    static List<string> FindAllAssetPathFromFolder(string directoryPath)
    {
        List<string> objectPaths = new List<string>();
        //��λ��ָ���ļ���
        if (!Directory.Exists(directoryPath))
        {
            return objectPaths;
        }

        DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

        FileInfo[] fileInfos = directoryInfo.GetFiles();
        for (int i = 0; i < fileInfos.Length; i++)
        {
            var file = fileInfos[i];

            //����Unity��meta�ļ�����׺��Ϊ.meta��
            if (!IsValidExtentionName(file.Extension))
            {
                continue;
            }

            //����·��ֱ��ƴ����Ӧ���ļ������·��
            string path = $"{directoryPath}/{file.Name}";
            objectPaths.Add(path);
        }
        return objectPaths;
    }

    [MenuItem("AssetManager/AssetManagerWindow")]
    static void OpenAssetManagerWindow()
    {
        AssetManagerEditorWindow window = (AssetManagerEditorWindow)EditorWindow.GetWindow(typeof(AssetManagerEditorWindow));
        window.Show();
    }
}

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
    /// 可能是用于表达依赖关系的Node
    /// 一个Node可能引用多个Node
    /// 多个Node也可能引用一个Node
    /// </summary>
    public List<AssetBundleNode> Nodes = new List<AssetBundleNode>();
}
public class AssetBundleNode
{
    /// <summary>
    /// 每个Node中应该只有一个Asset
    /// 此处的string类型改成GUID也是一样的
    /// </summary>
    public string assetName;

    /// <summary>
    /// 代表SourceAsset的序号,如果不是SourceAsset则为默认值
    /// </summary>
    public int SourceIndex = -1;

    /// <summary>
    /// 代表引用该资源的SourceAsset数量和序号
    /// </summary>
    public List<int> SourceIndices;

    /// <summary>
    /// 用于表达当前资源属于哪个Package
    /// 可能有多个SourceAsset拥有同一个PackageName
    /// </summary>
    public string PackageName;

    /// <summary>
    /// 用于表达当前资源被哪个Package所引用
    /// </summary>
    public List<string> PackageNames = new List<string>();

    /// <summary>
    /// 当前Node所引用的Nodes
    /// </summary>
    public AssetBundleEdge OutEdge;
    /// <summary>
    /// 引用当前Node的Nodes
    /// </summary>
    public AssetBundleEdge InEdge;


}


public class AssetManagerEditor
{


    public static AssetManagerConfigScirptableObject AssetManagerConfig;





    /// <summary>
    /// 整个打包数据的输出路径
    /// </summary>
    public static string BuildOutputPath;

    /// <summary>
    /// 具体某个版本的AssetBundle的输出路径
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
    /// 对比不同版本的哈希表
    /// 在服务器端和客户端上使用时可以获得需要下载的AssetBundle列表
    /// </summary>
    /// <param name="oldHashTable"></param>
    /// <param name="newHashTable"></param>
    /// <returns></returns>
    public static AssetBundleVersionDiffrence ContrastAssetBundleHashTable(string[] oldHashTable, string[] newHashTable)
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
        //所有选择的Asset,都视为SourceAsset
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
                //如果一个SourceAsset在之前已经被当做DerivedAsset添加过
                //那么直接使用相同的Node
                foreach (AssetBundleNode node in allNodes)
                {
                    if (node.assetName == assetName)
                    {
                        currentNode = node;
                        currentNode.PackageName = info.PackageName;
                        break;
                    }
                }
                //否则创建新的Node
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

            //如果包名为空，代表是一个DerivedAsset
            //那么就遍历引用了该DerivedAsset的Package，并分配到一个新的包中
            if (string.IsNullOrEmpty(node.PackageName))
            {
                //将被多个包引用的资源，分配到一个新的包下
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

                //因为该循环会遍历所有的Node
                //所以如果一个node的PackageNames不唯一，并且不是一个SourcePackage，则为PackageNames中的对应Package添加对该Package的引用
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

        //复制主包
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
    /// 嵌套函数,用于使用依赖创建Node
    /// </summary>
    /// <param name="lastNode"></param>上层Node,最顶层应为SourceAsset
    /// <param name="currentAllNodes"></param>当前所有的Node
    public static void GetNodesFromDependencies(AssetBundleNode lastNode, List<AssetBundleNode> currentAllNodes)
    {
        string[] assetNames = AssetDatabase.GetDependencies(lastNode.assetName, false);
        if (assetNames.Length > 0)
        {
            lastNode.OutEdge = new AssetBundleEdge();
        }
        foreach (string assetName in assetNames)
        {
            //为了避免从依赖中加载到不必要的资源,所以首先需要对资源的名称进行判断
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

            //包名也跟着有向图进行传递
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


            //继续向下获取依赖并返回Node
            GetNodesFromDependencies(currentNode, currentAllNodes);
        }
    }

    public static void BuildAssetBundleFromSets()
    {
        CheckOutputPath();
        //所有通过Editor选择的Asset,都视为SourceAsset
        List<string> selectedAssetNames = GetSelectAssetNames();

        //所有SourceAsset所依赖的,都视为DerivedAsset
        //由DerivedAsset的集合所构成的列表,也就是步骤2中的集合列表L
        List<List<GUID>> selectedAssetDependencies = new List<List<GUID>>();

        //遍历每个SourceAsset的依赖
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
            //将多个Asset打包进一个AssetBundle
            //assetBundleName是要打包的包名
            //string directoryPath = AssetDatabase.GetAssetPath(AssetManagerEditor.AssetManagerConfig.AssetBundleDirectory);
            //string assetName = selectedAssetNames[i].Replace($"{directoryPath}/", string.Empty);
            //assetName = assetName.Replace(".prefab", string.Empty);
            //builds[i].assetBundleName = assetName;

            string[] assetNames = new string[] { selectedAssetNames[i] };
            //assetName实际上指的是资源的路径
            builds[i].assetNames = assetNames;

        }
        BuildPipeline.BuildAssetBundles(BuildOutputPath, builds, BuildAssetBundleOptions.None,
                            BuildTarget.StandaloneWindows);

        AssetDatabase.Refresh();
    }



    /// <summary>
    /// 点击按钮时,添加一个空对象
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

        //将多个Asset打包进一个AssetBundle
        //assetBundleName是要打包的包名
        //builds[0].assetBundleName = AssetBundleLoad.ObjectBundleName;
        //assetName实际上指的是资源的路径
        //builds[0].assetNames = AssetManagerConfig.CurrentAllAssets.ToArray();


        BuildPipeline.BuildAssetBundles(BuildOutputPath, builds, BuildAssetBundleOptions.None,
                            BuildTarget.StandaloneWindows);

        AssetDatabase.Refresh();

    }

    /// <summary>
    /// 判断文件名的拓展名是否有效
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
        //定位到指定文件夹
        if (!Directory.Exists(directoryPath))
        {
            return objectPaths;
        }

        DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

        FileInfo[] fileInfos = directoryInfo.GetFiles();
        for (int i = 0; i < fileInfos.Length; i++)
        {
            var file = fileInfos[i];

            //跳过Unity的meta文件（后缀名为.meta）
            if (!IsValidExtentionName(file.Extension))
            {
                continue;
            }

            //根据路径直接拼出对应的文件的相对路径
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

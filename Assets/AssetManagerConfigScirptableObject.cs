using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum AssetBundlePattern
{
    EditorSimulation,
    Local,
    Remote
}

public enum IncrementalBuildMode
{
    None,
    UseIncrementalBuild,
    ForcusRebuild
}
public enum AssetBundleBuildMode
{
    AllProject,
    Directory,
    SelectAssets,
    SelectAssetFromSets,
    SelectAssetFromDirectedGraph
}

public class AssetBundleVersionDiffrence
{
    public List<string> AdditionAssetBundles;
    public List<string> ReducedAssetBundles;
}

/// <summary>
/// AssetBundle信息
/// </summary>
[Serializable]
public class PackageInfoEditor
{
    /// <summary>
    /// 当前AssetBundle名称,可以用于代表主包名称
    /// </summary>
    public string PackageName=string.Empty;
    /// <summary>
    /// 当前这个包中所包含的所有的资源名称
    /// </summary>
    public List<UnityEngine.Object> Assets=new List<UnityEngine.Object>();
}

/// <summary>
/// EditorPackageInfo是用于在Editor环境下收集资源
/// 这个类是用于收集打包成AssetBudnle之间的PackageInfo
/// </summary>
public class PackageInfo
{
    public string PackageName = string.Empty;
    public bool IsSourcePackage = false;
    public List<AssetInfo> assetInfos=new List<AssetInfo>();
    public List<string> PackageDependencies=new List<string>();
}

public class AssetInfo
{
    /// <summary>
    /// 资源名称
    /// </summary>
    public string AssetName;

    /// <summary>
    /// 资源所属AssetBundle名称
    /// </summary>
    public string AssetBundleName;
}

/// <summary>
/// fileName=点击按钮之后创建的默认文件名
/// menuName=在Create菜单中的菜单层级
/// order=规定了创建 ScriptableObject 数据资源文件的路径在菜单上显示的位置
/// order 越低，就显示在越上面。
/// 如果 order 相同，则是按照继承自 ScriptableObject 的脚本创建时间排序，新创建的排在上面。
/// </summary>
[CreateAssetMenu(fileName = "AssetManagerConfig", menuName = "AssetManager/AssetManagerConfig", order = 1)]
public class AssetManagerConfigScirptableObject : ScriptableObject
{
    public Texture2D LogoTexture;
    /// <summary>
    /// 工具标题
    /// </summary>
    public string ManagerTitle;
    /// <summary>
    /// 工具版本
    /// </summary>
    public int AssetBundleToolVersion = 100;
    /// <summary>
    /// AssetBundle包打包版本
    /// </summary>
    public int CurrentBuildVersion = 100;


    public AssetBundlePattern AssetBundleOutputPattern;

    public IncrementalBuildMode InCrementalMode;

    ///// <summary>
    ///// 此处需要注意,ScriptableObject无法序列化具有访问器的变量
    ///// </summary>
    //public DefaultAsset AssetBundleDirectory;

    //public List<string> CurrentAllAssets;

    //public bool[] CurrentSelectedAssets;

    public List<PackageInfoEditor> EditorPackageInfos;

    public string[] InvalidExtentionName = new string[] { ".meta", ".cs" };


}

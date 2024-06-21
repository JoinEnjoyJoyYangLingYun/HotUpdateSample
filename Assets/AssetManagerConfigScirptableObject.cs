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
/// AssetBundle��Ϣ
/// </summary>
[Serializable]
public class PackageInfoEditor
{
    /// <summary>
    /// ��ǰAssetBundle����,�������ڴ�����������
    /// </summary>
    public string PackageName=string.Empty;
    /// <summary>
    /// ��ǰ������������������е���Դ����
    /// </summary>
    public List<UnityEngine.Object> Assets=new List<UnityEngine.Object>();
}

/// <summary>
/// EditorPackageInfo��������Editor�������ռ���Դ
/// ������������ռ������AssetBudnle֮���PackageInfo
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
    /// ��Դ����
    /// </summary>
    public string AssetName;

    /// <summary>
    /// ��Դ����AssetBundle����
    /// </summary>
    public string AssetBundleName;
}

/// <summary>
/// fileName=�����ť֮�󴴽���Ĭ���ļ���
/// menuName=��Create�˵��еĲ˵��㼶
/// order=�涨�˴��� ScriptableObject ������Դ�ļ���·���ڲ˵�����ʾ��λ��
/// order Խ�ͣ�����ʾ��Խ���档
/// ��� order ��ͬ�����ǰ��ռ̳��� ScriptableObject �Ľű�����ʱ�������´������������档
/// </summary>
[CreateAssetMenu(fileName = "AssetManagerConfig", menuName = "AssetManager/AssetManagerConfig", order = 1)]
public class AssetManagerConfigScirptableObject : ScriptableObject
{
    public Texture2D LogoTexture;
    /// <summary>
    /// ���߱���
    /// </summary>
    public string ManagerTitle;
    /// <summary>
    /// ���߰汾
    /// </summary>
    public int AssetBundleToolVersion = 100;
    /// <summary>
    /// AssetBundle������汾
    /// </summary>
    public int CurrentBuildVersion = 100;


    public AssetBundlePattern AssetBundleOutputPattern;

    public IncrementalBuildMode InCrementalMode;

    ///// <summary>
    ///// �˴���Ҫע��,ScriptableObject�޷����л����з������ı���
    ///// </summary>
    //public DefaultAsset AssetBundleDirectory;

    //public List<string> CurrentAllAssets;

    //public bool[] CurrentSelectedAssets;

    public List<PackageInfoEditor> EditorPackageInfos;

    public string[] InvalidExtentionName = new string[] { ".meta", ".cs" };


}

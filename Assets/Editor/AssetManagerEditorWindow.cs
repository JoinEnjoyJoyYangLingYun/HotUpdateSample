using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEngine;


public class AssetManagerEditorWindow : EditorWindow
{

    public string VersionString = "";


    public AssetManagerEditorWindowConfig WindowConfig;
    private void Awake()
    {

        AssetManagerEditor.LoadAssetManagerConfig(this);
        AssetManagerEditor.LoadAssetManagerWindowConfig(this);

        WindowConfig.TitleLabelStyle.alignment = TextAnchor.MiddleCenter;
        WindowConfig.TitleLabelStyle.fontSize = 24;
        WindowConfig.TitleLabelStyle.normal.textColor = Color.red;

        WindowConfig.VersionLabelStyle.alignment = TextAnchor.LowerRight;
        WindowConfig.VersionLabelStyle.fontSize = 14;
        WindowConfig.VersionLabelStyle.normal.textColor = Color.green;



        //LogoLabelStyle.alignment = TextAnchor.MiddleCenter;
        WindowConfig.LogoLabelStyle.fixedWidth = AssetManagerEditor.AssetManagerConfig.LogoTexture.width / 2;
        WindowConfig.LogoLabelStyle.fixedHeight = AssetManagerEditor.AssetManagerConfig.LogoTexture.height / 2;


    }
    /// <summary>
    /// ScriptableObject本身的值是序列化的
    /// 但是在Editor类中声明的ScriptableObject变量是会随着编译而变成默认值(null)的
    /// </summary>
    private void OnValidate()
    {
        AssetManagerEditor.LoadAssetManagerConfig(this);
        AssetManagerEditor.LoadAssetManagerWindowConfig(this);
    }
    private void OnInspectorUpdate()
    {
        AssetManagerEditor.LoadAssetManagerConfig(this);
        AssetManagerEditor.LoadAssetManagerWindowConfig(this);

    }
    private void OnFocus()
    {
        AssetManagerEditor.LoadAssetManagerConfig(this);
        AssetManagerEditor.LoadAssetManagerWindowConfig(this);
    }

    private void OnEnable()
    {
        AssetManagerEditor.GetFolderAllAssets();
    }


    private void OnGUI()
    {
        #region 标题图
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(AssetManagerEditor.AssetManagerConfig.LogoTexture, WindowConfig.LogoLabelStyle);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        #endregion 
        #region 标题
        //固定空白间隔
        GUILayout.Space(20);

        GUILayout.Label(AssetManagerEditor.AssetManagerConfig.ManagerTitle, WindowConfig.TitleLabelStyle);

        #endregion

        #region 版本号
        GUILayout.Space(10);

        GUILayout.Label(VersionString, WindowConfig.VersionLabelStyle);

        #endregion

        #region 打包路径选择

        GUILayout.Space(10);
        AssetManagerEditor.AssetManagerConfig.AssetBundleOutputPattern = (AssetBundlePattern)EditorGUILayout.EnumPopup("打包路径", AssetManagerEditor.AssetManagerConfig.AssetBundleOutputPattern);


        #endregion

        #region 打包资源选择
        GUILayout.Space(10);

        GUILayout.BeginVertical("frameBox");

        for(int i=0;i< AssetManagerEditor.AssetManagerConfig.EditorPackageInfos.Count; i++)
        {
            GUILayout.BeginVertical("frameBox");

            PackageInfoEditor info = AssetManagerEditor.AssetManagerConfig.EditorPackageInfos[i];

            GUILayout.BeginHorizontal();
            info.PackageName= EditorGUILayout.TextField($"PackageInfo{i}",info.PackageName);

            if (GUILayout.Button("Remove"))
            {
                AssetManagerEditor.RemoveAssetBundleInfo(info);
            }
            GUILayout.EndHorizontal();

            #region AssetObject绘制

            if (info.Assets.Count > 0)
            {
                GUILayout.BeginVertical("frameBox");
                for (int j = 0; j < info.Assets.Count; j++)
                {


                    GUILayout.BeginHorizontal();
                    info.Assets[j] = EditorGUILayout.ObjectField(info.Assets[j], typeof(UnityEngine.Object), true) as UnityEngine.Object;

                    if (GUILayout.Button("Remove"))
                    {
                        AssetManagerEditor.RemoveAsset(info, info.Assets[j]);
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }
            if (GUILayout.Button("新增Asset"))
            {
                AssetManagerEditor.AddAsset(info);
            }
            GUILayout.EndVertical();

            #endregion
        }

        GUILayout.Space(10);
        if (GUILayout.Button("新增Package"))
        {
            AssetManagerEditor.AddAssetBundleInfo();
        }
        GUILayout.EndVertical();

        #endregion


        #region 增量打包选择

        GUILayout.Space(10);
        AssetManagerEditor.AssetManagerConfig.InCrementalMode = (IncrementalBuildMode)EditorGUILayout.EnumPopup("增量打包模式", AssetManagerEditor.AssetManagerConfig.InCrementalMode);


        #endregion

        GUILayout.Space(10);
        if (GUILayout.Button("打包AssetBundle"))
        {
            Debug.Log("按钮按下");
            //AssetManagerEditor.BuildAssetBundleFromEditorWindow();
            //AssetManagerEditor.BuildAssetBundleFromFolder();

            AssetManagerEditor.BuildAssetBundleFromDirectedGraph();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("保存Config"))
        {
            AssetManagerEditor.SaveConfigToJSON();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("读取Config"))
        {
            AssetManagerEditor.ReadConfigFromJSON();
        }
    }
}


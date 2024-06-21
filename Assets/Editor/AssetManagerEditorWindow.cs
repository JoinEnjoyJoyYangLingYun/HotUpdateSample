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
    /// ScriptableObject�����ֵ�����л���
    /// ������Editor����������ScriptableObject�����ǻ����ű�������Ĭ��ֵ(null)��
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
        #region ����ͼ
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(AssetManagerEditor.AssetManagerConfig.LogoTexture, WindowConfig.LogoLabelStyle);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        #endregion 
        #region ����
        //�̶��հ׼��
        GUILayout.Space(20);

        GUILayout.Label(AssetManagerEditor.AssetManagerConfig.ManagerTitle, WindowConfig.TitleLabelStyle);

        #endregion

        #region �汾��
        GUILayout.Space(10);

        GUILayout.Label(VersionString, WindowConfig.VersionLabelStyle);

        #endregion

        #region ���·��ѡ��

        GUILayout.Space(10);
        AssetManagerEditor.AssetManagerConfig.AssetBundleOutputPattern = (AssetBundlePattern)EditorGUILayout.EnumPopup("���·��", AssetManagerEditor.AssetManagerConfig.AssetBundleOutputPattern);


        #endregion

        #region �����Դѡ��
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

            #region AssetObject����

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
            if (GUILayout.Button("����Asset"))
            {
                AssetManagerEditor.AddAsset(info);
            }
            GUILayout.EndVertical();

            #endregion
        }

        GUILayout.Space(10);
        if (GUILayout.Button("����Package"))
        {
            AssetManagerEditor.AddAssetBundleInfo();
        }
        GUILayout.EndVertical();

        #endregion


        #region �������ѡ��

        GUILayout.Space(10);
        AssetManagerEditor.AssetManagerConfig.InCrementalMode = (IncrementalBuildMode)EditorGUILayout.EnumPopup("�������ģʽ", AssetManagerEditor.AssetManagerConfig.InCrementalMode);


        #endregion

        GUILayout.Space(10);
        if (GUILayout.Button("���AssetBundle"))
        {
            Debug.Log("��ť����");
            //AssetManagerEditor.BuildAssetBundleFromEditorWindow();
            //AssetManagerEditor.BuildAssetBundleFromFolder();

            AssetManagerEditor.BuildAssetBundleFromDirectedGraph();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("����Config"))
        {
            AssetManagerEditor.SaveConfigToJSON();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("��ȡConfig"))
        {
            AssetManagerEditor.ReadConfigFromJSON();
        }
    }
}


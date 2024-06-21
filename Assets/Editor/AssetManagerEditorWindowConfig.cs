using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetManagerWindowConfig", menuName = "AssetManager/AssetManagerWindowConfig", order = 1)]
public class AssetManagerEditorWindowConfig : ScriptableObject
{
    public GUIStyle TitleLabelStyle;
    public GUIStyle VersionLabelStyle;
    public GUIStyle LogoLabelStyle;
}

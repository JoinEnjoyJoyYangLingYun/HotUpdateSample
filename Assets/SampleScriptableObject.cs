using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName ="SO",menuName ="Sample/SO",order =1)]
public class SampleScriptableObject : ScriptableObject
{
    public string Name="Student";
    public int Age=100;
}

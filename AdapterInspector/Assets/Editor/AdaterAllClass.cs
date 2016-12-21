using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Collections;
using UnityEditor;
using Object = UnityEngine.Object;
public class AllSelfInspector : Editor
{
    public override void OnInspectorGUI()
    {
        //try
        //{
            if (GUILayout.Button("窗口"))
            {
                AdapterInspectorWindows.ShowWindows(target, serializedObject);
            }
            AdapterInspector.SetTarget(target, serializedObject);

        //}
        //catch (Exception e)
        //{
            
        //    Debug.LogWarning(e.ToString());
        //}
    }
}
public class AdapterInspectorWindows : EditorWindow
{
    private static Object Target;
    private static SerializedObject serializedObject;
    private Vector2 m_scrollValue=Vector2.zero;
    public static void ShowWindows(Object target,SerializedObject so)
    {
        Target = target;
        serializedObject = so;
        EditorWindow.GetWindow<AdapterInspectorWindows>(false, "编辑窗口", true).Show();
        EditorWindow.FocusWindowIfItsOpen<AdapterInspectorWindows>();
    }

    void OnGUI()
    {
        if (Target!=null)
        {
            m_scrollValue =GUILayout.BeginScrollView(m_scrollValue, false, true);
            AdapterInspector.SetTarget(Target, serializedObject);
            GUILayout.EndScrollView();
        }
    }
}
[CustomEditor(typeof(Test), true)]
public class TestInspector : AllSelfInspector
{


}
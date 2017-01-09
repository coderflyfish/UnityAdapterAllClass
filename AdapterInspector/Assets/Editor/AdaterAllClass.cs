using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Collections;
using UnityEditor;
using Object = UnityEngine.Object;
public class AllSelfInspector : Editor
{
    private static AdapterInspectorWindows windows;
    public override void OnInspectorGUI()
    {
        //try
        //{

        if (GUILayout.Button("窗口") || (windows != null && windows.winState==2))
            {
                windows = AdapterInspectorWindows.ShowWindows(target, serializedObject, CloseWindows);
            }
            AdapterInspector.SetTarget(target, serializedObject);

        //}
        //catch (Exception e)
        //{
            
        //    Debug.LogWarning(e.ToString());
        //}
    }

    void OnDisable()
    {
        if (windows!=null)
        windows.End();
    }

    void CloseWindows()
    {
        windows = null;
    }
}
public class AdapterInspectorWindows : EditorWindow
{
    private Object Target;
    private SerializedObject serializedObject;
    private Vector2 m_scrollValue=Vector2.zero;
    public byte winState = 0;
    private Action closeCall=null;
    public static AdapterInspectorWindows ShowWindows(Object target, SerializedObject so,Action closeCall)
    {        
        AdapterInspectorWindows windows = EditorWindow.GetWindow<AdapterInspectorWindows>(false, "编辑窗口", true);
        windows.Show();
        windows.Target = target;
        windows.serializedObject = so;
        windows.closeCall = closeCall;
        windows.winState = 1;
        EditorWindow.FocusWindowIfItsOpen<AdapterInspectorWindows>();
        return windows;
    }

    public  void End()
    {
        Target = null;
        serializedObject = null;
        winState = 2;
    }
    void OnDestroy()
    {
        if (closeCall != null)
        {
            closeCall();
            closeCall = null;
        }
        winState = 0;
    }
    void OnGUI()
    {
        if (Target != null && serializedObject!=null)
        {
            m_scrollValue =GUILayout.BeginScrollView(m_scrollValue, false, true);
            AdapterInspector.SetTarget(Target, serializedObject);
            GUILayout.EndScrollView();
        }
    }
}
[CustomEditor(typeof(LocalActionController), true)]
public class TestInspector : AllSelfInspector
{


}
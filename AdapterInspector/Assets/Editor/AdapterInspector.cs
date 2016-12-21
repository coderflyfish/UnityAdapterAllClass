using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Reflection;

public static class AdapterInspector
{
    private delegate void ActionMy(ref FieldInfo fieldInfo , params object[] target);
    private static Dictionary<Type, ActionMy> m_typeToOprMethodDic = new Dictionary<Type, ActionMy> 
    {
        //{typeof(int),OprInt},
        //{typeof(float),OprFloat},
        //{typeof(double),OprFloat},
        //{typeof(string),OprString},
        // {typeof(bool),OprBoolean},
        //{typeof(Vector2),OprVector2},
        //{typeof(Vector3),OprVector3},
        //{typeof(List<GameObject>),OprObjectList<GameObject>},
        {typeof(List<>),OprList},
        //{typeof(AnimationCurve),OprAniamtionCurve},
        //{typeof(LayerMask),OprLayerMask},
        //{typeof(Color),OprColor},
    };

    private static bool mEndHorizontal = false;
    private static bool minimalisticLook = false;
    private static SerializedObject m_sp;
    public static void SetTarget(object target, SerializedObject sp)
    {
        m_sp = sp;
        if (sp == null)
            return;
        Type t = target.GetType();
        FieldInfo[] fieldArray = t.GetFields();
        for (int i = 0; i < fieldArray.Length; i++)
        {
            if (fieldArray[i].FieldType.IsSubclassOf(typeof(ScriptableObject)))
            {
                Object value = fieldArray[i].GetValue(target) as Object;
                if (value == null)
                {
                    value = CreateInstance(fieldArray[i].FieldType) as Object;
                    fieldArray[i].SetValue(target, value);
                }
                var sp1 = new SerializedObject(value);
                OprAdapterInspector(sp1, null);
                sp1.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.PropertyField(sp.FindProperty(fieldArray[i].Name));
            }
        }

        m_sp.ApplyModifiedProperties();
        SaveAndLoad(target);
    }

    private static void SaveAndLoad(object target)
    {
        EditorGUILayout.BeginHorizontal();
        bool save = GUILayout.Button("保存");
        bool load = GUILayout.Button("加载");
        if (save || load)
        {
            if (!Directory.Exists(Application.dataPath + "/Resources/"))
            {
                Directory.CreateDirectory(Application.dataPath + "/Resources/");
            }
            var value = target as MonoBehaviour;

            Type t = target.GetType();
            FieldInfo[] fieldArray = t.GetFields();
            string configName = string.Empty;
            if (value)
            {
                configName = value.name;
            }
            for (int i = 0; i < fieldArray.Length; i++)
            {
                if (fieldArray[i].Name == "ConfigName")
                {
                    configName = fieldArray[i].GetValue(target) as string;
                    break;
                }
            }
            for (int i = 0; i < fieldArray.Length; i++)
            {
                if (fieldArray[i].FieldType.IsSubclassOf(typeof(ScriptableObject)))
                {

                    string savePath = "Assets/Resources/" + configName + ".asset";
                    string path = Application.dataPath + "/Resources/" + configName + ".asset";
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    if (save)
                    {
                        Object obj = fieldArray[i].GetValue(target) as ScriptableObject;
                        Object objNew = Object.Instantiate(obj);
                        if (File.Exists(savePath))
                            AssetDatabase.DeleteAsset(savePath);
                       
                        
                        AssetDatabase.CreateAsset(objNew, savePath);
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        Object obj = AssetDatabase.LoadAssetAtPath(savePath, fieldArray[i].FieldType);
                        Object objNew = Object.Instantiate(obj);
                        AssetDatabase.Refresh();
                        fieldArray[i].SetValue(target, objNew);
                    }
                }
            }

        }
        EditorGUILayout.EndHorizontal();
    }

    public static void OprAdapterInspector(object target,object obj)
    {
        Type t = null;
        SerializedProperty pp = null;
        SerializedObject so = null;
        if (target.GetType() == typeof(SerializedObject))
        {
            so = target as SerializedObject;
            t = so.targetObject.GetType();
            obj = so.targetObject;
        }
        else if (target.GetType() == typeof(SerializedProperty))
        {
            pp = target as SerializedProperty;
            t = obj.GetType();
        }

        FieldInfo[] fieldArray = t.GetFields();

        for (int i = 0; i < fieldArray.Length; i++)
        {
            var hide = GetCustomAttr<HideInInspector>(fieldArray[i]);
           if (hide == null)
           {
               SerializedProperty property = null;
               if (so != null)
               {
                   property = so.FindProperty(fieldArray[i].Name);
               }
               else if (pp != null)
               {
                   property = pp.FindPropertyRelative(fieldArray[i].Name);
                   if (property == null)
                   {
                       Debug.LogError(pp.displayName + "  " + fieldArray[i].Name);
                       continue;
                   }
               }
                Attribute subAttr = GetSerializableAttr(fieldArray[i].FieldType);
                if (fieldArray[i].FieldType.IsGenericType)
                {
                    Type genericType = fieldArray[i].FieldType.GetGenericTypeDefinition();
                    if (genericType != null && m_typeToOprMethodDic.ContainsKey(genericType))
                    {
                        m_typeToOprMethodDic[genericType](ref fieldArray[i], obj, property);
                    }
                   
                }
                 else if (fieldArray[i].FieldType.FullName.Contains("[]"))
                 {
                     OprArray(ref fieldArray[i], obj, property);
                 }
                else if (subAttr != null && fieldArray[i].FieldType.IsClass
                    && fieldArray[i].FieldType!=typeof(string))
                 {
                     if (DrawHeader(GetShowName(fieldArray[i], fieldArray[i].Name)))
                     {
                         BeginContents();
                         object field = null;
                         object parent = obj;
                         if (so != null)
                         {
                             parent = so.targetObject;

                         }
                         field = fieldArray[i].GetValue(parent);
                         if (field == null)
                         {
                             field = CreateInstance(fieldArray[i].FieldType);
                             fieldArray[i].SetValue(parent, field);
                         }
                         OprAdapterInspector(property, field);
                         EndContents();
                     }

                 }
                 else
                 {
                     EditorGUILayout.PropertyField(property,
                         new GUIContent(GetShowName(fieldArray[i], fieldArray[i].Name, false)), true);
                 }
            }
        }

    }

    private static bool ObjectListRemoveSame = true;
    private static void OprObjectList(ref FieldInfo field, object target, Type type, SerializedProperty property) 
    {
        Type elementType = field.FieldType.GetGenericArguments()[0];
        BeginContents();
        IList iList = field.GetValue(target) as IList;
       
        if (iList == null)
        {
            EndContents();
            return;
        }
       
        string name = GetShowName(field, field.Name);
        OprIList(iList, name, elementType, true,property);
        EndContents();
    }

    private static void OprIList(IList iList, string name, Type elementType, bool showRemoveSame, SerializedProperty property)
    {
        bool isObject = IsObject(elementType);
        int remove = -1;
        
        if (DrawHeader(name))
        {           
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            var subAttr = GetSerializableAttr(elementType);
            if (showRemoveSame)
            {
                ObjectListRemoveSame = EditorGUILayout.Toggle("允许添加重复项 :", ObjectListRemoveSame);
            }
            //EditorGUILayout.BeginScrollView(Vector2.zero, false, true);
            for (int i = 0; i < iList.Count; i++)
            {
                try
                {
                    EditorGUILayout.BeginHorizontal();
                }
                catch (Exception e)
                {
                   
                }
                
                if (isObject)
                {
                    
                    if ((subAttr != null&&elementType.IsClass))
                    {
                        if (DrawHeader(name + i.ToString()))
                        {
                            BeginContents();
                            object value = iList[i];
                            if (value == null)
                            {
                                iList[i] = CreateInstance(elementType);
                            }

                            OprAdapterInspector(property.GetArrayElementAtIndex(i), value);

                            EndContents();
                        }
                    }
                    //else if ()
                    //{

                    //    if (DrawHeader(name + i.ToString()))
                    //    {
                    //        BeginContents();
                    //        object valueresult = iList[i];
                    //        OprAdapterInspector(ref valueresult);
                    //        EndContents();
                            
                    //    }
                    //}
                    else
                    {
                        Type useType = elementType;
                        if (iList[i] != null)
                        {
                            useType = iList[i].GetType();
                        }
                        iList[i] = EditorGUILayout.ObjectField(i.ToString(), iList[i] as Object, useType, true);
                    }

                }
                
                else
                {
                    iList[i] = ShowOprNormalType(iList[i].GetType(), iList[i], i);
                }

                if (GUILayout.Button("删除"))
                {
                    remove = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            //EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            if (remove >= 0)
            {
                iList.RemoveAt(remove);
            }

            if (isObject)
            {
                if (elementType.IsSubclassOf(typeof(ScriptableObject)) || subAttr!=null)
                {
                    EditorGUILayout.BeginHorizontal();
                   if (GUILayout.Button("拷贝增加"))
                    {
                        object addGo =null;
                        if (iList.Count > 0)
                        {
                            object newValue = CreateInstance(elementType);
                            object oldValue = null;
                            for (int i = iList.Count-1; i >=0; i--)
                            {
                                Debug.Log(i);
                                if (oldValue == null)
                                {
                                    oldValue = iList[i];
                                }
                                else
                                {
                                    break;
                                }
                            }

                            addGo = Copy(oldValue, newValue);
                        }
                        else
                        {
                            addGo = CreateInstance(elementType);
                        }
                       
                        if (addGo != null)
                        {
                            if (ObjectListRemoveSame || !iList.Contains(addGo))
                            {
                                iList.Add(addGo);
                            }
                        }
                    }
                   else if (GUILayout.Button("增加"))
                   {
                       object addGo = CreateInstance(elementType);
                       if (addGo != null)
                       {
                           if (ObjectListRemoveSame || !iList.Contains(addGo))
                           {
                               iList.Add(addGo);
                           }
                       }
                   }
                   EditorGUILayout.EndHorizontal();
                }
                else
                {
                    Object addGo = EditorGUILayout.ObjectField(null, elementType, true);
                    if (addGo != null)
                    {
                        if (ObjectListRemoveSame || !iList.Contains(addGo))
                        {
                            iList.Add(addGo);
                        }
                    }
                }
                //if (iList.Count != 0)
                //{
                //    field.SetValue(target, iList);
                //}
            }
            else
            {
                if (GUILayout.Button("增加"))
                {
                    object newValue = CreateInstance(elementType);
                    if (newValue != null)
                    {
                        iList.Add(newValue);
                    }
                    else
                    {
                        Debug.LogError("can't CreateInstance");
                    }
                }
            }
        }
        
    }
    public static object Copy(object oldValue,object newValue)
    {
        Type type = oldValue.GetType();
        if (newValue == null )
        {
            if (type != typeof (GameObject) && !type.IsSubclassOf(typeof (Component)))
            {
                newValue = CreateInstance(type);
            }
            else
            {
                newValue = oldValue;
                return newValue;
            }
        }
        var fields = type.GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            object ov = fields[i].GetValue(oldValue);
            if (fields[i].FieldType.IsClass)
            {
                if (ov != null)
                {
                    if (fields[i].FieldType.IsGenericType)
                    {
                        Type genericType = fields[i].FieldType.GetGenericTypeDefinition();
                        if (genericType == typeof (List<>))
                        {
                            fields[i].SetValue(newValue, CopyList(fields[i].FieldType,fields[i].GetValue(newValue) ,ov));
                            //Type elementType = field.FieldType.GetGenericArguments()[0];
                            //IList ovList = ov as IList;
                            //IList nvList = fields[i].GetValue(newValue) as IList;
                            //if (elementType.IsClass)
                            //{
                            //    for (int j = 0; j < ovList.Count; j++)
                            //    {
                            //        ovList[i]
                            //    }
                            //}
                            
                        }
                    }
                    else if (fields[i].FieldType.Name.Contains("[]"))
                    {
                         Type elementType = fields[i].FieldType.GetElementType();
                         var genericType = typeof(List<>).MakeGenericType(elementType);
                         var oList = ArrayToList(ov as Array, genericType);
                         object nList = CopyList(oList.GetType(), null, oList);
                        fields[i].SetValue(newValue, ListToArray(nList as IList, elementType));
                    }
                    else
                    {
                        fields[i].SetValue(newValue, Copy(ov, fields[i].GetValue(newValue)));
                    }
                   
                }
                else
                {
                    Debug.LogError(fields[i].FieldType +"==null");
                }
            }
            else
            {
                fields[i].SetValue(newValue,ov);
            }
        }
        return newValue;
    }

    public static object CopyList(Type type,object value ,object defaultValue)
    {
        Type genericType = type.GetGenericTypeDefinition();
        if (value == null)
        {
            value = CreateInstance(type);
        }
        if (defaultValue != null)
        {
            IList nList = value as IList;
            IList oList = defaultValue as IList;
            for (int i = 0; i < oList.Count; i++)
            {
                if (i < nList.Count)
                {
                     nList[i] =Copy(oList[i],nList[i]);
                }
                else
                {
                    Type elementType = type.GetGenericArguments()[0];
                    object nv = CreateInstance(elementType);
                    nList.Add(Copy(oList[i], nv));
                }
            }
        }
        return value;
    }
    private static bool IsObject(Type elementType)
    {
        return elementType.IsSubclassOf(typeof(Object))
           || elementType.IsSubclassOf(typeof(ScriptableObject))
           || elementType.IsClass;
    }

    private static void OprObjectArray(ref FieldInfo field, object target, Type type, SerializedProperty property)
    {
        Type elementType = field.FieldType.GetElementType();
        var genericType = typeof(List<>).MakeGenericType(elementType);

        if (elementType == null)
        {
            Debug.LogError(" elementType == null");
            return;
        }
  
                   
        Array iList = field.GetValue(target) as Array;
        var list = ArrayToList(iList, genericType);
        if (list == null)
        {
            Debug.LogError(" list == null");
            return;
        }
        //if (iList != null)
        //{
        //    for (int i = 0; i < iList.Length; i++)
        //    {
        //        object value = iList.GetValue(i);
        //        if (value == null)
        //        {
        //            value = CreateInstance(elementType);
        //        }
        //        list.Add(value);
        //    }
        //}
        
        BeginContents();
        string name = GetShowName(field, field.Name);
        OprIList(list, name, elementType, false, property);
        //iList = Array.CreateInstance(elementType, list.Count);
        //for (int i = 0; i < iList.Length; i++)
        //{
        //    iList.SetValue(list[i],i);
        //}
        iList = ListToArray(list, elementType);
        field.SetValue(target, iList);
        EndContents();
        
    }

    private static IList ArrayToList(Array array, Type elementType)
    {
        var list = Activator.CreateInstance(elementType) as IList;
        if (list==null)
        {
            return null;
        }
        if (array != null)
        {
            for (int i = 0; i < array.Length; i++)
            {
                object value = array.GetValue(i);
                if (value == null)
                {
                    value = CreateInstance(elementType);
                }
                list.Add(value);
            }
        }
        return list;
    }

    private static Array ListToArray(IList list, Type elementType)
    {
        var array = Array.CreateInstance(elementType, list.Count);
        for (int i = 0; i < array.Length; i++)
        {
            array.SetValue(list[i], i);
        }
        return array;
    }

    private static Attribute GetSerializableAttr(MemberInfo memberInfo)
    {
        if (memberInfo == null)
            return null;
       return Attribute.GetCustomAttribute(memberInfo, typeof(SerializableAttribute));
    }

    private static T GetCustomAttr<T>(MemberInfo memberInfo) where T : Attribute
    {
        return (T)Attribute.GetCustomAttribute(memberInfo, typeof(T));
    }

    private static object CreateInstance(Type elementType)
    {
        object newValue = null;
        if (elementType.IsSubclassOf(typeof (ScriptableObject)))
        {
            newValue = ScriptableObject.CreateInstance(elementType);
        }
        else if (elementType.GetConstructors().Length == 0 || elementType.GetConstructor(new Type[0]) != null)
        {
            newValue = Activator.CreateInstance(elementType);
        }
        else
        {
            ConstructorInfo[] constructors = elementType.GetConstructors();
            for (int i = 0; i < constructors.Length; i++)
            {
                ParameterInfo[] paraInfos = constructors[i].GetParameters();
                object[] parameters = new object[paraInfos.Length];
                Type[] typeArray = new Type[paraInfos.Length];
                for (int j = 0; j < paraInfos.Length; j++)
                {
                    if (paraInfos[j].RawDefaultValue != DBNull.Value)
                    {
                        parameters[j] = paraInfos[j].RawDefaultValue;
                        typeArray[j] = paraInfos[j].ParameterType;
                    }
                    else if (paraInfos[j].ParameterType.GetConstructors().Length == 0 ||
                             paraInfos[j].ParameterType.GetConstructor(new Type[0]) != null)
                    {
                        try
                        {
                            parameters[j] = Activator.CreateInstance(paraInfos[j].ParameterType);
                            typeArray[j] = paraInfos[j].ParameterType;
                        }
                        catch (Exception e)
                        {
                            parameters = new object[0];
                            typeArray = new Type[0];
                            break;
                        }
                    }
                    else
                    {
                        parameters = new object[0];
                        typeArray = new Type[0];
                        break;
                    }

                }
                if (elementType.GetConstructor(typeArray) != null)
                {
                    try
                    {
                        newValue = Activator.CreateInstance(elementType, parameters);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        Debug.LogError(newValue == null);
                    }

                }
                if (newValue != null)
                {
                    break;
                }

            }
        }
        return newValue;
    }
    private static object ShowOprNormalType(Type  elementType ,object para,int no)
    {
        object value = para;
        if (elementType == typeof(int))
        {
            value = EditorGUILayout.IntField(no.ToString(),(int) value);
        }
        else if (elementType == typeof(float))
        {
            value = EditorGUILayout.FloatField(no.ToString(), (float)value);
        }
        else if (elementType == typeof(string))
        {
            value = EditorGUILayout.TextField(no.ToString(), (string)value);
        }
        else if (elementType == typeof(Enum))
        {
            value = EditorGUILayout.EnumPopup(no.ToString(), (Enum)value);
        }
        return value;
    }
    
    private static void OprList(ref FieldInfo field, params object[] target)
    {
        OprObjectList(ref field, target[0], field.FieldType, target[1] as SerializedProperty);
        //EditorGUILayout.ObjectField("List", field.GetValue(target) as Object, field.FieldType);
    }
    private static void OprArray(ref FieldInfo field, object target, SerializedProperty property)
    {
        OprObjectArray(ref field, target, field.FieldType, property);
        //EditorGUILayout.ObjectField("List", field.GetValue(target) as Object, field.FieldType);
    }

    private static string GetShowName(FieldInfo t,string defaultName,bool showHeader=true)
    {
        if (showHeader)
        {
            var headAttr = (HeaderAttribute)Attribute.GetCustomAttribute(t, typeof(HeaderAttribute));
            if (headAttr != null)
            {
                EditorGUILayout.LabelField(headAttr.header);
            }
        }
        var nameattr = (NameAttribute)Attribute.GetCustomAttribute(t, typeof(NameAttribute));
        if (nameattr != null && !string.IsNullOrEmpty(nameattr.Name))
            return nameattr.Name;   
        return defaultName;

       
    }

    static public void BeginContents(bool minimalistic=false)
    {
        if (!minimalistic)
        {
            mEndHorizontal = true;
            GUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(10f));
        }
        else
        {
            mEndHorizontal = false;
            EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(10f));
            GUILayout.Space(10f);
        }
        GUILayout.BeginVertical();
        GUILayout.Space(2f);
    }

    static public void EndContents()
    {
        GUILayout.Space(3f);
        GUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (mEndHorizontal)
        {
            GUILayout.Space(3f);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(3f);
    }

    static public bool DrawHeader(string text) { return DrawHeader(text, text, false, minimalisticLook); }

    /// <summary>
    /// Draw a distinctly different looking header label
    /// </summary>

    static public bool DrawHeader(string text, string key) { return DrawHeader(text, key, false, minimalisticLook); }

    /// <summary>
    /// Draw a distinctly different looking header label
    /// </summary>

    static public bool DrawHeader(string text, bool detailed) { return DrawHeader(text, text, detailed, !detailed); }

    /// <summary>
    /// Draw a distinctly different looking header label
    /// </summary>

    static public bool DrawHeader(string text, string key, bool forceOn, bool minimalistic)
    {
        bool state = EditorPrefs.GetBool(key, true);

        if (!minimalistic) GUILayout.Space(3f);
        if (!forceOn && !state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
        GUILayout.BeginHorizontal();
        GUI.changed = false;

        if (minimalistic)
        {
            if (state) text = "\u25BC" + (char)0x200a + text;
            else text = "\u25BA" + (char)0x200a + text;

            GUILayout.BeginHorizontal();
            GUI.contentColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.7f) : new Color(0f, 0f, 0f, 0.7f);
            if (!GUILayout.Toggle(true, text, "PreToolbar2", GUILayout.MinWidth(20f))) state = !state;
            GUI.contentColor = Color.white;
            GUILayout.EndHorizontal();
        }
        else
        {
            text = "<b><size=11>" + text + "</size></b>";
            if (state) text = "\u25BC " + text;
            else text = "\u25BA " + text;
            if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) state = !state;
        }

        if (GUI.changed) EditorPrefs.SetBool(key, state);

        if (!minimalistic) GUILayout.Space(2f);
        GUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
        if (!forceOn && !state) GUILayout.Space(3f);
        return state;
    }

}

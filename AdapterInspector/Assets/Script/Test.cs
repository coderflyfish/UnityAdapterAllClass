using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    public string ConfigName = "Test";
    public ScriptableDataTest DataTest = null;
    public void ReadSkillInfo()
    {
        string name = gameObject.name;
        if (!string.IsNullOrEmpty(ConfigName))
        {
            name = ConfigName;
        }
        DataTest = Resources.Load<ScriptableDataTest>(name);
        if (DataTest == null)
        {
            DataTest = new ScriptableDataTest();
            Debug.LogError("找不到配置文件 " + name + "在模型" + gameObject.name + "上");
        }
    }
	// Use this for initialization
	void Start ()
	{
	    ReadSkillInfo();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

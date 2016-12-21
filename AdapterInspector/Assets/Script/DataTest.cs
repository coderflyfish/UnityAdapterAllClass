using System.Collections.Generic;
using UnityEngine;
using System.Collections;
[System.Serializable]
public class SkillActionInfo
{
    [Header("设置技能攻击事件")]
    public SkillActionEvents SkillActionEvent = new SkillActionEvents();

    [Header("自身材质修改设置")]
    public List<MatPropertyInfo> SelfMatPropertyList = new List<MatPropertyInfo>();
}

[System.Serializable]
public class AttackEvent
{
    [Header("攻击事件在攻击动作中触发的时间")]
    public float AttackTime;
    public EnumType hitType;
    public float hitReactSpeed = 1;
    public float hitMotionScale = 1;
    [Header("修改材质属性相关目前只支持颜色和数值")]
    public List<MatPropertyInfo> MatPropertyList = new List<MatPropertyInfo>();
    [HideInInspector]
    public bool IsTriggered = false;
}

[System.Serializable]
public class MatPropertyInfo
{
    [Name("材质属性名")]
    public string PropertyName;
    [Name("改材质开始时间")]
    public float StartTime;
    [Name("改材质持续时间")]
    public float KeepTime;
    [Name("是否是颜色")]
    public bool IsColor;
    [Name("材质颜色值")]
    public Color MatColor;
    [Name("材质数值")]
    public float MatValue;
}
[System.Serializable]
public class SkillActionEvents
{
    public bool FlushEventAtExit = true;
    public AttackEvent[] AttackEvents;
}

public enum EnumType
{
    Type0 = 0,
    Type1 = 1,
    Type2 = 2,
    Type3 = 3,
}
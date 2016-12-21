using UnityEngine;
using System.Collections;

public class ScriptableDataTest : ScriptableObject
{
    public float DirectionDampTime = .25f;
    public float Speed = 0.5f;
    public float SkipBTUpTime = 0.2f;
    public float PressTime = 0.2f;
    public SkillActionInfo[] SkillInfos = new SkillActionInfo[9];
}

// SkillData.cs 맨 위에 이거 추가!
using UnityEngine;  // ← 이게 있어야 Sprite를 인식함

[System.Serializable]
public class SkillData
{
    public string skillName;
    public float cooldown;
    public float duration;
    public Sprite icon;
    public string description;
}

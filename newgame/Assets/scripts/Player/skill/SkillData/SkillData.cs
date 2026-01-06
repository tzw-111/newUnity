using UnityEngine;

// 普通C#类，不是ScriptableObject，可直接在Inspector显示
[System.Serializable] // 必须加这个标签，否则Inspector里看不到属性
public class SkillData
{
    [Header("基础配置")]
    public string skillName = "新技能"; // 技能名称
    public float coolDownTime = 2f; // 冷却时间（秒）
    public float castTime = 0f; // 施法前摇（0为瞬发）
    public int damage = 10; // 技能伤害

    [Header("表现配置")]
    public GameObject skillEffectPrefab; // 技能特效预制体
    public GameObject effectSpawnObject; // 特效生成的空物体（直接拖Hierarchy里的对象）
    public AudioClip castSound; // 施法音效
}
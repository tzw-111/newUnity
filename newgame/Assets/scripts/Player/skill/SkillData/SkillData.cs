using UnityEngine;

// 技能数据模板，可在编辑器创建多个实例（比如火球术、治疗术）
[CreateAssetMenu(fileName = "New Skill", menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("基础配置")]
    public string skillName; // 技能名称
    public float coolDownTime = 2f; // 冷却时间（秒）
    public float castTime = 0f; // 施法前摇（0为瞬发）
    public int damage = 10; // 技能伤害

    [Header("表现配置")]
    public GameObject skillEffectPrefab; // 技能特效预制体（比如火球粒子）
    public Transform effectSpawnPoint; // 特效生成位置（角色的手部/武器）
    public AudioClip castSound; // 施法音效
}
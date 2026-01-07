using UnityEngine;

// 技能类型枚举
public enum SkillType
{
    CircleArea,    // 圆形范围伤害
    RectProjectile // 矩形投射伤害
}

[System.Serializable]
public class SkillData
{
    [Header("基础配置")]
    public string skillName = "新技能";
    public float coolDownTime = 2f;
    public float castTime = 0f;
    public int damage = 10;

    [Header("技能类型")]
    public SkillType skillType = SkillType.CircleArea;

    [Header("圆形范围参数")]
    [Tooltip("圆形伤害半径")]
    public float circleRadius = 5f;

    [Header("矩形投射参数")]
    [Tooltip("投射距离")]
    public float castDistance = 10f;
    [Tooltip("矩形宽度")]
    public float boxWidth = 1.5f;
    [Tooltip("矩形高度")]
    public float boxHeight = 0.5f;
    [Tooltip("发射方向（基值，会根据角色朝向自动翻转）")]
    public Vector2 castDirection = new Vector2(1, 0);

    [Header("表现配置")]
    public GameObject skillEffectPrefab;
    public GameObject effectSpawnObject;
    public AudioClip castSound;
}
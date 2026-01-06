using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class CharacterSkillSystem : MonoBehaviour
{
    [Header("技能配置")]
    public List<SkillData> skills = new List<SkillData>(); // 角色拥有的技能列表
    public KeyCode[] skillKeys = { KeyCode.Alpha1, KeyCode.Alpha2 }; // 技能快捷键

    [Header("内部状态（无需手动改）")]
    private Dictionary<int, float> skillCoolDownTimers = new Dictionary<int, float>(); // 技能冷却计时器
    private AudioSource audioSource; // 播放音效的组件

    void Start()
    {
        // 初始化冷却计时器
        for (int i = 0; i < skills.Count; i++)
        {
            skillCoolDownTimers[i] = 0f;
        }

        // 自动添加AudioSource组件（如果没有）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // 1. 更新所有技能的冷却时间
        UpdateSkillCoolDowns();

        // 2. 检测技能输入
        CheckSkillInput();
    }

    // 更新技能冷却
    private void UpdateSkillCoolDowns()
    {
        for (int i = 0; i < skillCoolDownTimers.Count; i++)
        {
            if (skillCoolDownTimers[i] > 0)
            {
                skillCoolDownTimers[i] -= Time.deltaTime;
                // 确保冷却时间不会小于0
                skillCoolDownTimers[i] = Mathf.Max(0, skillCoolDownTimers[i]);
            }
        }
    }

    // 检测技能输入
    private void CheckSkillInput()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            // 检测对应快捷键是否按下，且技能不在冷却中
            if (Input.GetKeyDown(skillKeys[i]) && skillCoolDownTimers[i] <= 0)
            {
                CastSkill(i); // 释放第i个技能
            }
        }
    }

    // 释放技能的核心逻辑
    private void CastSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            UnityEngine.Debug.LogError("技能索引超出范围！");
            return;
        }

        SkillData currentSkill = skills[skillIndex];

        // 1. 标记技能进入冷却
        skillCoolDownTimers[skillIndex] = currentSkill.coolDownTime;

        // 2. 播放施法音效
        if (currentSkill.castSound != null)
        {
            audioSource.PlayOneShot(currentSkill.castSound);
        }

        // 3. 处理施法前摇（如果有）
        if (currentSkill.castTime > 0)
        {
            // 前摇结束后再执行技能效果
            Invoke("ExecuteSkillEffect", currentSkill.castTime);
        }
        else
        {
            // 瞬发技能直接执行效果
            ExecuteSkillEffect(skillIndex);
        }

        UnityEngine.Debug.Log($"释放技能：{currentSkill.skillName}");
    }

    // 执行技能的实际效果（可扩展：伤害、位移、加血等）
    private void ExecuteSkillEffect(int skillIndex)
    {
        SkillData currentSkill = skills[skillIndex];

        // 1. 生成技能特效
        if (currentSkill.skillEffectPrefab != null)
        {
            // 确定特效生成位置（优先用配置的点，没有就用角色位置）
            Transform spawnPoint = currentSkill.effectSpawnPoint != null
                ? currentSkill.effectSpawnPoint
                : transform;

            GameObject effect = Instantiate(currentSkill.skillEffectPrefab, spawnPoint.position, spawnPoint.rotation);
            // 自动销毁特效（避免内存泄漏）
            Destroy(effect, 3f); // 3秒后销毁，可根据特效时长调整
        }

        // 2. 扩展：添加技能伤害检测（比如检测范围内敌人）
        // 示例：检测角色前方5米内的敌人，造成伤害
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f, LayerMask.GetMask("Enemy"));
        foreach (var hitCollider in hitColliders)
        {
            // 假设敌人有EnemyHealth组件，调用TakeDamage方法
            FlyingEyeController enemyHealth = hitCollider.GetComponent<FlyingEyeController>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(currentSkill.damage, Vector2.zero);
            }
        }
    }

    // 外部调用：获取技能剩余冷却时间（比如UI显示）
    public float GetSkillRemainingCoolDown(int skillIndex)
    {
        if (skillCoolDownTimers.ContainsKey(skillIndex))
        {
            return skillCoolDownTimers[skillIndex];
        }
        return 0f;
    }
}
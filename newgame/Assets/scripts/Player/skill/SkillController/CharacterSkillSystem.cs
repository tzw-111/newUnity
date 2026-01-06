using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class CharacterSkillSystem : MonoBehaviour
{
    [Header("技能配置【直接在这里加技能】")]
    public List<SkillData> skills = new List<SkillData>(); // 直接配置多个技能
    public KeyCode[] skillKeys = { KeyCode.Alpha1, KeyCode.Alpha2 }; // 技能快捷键

    [Header("内部状态（无需手动改）")]
    private Dictionary<int, float> skillCoolDownTimers = new Dictionary<int, float>();
    private AudioSource audioSource;

    void Start()
    {
        // 初始化冷却计时器
        for (int i = 0; i < skills.Count; i++)
        {
            skillCoolDownTimers[i] = 0f;
        }

        // 自动添加AudioSource
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        UpdateSkillCoolDowns();
        CheckSkillInput();
    }

    // 更新技能冷却
    private void UpdateSkillCoolDowns()
    {
        for (int i = 0; i < skillCoolDownTimers.Count; i++)
        {
            if (skillCoolDownTimers[i] > 0)
            {
                skillCoolDownTimers[i] = Mathf.Max(0, skillCoolDownTimers[i] - Time.deltaTime);
            }
        }
    }

    // 检测技能输入
    private void CheckSkillInput()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            // 确保索引不越界，且技能不在冷却
            if (i >= skillKeys.Length) break;
            if (Input.GetKeyDown(skillKeys[i]) && skillCoolDownTimers[i] <= 0)
            {
                CastSkill(i);
            }
        }
    }

    // 释放技能核心逻辑
    private void CastSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            UnityEngine.Debug.LogError("技能索引超出范围！");
            return;
        }

        SkillData currentSkill = skills[skillIndex];

        // 标记冷却
        skillCoolDownTimers[skillIndex] = currentSkill.coolDownTime;

        // 播放音效
        if (currentSkill.castSound != null)
        {
            audioSource.PlayOneShot(currentSkill.castSound);
        }

        // 处理施法前摇
        if (currentSkill.castTime > 0)
        {
            Invoke(nameof(ExecuteSkillEffect), currentSkill.castTime);
        }
        else
        {
            ExecuteSkillEffect(skillIndex);
        }

        UnityEngine.Debug.Log($"释放技能：{currentSkill.skillName}");
    }

    // 执行技能效果
    private void ExecuteSkillEffect(int skillIndex)
    {
        SkillData currentSkill = skills[skillIndex];

        // 获取特效生成位置（容错处理）
        Transform spawnPoint = transform;
        if (currentSkill.effectSpawnObject != null)
        {
            spawnPoint = currentSkill.effectSpawnObject.transform;
        }

        // 生成特效
        if (currentSkill.skillEffectPrefab != null)
        {
            GameObject effect = Instantiate(currentSkill.skillEffectPrefab, spawnPoint.position, spawnPoint.rotation);
            Destroy(effect, 3f); // 自动销毁
        }

        // 伤害检测（和之前一致）
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 5f, LayerMask.GetMask("guaiwu"));
        foreach (var hitCollider in hitColliders)
        {
            FlyingEyeController enemyHealth = hitCollider.GetComponent<FlyingEyeController>();
            enemyHealth?.TakeDamage(currentSkill.damage, Vector2.zero);
        }
    }

    // 获取剩余冷却时间（用于UI）
    public float GetSkillRemainingCoolDown(int skillIndex)
    {
        return skillCoolDownTimers.ContainsKey(skillIndex) ? skillCoolDownTimers[skillIndex] : 0f;
    }
}
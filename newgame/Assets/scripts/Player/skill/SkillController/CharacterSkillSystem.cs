using UnityEngine;
using System.Collections.Generic;

public class CharacterSkillSystem : MonoBehaviour
{
    [Header("技能配置")]
    public List<SkillData> skills = new List<SkillData>();
    public KeyCode[] skillKeys = { KeyCode.Alpha1, KeyCode.Alpha2 };

    private Dictionary<int, float> skillCoolDownTimers = new Dictionary<int, float>();
    private AudioSource audioSource;
    private int currentSkillIndex = -1;

    // 【新增】引用PlayerController获取准确朝向
    private PlayerController playerController;

    void Start()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            skillCoolDownTimers[i] = 0f;
        }

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // 【新增】获取PlayerController
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("【启动检查】未找到PlayerController组件！朝向判断可能不准确！");
        }

        if (skills.Count > 0 && skills[0].skillEffectPrefab != null)
        {
            Debug.Log($"【启动检查】检测到特效预制体: {skills[0].skillEffectPrefab.name}");
        }

        int layerMask = LayerMask.GetMask("guaiwu");
        if (layerMask == 0)
        {
            Debug.LogError("【启动检查】Layer 'guaiwu' 不存在！");
        }
    }

    void Update()
    {
        UpdateSkillCoolDowns();
        CheckSkillInput();
    }

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

    private void CheckSkillInput()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (i >= skillKeys.Length) break;
            if (Input.GetKeyDown(skillKeys[i]) && skillCoolDownTimers[i] <= 0)
            {
                CastSkill(i);
            }
        }
    }

    private void CastSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            Debug.LogError("技能索引超出范围！");
            return;
        }

        SkillData currentSkill = skills[skillIndex];
        if (currentSkill == null)
        {
            Debug.LogError($"skills[{skillIndex}] 为null！");
            return;
        }

        skillCoolDownTimers[skillIndex] = currentSkill.coolDownTime;

        if (currentSkill.castSound != null)
        {
            audioSource.PlayOneShot(currentSkill.castSound);
        }

        if (currentSkill.castTime > 0)
        {
            currentSkillIndex = skillIndex;
            Invoke(nameof(DelayedExecuteSkill), currentSkill.castTime);
        }
        else
        {
            ExecuteSkillEffect(skillIndex);
        }

        Debug.Log($"释放技能：{currentSkill.skillName} | 类型：{currentSkill.skillType}");
    }

    private void DelayedExecuteSkill()
    {
        if (currentSkillIndex >= 0)
        {
            ExecuteSkillEffect(currentSkillIndex);
            currentSkillIndex = -1;
        }
    }

    private void ExecuteSkillEffect(int skillIndex)
    {
        SkillData currentSkill = skills[skillIndex];
        Debug.Log($"[技能系统] 执行技能: {currentSkill.skillName} | 类型: {currentSkill.skillType}");

        // 获取朝向
        float facingDirection = playerController != null ? playerController.FacingDirection :
                               (transform.localScale.x < 0 ? -1f : 1f);

        Vector3 positionOffset;
        if (currentSkill.skillType == SkillType.CircleArea)
        {
            positionOffset = Vector3.up * 0.3f;
        }
        else // RectProjectile
        {
            positionOffset = new Vector3(0.5f * facingDirection, 0.2f, 0f);
        }

        Vector3 spawnPos = transform.position + positionOffset;
        Quaternion spawnRotation = transform.rotation;

        // 生成特效
        if (currentSkill.skillEffectPrefab != null)
        {
            GameObject effect = Instantiate(currentSkill.skillEffectPrefab, spawnPos, spawnRotation);
            if (effect == null)
            {
                Debug.LogError($"[技能系统] Instantiate失败！预制体损坏: {currentSkill.skillEffectPrefab.name}");
                return;
            }

            // 【核心修复】矩形技能根据朝向翻转特效
            if (currentSkill.skillType == SkillType.RectProjectile && facingDirection < 0)
            {
                // 2D游戏：翻转X轴让特效向左
                Vector3 effectScale = effect.transform.localScale;
                effectScale.x *= -1;
                effect.transform.localScale = effectScale;
            }

            Debug.Log($"[技能系统] 特效已生成: {effect.name}");
            Destroy(effect, 2f);
        }

        // 伤害逻辑
        switch (currentSkill.skillType)
        {
            case SkillType.CircleArea:
                PerformCircleDamage(currentSkill);
                break;
            case SkillType.RectProjectile:
                PerformRectangularDamage(currentSkill, facingDirection);
                break;
        }
    }

    private void PerformCircleDamage(SkillData skill)
    {
        int layerMask = LayerMask.GetMask("guaiwu");
        if (layerMask == 0)
        {
            Debug.LogError("Layer 'guaiwu' 未创建！");
            return;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, skill.circleRadius, layerMask);
        Debug.Log($"[圆形伤害] 检测到 {hitColliders.Length} 个目标");

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider == null) continue;

            FlyingEyeController enemyHealth = hitCollider.GetComponent<FlyingEyeController>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(skill.damage, Vector2.zero);
            }
        }

        DebugDrawCircle(transform.position, skill.circleRadius, Color.red, 1f);
    }

    // 【修改】增加facingDirection参数，避免重复计算
    private void PerformRectangularDamage(SkillData skill, float facingDirection)
    {
        Vector2 direction = new Vector2(skill.castDirection.x * facingDirection, skill.castDirection.y);
        Vector2 origin = transform.position;
        Vector2 boxSize = new Vector2(skill.boxWidth, skill.boxHeight);

        Debug.Log($"[矩形伤害] 发射方向: {direction}, 起点: {origin}, 尺寸: {boxSize}");

        int layerMask = LayerMask.GetMask("guaiwu");
        if (layerMask == 0)
        {
            Debug.LogError("Layer 'guaiwu' 未创建！");
            return;
        }

        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, boxSize, 0f, direction, skill.castDistance, layerMask);
        Debug.Log($"[矩形伤害] 检测到 {hits.Length} 个目标");

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            FlyingEyeController enemyHealth = hit.collider.GetComponent<FlyingEyeController>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(skill.damage, direction);
                Debug.Log($"[矩形伤害] 命中: {hit.collider.name}");
            }
        }

        DebugDrawBoxCast(origin, boxSize, direction, skill.castDistance, Color.yellow, 1f);
    }

    #region Debug Visualization
    private void DebugDrawBoxCast(Vector2 origin, Vector2 size, Vector2 direction, float distance, Color color, float duration)
    {
        Vector2 endCenter = origin + direction * distance;
        DrawBox(origin, size, color, duration);
        DrawBox(endCenter, size, color, duration);

        Vector2 halfSize = size * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 offset = new Vector2(
                (i % 2 == 0 ? -1 : 1) * halfSize.x,
                (i < 2 ? -1 : 1) * halfSize.y
            );
            Debug.DrawLine(origin + offset, endCenter + offset, color, duration);
        }
    }

    private void DrawBox(Vector2 center, Vector2 size, Color color, float duration)
    {
        Vector2 halfSize = size * 0.5f;
        Vector2 topLeft = center + new Vector2(-halfSize.x, halfSize.y);
        Vector2 topRight = center + new Vector2(halfSize.x, halfSize.y);
        Vector2 bottomLeft = center + new Vector2(-halfSize.x, -halfSize.y);
        Vector2 bottomRight = center + new Vector2(halfSize.x, -halfSize.y);

        Debug.DrawLine(topLeft, topRight, color, duration);
        Debug.DrawLine(topRight, bottomRight, color, duration);
        Debug.DrawLine(bottomRight, bottomLeft, color, duration);
        Debug.DrawLine(bottomLeft, topLeft, color, duration);
    }

    private void DebugDrawCircle(Vector2 center, float radius, Color color, float duration)
    {
        const int segments = 32;
        Vector2 prevPoint = center + new Vector2(radius, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            Vector2 newPoint = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            Debug.DrawLine(prevPoint, newPoint, color, duration);
            prevPoint = newPoint;
        }
    }
    #endregion

    public float GetSkillRemainingCoolDown(int skillIndex)
    {
        return skillCoolDownTimers.ContainsKey(skillIndex) ? skillCoolDownTimers[skillIndex] : 0f;
    }
}
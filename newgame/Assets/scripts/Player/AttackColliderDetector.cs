using UnityEngine;

/// <summary>
/// 攻击判定框碰撞检测脚本，挂载到每个玩家攻击判定框上
/// </summary>
public class AttackColliderDetector : MonoBehaviour
{
    // 攻击伤害（可在Inspector面板调整，对应怪物受击的damage参数）
    [Tooltip("该段普攻造成的伤害")]
    public float attackDamage = 1f;

    // 新增：攻击命中粒子特效预制体（拖拽你的PixelHitVFX预制体进来）
    [Tooltip("攻击命中时播放的粒子特效预制体")]
    public GameObject hitVFXPrefab;

    // 新增：粒子自动销毁时间（建议和粒子生命周期一致，比如0.8秒）
    [Tooltip("粒子特效播放后自动销毁的时间（匹配粒子生命周期）")]
    public float vfxDestroyTime = 0.8f;

    // 碰撞进入时触发（判定框是Trigger，所以用OnTriggerEnter2D）
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 仅检测保留的敌人脚本 FlyingEyeController（已移除Enemy2DController相关逻辑）
        FlyingEyeController flyingEye = other.GetComponent<FlyingEyeController>();
        if (flyingEye != null)
        {
            // 计算受击方向：从怪物位置指向玩家攻击判定框的父物体（玩家）位置
            Vector2 hitDirection = (transform.parent.position - other.transform.position).normalized;
            // 调用FlyingEyeController的受击方法
            flyingEye.TakeDamage(attackDamage, hitDirection);
            // 调试日志
            UnityEngine.Debug.Log("攻击判定框命中 FlyingEyeController，造成 " + attackDamage + " 点伤害");

            // 新增：播放命中粒子特效（判断预制体是否赋值，避免空指针报错）
            if (hitVFXPrefab != null)
            {
                // 获取命中位置（怪物碰撞体的中心点，也可以用碰撞点更精准）
                Vector3 hitPosition = other.transform.position;
                // 实例化粒子特效（保持默认旋转，也可根据受击方向调整）
                GameObject spawnedVfx = Instantiate(hitVFXPrefab, hitPosition, Quaternion.identity);
                // 粒子播放完毕后自动销毁，避免内存占用
                Destroy(spawnedVfx, vfxDestroyTime);
            }
        }
    }

    // 可选：如果需要持续碰撞时重复触发（比如多段伤害），可添加OnTriggerStay2D
    // private void OnTriggerStay2D(Collider2D other)
    // {
    //     // 逻辑同OnTriggerEnter2D，可添加冷却避免重复调用过于频繁
    // }
}
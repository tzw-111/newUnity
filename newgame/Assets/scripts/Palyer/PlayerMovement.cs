using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 玩家移动与状态管理核心脚本
/// 功能：四向移动、待机、喝药（最高优先级）、左右三连击普攻（带攻击判定框）、受击闪烁+无敌
/// 优先级：喝药 > 普攻 > 移动
/// 适配动画：左1(pugong1_L)、右1(pugong1_R)、左2(pugong2_L)、右2(pugong2_R)、左3(pugong3_L)、右3(pugong3_R)
/// 新增：攻击判定碰撞检测、防重复命中机制
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))] // 依赖Health组件，自动挂载
public class PlayerController : MonoBehaviour
{
    #region 配置参数（Inspector可调）
    [Header("移动配置")]
    [Tooltip("角色移动速度")]
    public float moveSpeed = 5f;

    [Header("喝药配置")]
    [Tooltip("喝药触发按键（单击触发）")]
    public KeyCode drinkKey = KeyCode.R;
    [Tooltip("喝药动画时长（需与实际动画长度一致，单位：秒）")]
    public float drinkAnimationDuration = 0.375f;

    [Header("三连击普攻配置")]
    [Tooltip("普攻触发按键（鼠标左键）")]
    public KeyCode attackKey = KeyCode.Mouse0;
    [Tooltip("单段普攻动画时长（单位：秒）")]
    public float attackAnimationDuration = 0.5f;
    [Tooltip("连击间隔超时时间（窗口期，超过则重置连击，单位：秒）")]
    public float comboTimeoutDuration = 0.5f;

    [Header("攻击判定框配置")]
    [Tooltip("第1段普攻判定框")]
    public GameObject AttackCollider_1;
    [Tooltip("第2段普攻判定框")]
    public GameObject AttackCollider_2;
    [Tooltip("第3段普攻判定框")]
    public GameObject AttackCollider_3;
    [Tooltip("各段普攻伤害")]
    public float attackDamage_1 = 1f;
    public float attackDamage_2 = 2f;
    public float attackDamage_3 = 3f;
    [Tooltip("攻击命中冷却时间（防重复命中，单位：秒）")]
    public float hitCoolDown = 0.1f;

    [Header("受击配置（可自定义）")]
    [Tooltip("单次闪烁时长（降低/恢复各一次的时长）")]
    public float singleFlashTime = 0.1f;
    [Tooltip("受击闪烁总次数")]
    public int flashTimes = 3;
    [Tooltip("闪烁时的目标透明度（0~1，数值越小越透明）")]
    public float targetAlpha = 0.3f;
    #endregion

    #region 组件与状态变量
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _playerSpriteRenderer; // 控制主角透明度
    private Health _playerHealth; // 玩家生命值组件

    // 输入缓存
    private float _horizontalInput;
    private float _verticalInput;

    // 状态锁定（核心：确保朝向永不丢失）
    private float _lockedHorizontalDir = 1f; // 1:右  -1:左
    private bool _isFirstFrame = true;       // 初始化帧标记

    // 基础状态标记
    private bool _isMoving = false;
    private bool _isDrinking = false;        // 喝药状态（最高优先级）
    private float _drinkTimer = 0f;          // 喝药计时器

    // 三连击普攻状态标记
    private bool _isAttacking = false;       // 普攻状态（中优先级）
    private int _attackComboCount = 0;       // 当前连击数（0=无连击，1/2/3=对应三连击）
    private float _attackTimer = 0f;         // 单段普攻计时器
    private float _comboTimeoutTimer = 0f;   // 连击窗口期计时器

    // 受击无敌标记
    private bool _isInvincible = false;

    // 防重复命中相关（目标改为FlyingEyeController）
    private Dictionary<GameObject, float> _hitEnemyRecord = new Dictionary<GameObject, float>();
    #endregion

    #region 初始化
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();
        _playerHealth = GetComponent<Health>();

        // 组件缺失警告
        if (_playerSpriteRenderer == null)
        {
            Debug.LogWarning("主角未挂载SpriteRenderer组件，受击闪烁效果无法生效！");
        }
        if (_playerHealth == null)
        {
            Debug.LogError("主角未挂载Health组件，生命值相关逻辑无法生效！");
        }

        InitializeInitialState();
        InitAttackColliderState();
        InitAttackColliderTrigger();
    }

    /// <summary>
    /// 初始化角色初始状态（强制锁定右向）
    /// </summary>
    private void InitializeInitialState()
    {
        // 强制设置Animator参数
        _anim.SetFloat("Horizontal", _lockedHorizontalDir);
        _anim.SetBool("IsMoving", false);
        _anim.SetFloat("Vertical", 0);
        _anim.SetBool("IsDrinking", false);
        _anim.SetBool("IsAttacking", false);
        _anim.SetFloat("AttackComboCount", 0f);

        // 强制刷新动画状态，避免初始帧参数漂移
        _anim.Update(0);

        // 物理初始化
        _rb.velocity = Vector2.zero;
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
    }

    /// <summary>
    /// 初始化攻击判定框：默认关闭所有碰撞器，设置为触发器
    /// </summary>
    private void InitAttackColliderState()
    {
        SetColliderEnabled(AttackCollider_1, false);
        SetColliderEnabled(AttackCollider_2, false);
        SetColliderEnabled(AttackCollider_3, false);
    }

    /// <summary>
    /// 初始化攻击判定框：设置为触发器，确保碰撞检测生效
    /// </summary>
    private void InitAttackColliderTrigger()
    {
        SetColliderAsTrigger(AttackCollider_1, true);
        SetColliderAsTrigger(AttackCollider_2, true);
        SetColliderAsTrigger(AttackCollider_3, true);
    }
    #endregion

    #region 每帧逻辑
    private void Update()
    {
        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            return;
        }

        // 1. 获取输入 & 同步朝向（优先确保朝向正确）
        HandleInput();
        ForceSyncHorizontalDir();

        // 2. 最高优先级：喝药逻辑
        HandleDrinkLogic();
        if (_isDrinking) return;

        // 3. 中优先级：普攻逻辑
        HandleAttackLogic();
        if (_isAttacking) return;

        // 4. 最低优先级：移动/待机
        UpdateMovementState();
        SyncAnimatorParams();

        // 更新命中敌人的冷却记录（移除已超时的记录）
        UpdateHitEnemyCoolDown();
    }

    /// <summary>
    /// 强制同步朝向（包含判定框翻转）
    /// </summary>
    private void ForceSyncHorizontalDir()
    {
        // 同步Animator朝向参数（控制左右动画切换）
        _anim.SetFloat("Horizontal", _lockedHorizontalDir);
        _anim.Update(0); // 立即生效

        // 同步攻击判定框的左右翻转
        SyncAttackColliderScale();
    }

    /// <summary>
    /// 输入处理（仅在有左右输入时更新朝向）
    /// </summary>
    private void HandleInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if (_horizontalInput != 0)
        {
            _lockedHorizontalDir = _horizontalInput;
        }
    }

    /// <summary>
    /// 喝药逻辑（最高优先级）
    /// </summary>
    private void HandleDrinkLogic()
    {
        if (Input.GetKeyDown(drinkKey) && !_isDrinking)
        {
            EnterDrinkState();
        }

        if (_isDrinking)
        {
            _drinkTimer += Time.deltaTime;
            if (_drinkTimer >= drinkAnimationDuration)
            {
                ExitDrinkState();
            }
            ResetAttackState(); // 喝药打断普攻
            _isMoving = false;
            _anim.SetBool("IsMoving", false);
        }
    }

    /// <summary>
    /// 普攻逻辑（中优先级）
    /// </summary>
    private void HandleAttackLogic()
    {
        // 触发普攻（非喝药、非攻击中、连击数未满）
        if (Input.GetKeyDown(attackKey) && !_isDrinking && _attackComboCount < 3 && !_isAttacking)
        {
            ForceSyncHorizontalDir(); // 触发前再同步一次朝向
            UpdateComboCount();
            EnterAttackState();
        }

        // 普攻倒计时
        if (_isAttacking)
        {
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackAnimationDuration)
            {
                ExitSingleAttackState();
            }
        }

        // 连击窗口期超时检测
        HandleComboTimeout();

        // 普攻中锁定移动
        if (_isAttacking)
        {
            _isMoving = false;
            _anim.SetBool("IsMoving", false);
            _anim.SetFloat("AttackComboCount", _attackComboCount);
        }
    }

    /// <summary>
    /// 更新连击数
    /// </summary>
    private void UpdateComboCount()
    {
        _attackComboCount = _attackComboCount == 0 ? 1 : _attackComboCount + 1;
    }
    #endregion

    #region 移动逻辑
    private void UpdateMovementState()
    {
        _isMoving = (_horizontalInput != 0 || _verticalInput != 0);
    }

    private void SyncAnimatorParams()
    {
        _anim.SetFloat("Vertical", _isMoving ? _verticalInput : 0);
        _anim.SetBool("IsMoving", _isMoving);
        _anim.SetBool("IsDrinking", _isDrinking);
        _anim.SetBool("IsAttacking", _isAttacking);
        _anim.SetFloat("AttackComboCount", _attackComboCount);
    }

    private void FixedUpdate()
    {
        if (_isDrinking || _isAttacking || _isFirstFrame)
        {
            _rb.velocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = new Vector2(_horizontalInput, _verticalInput).normalized;
        _rb.velocity = moveDir * moveSpeed;
    }
    #endregion

    #region 喝药状态
    private void EnterDrinkState()
    {
        _isDrinking = true;
        _anim.SetBool("IsDrinking", true);
        _rb.velocity = Vector2.zero;
        _drinkTimer = 0f;
    }

    private void ExitDrinkState()
    {
        _isDrinking = false;
        _anim.SetBool("IsDrinking", false);
        _drinkTimer = 0f;
        _isMoving = (_horizontalInput != 0 || _verticalInput != 0);
        _anim.SetBool("IsMoving", _isMoving);
    }
    #endregion

    #region 普攻状态
    private void EnterAttackState()
    {
        _isAttacking = true;
        _anim.SetBool("IsAttacking", true);
        _rb.velocity = Vector2.zero;
        _attackTimer = 0f;
        _comboTimeoutTimer = 0f; // 重置连击窗口期
    }

    private void ExitSingleAttackState()
    {
        _isAttacking = false;
        _attackTimer = 0f;
        _anim.SetBool("IsAttacking", false);

        // 三连击完成后重置
        if (_attackComboCount >= 3)
        {
            ResetAttackState();
            _isMoving = (_horizontalInput != 0 || _verticalInput != 0);
            _anim.SetBool("IsMoving", _isMoving);
        }
    }

    private void HandleComboTimeout()
    {
        if (_attackComboCount > 0 && !_isAttacking)
        {
            _comboTimeoutTimer += Time.deltaTime;
            if (_comboTimeoutTimer >= comboTimeoutDuration)
            {
                ResetAttackState();
            }
        }
        else
        {
            _comboTimeoutTimer = 0f;
        }
    }

    private void ResetAttackState()
    {
        _isAttacking = false;
        _attackComboCount = 0;
        _comboTimeoutTimer = 0f;
        _attackTimer = 0f;
        _anim.SetBool("IsAttacking", false);
        _anim.SetFloat("AttackComboCount", _attackComboCount);
        // 重置时关闭所有判定框
        InitAttackColliderState();
        // 清空命中记录
        _hitEnemyRecord.Clear();
    }
    #endregion

    #region 攻击判定框控制（适配FlyingEyeController）
    /// <summary>
    /// 开启对应段的攻击判定框（动画事件调用，参数：1/2/3）
    /// </summary>
    public void EnableAttackCollider(int colliderIndex)
    {
        switch (colliderIndex)
        {
            case 1: SetColliderEnabled(AttackCollider_1, true); break;
            case 2: SetColliderEnabled(AttackCollider_2, true); break;
            case 3: SetColliderEnabled(AttackCollider_3, true); break;
        }
    }

    /// <summary>
    /// 关闭对应段的攻击判定框（动画事件调用，参数：1/2/3）
    /// </summary>
    public void DisableAttackCollider(int colliderIndex)
    {
        switch (colliderIndex)
        {
            case 1: SetColliderEnabled(AttackCollider_1, false); break;
            case 2: SetColliderEnabled(AttackCollider_2, false); break;
            case 3: SetColliderEnabled(AttackCollider_3, false); break;
        }
    }

    /// <summary>
    /// 辅助方法：设置判定框的碰撞器启用状态
    /// </summary>
    private void SetColliderEnabled(GameObject colliderObj, bool isEnabled)
    {
        if (colliderObj == null) return;
        PolygonCollider2D collider = colliderObj.GetComponent<PolygonCollider2D>();
        if (collider != null) collider.enabled = isEnabled;
    }

    /// <summary>
    /// 辅助方法：设置判定框为触发器
    /// </summary>
    private void SetColliderAsTrigger(GameObject colliderObj, bool isTrigger)
    {
        if (colliderObj == null) return;
        PolygonCollider2D collider = colliderObj.GetComponent<PolygonCollider2D>();
        if (collider != null) collider.isTrigger = isTrigger;
    }

    /// <summary>
    /// 同步判定框的左右翻转（跟随角色朝向）
    /// </summary>
    private void SyncAttackColliderScale()
    {
        SyncColliderScale(AttackCollider_1);
        SyncColliderScale(AttackCollider_2);
        SyncColliderScale(AttackCollider_3);
    }

    /// <summary>
    /// 辅助方法：同步单个判定框的缩放
    /// </summary>
    private void SyncColliderScale(GameObject colliderObj)
    {
        if (colliderObj == null) return;
        Vector3 scale = colliderObj.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * _lockedHorizontalDir; // 仅翻转X轴
        colliderObj.transform.localScale = scale;
    }

    /// <summary>
    /// 攻击判定框碰撞检测（触发器回调，适配FlyingEyeController）
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 仅在攻击状态下触发碰撞检测
        if (!_isAttacking) return;

        // 获取FlyingEyeController（替代原Enemy2DController）
        FlyingEyeController enemy = other.GetComponent<FlyingEyeController>();
        if (enemy == null) return;

        // 检查该敌人是否在冷却期内，避免重复命中
        if (IsEnemyInCoolDown(enemy.gameObject)) return;

        // 根据当前连击数获取对应伤害
        float damage = GetCurrentAttackDamage();

        // 计算受击方向：从怪物位置指向玩家位置（确保击退方向正确）
        Vector2 hitDirection = (transform.position - other.transform.position).normalized;

        // 调用怪物受击方法
        enemy.TakeDamage(damage, hitDirection);

        // 记录该敌人的命中时间，开启冷却
        RecordHitEnemy(enemy.gameObject);

        // 调试日志
        Debug.Log($"第{_attackComboCount}段普攻命中敌人，造成{damage}点伤害");
    }

    /// <summary>
    /// 根据当前连击数获取对应伤害
    /// </summary>
    private float GetCurrentAttackDamage()
    {
        switch (_attackComboCount)
        {
            case 1: return attackDamage_1;
            case 2: return attackDamage_2;
            case 3: return attackDamage_3;
            default: return 0f;
        }
    }

    /// <summary>
    /// 记录命中的敌人及冷却时间
    /// </summary>
    private void RecordHitEnemy(GameObject enemyObj)
    {
        if (_hitEnemyRecord.ContainsKey(enemyObj))
        {
            // 若已存在，更新冷却时间
            _hitEnemyRecord[enemyObj] = Time.time;
        }
        else
        {
            // 若不存在，添加新记录
            _hitEnemyRecord.Add(enemyObj, Time.time);
        }
    }

    /// <summary>
    /// 检查敌人是否在冷却期内
    /// </summary>
    private bool IsEnemyInCoolDown(GameObject enemyObj)
    {
        if (_hitEnemyRecord.TryGetValue(enemyObj, out float lastHitTime))
        {
            // 若当前时间 - 上次命中时间 < 冷却时间，说明仍在冷却
            return Time.time - lastHitTime < hitCoolDown;
        }
        // 无记录则不在冷却期
        return false;
    }

    /// <summary>
    /// 更新命中敌人的冷却记录，移除已超时的记录（优化内存）
    /// </summary>
    private void UpdateHitEnemyCoolDown()
    {
        List<GameObject> enemiesToRemove = new List<GameObject>();

        foreach (var kvp in _hitEnemyRecord)
        {
            if (Time.time - kvp.Value >= hitCoolDown)
            {
                enemiesToRemove.Add(kvp.Key);
            }
        }

        // 移除超时的记录
        foreach (var enemy in enemiesToRemove)
        {
            _hitEnemyRecord.Remove(enemy);
        }
    }
    #endregion

    #region 受击闪烁+无敌逻辑
    /// <summary>
    /// 公共方法：触发主角受击（外部可调用，如敌人攻击时）
    /// 整合Health扣血逻辑
    /// </summary>
    /// <param name="damage">受击伤害</param>
    public void TakeDamage(float damage)
    {
        // 无敌状态下不重复触发
        if (_isInvincible)
        {
            Debug.Log("当前处于无敌状态，不受伤害！");
            return;
        }

        // 调用Health组件扣血
        if (_playerHealth != null)
        {
            _playerHealth.TakeDamage(damage);
        }

        // 启动闪烁协程和无敌协程
        StartCoroutine(HitFlashCoroutine());
        StartCoroutine(InvincibleCoroutine());
    }

    /// <summary>
    /// 协程：受击闪烁动画（异步执行，不打断其他状态）
    /// </summary>
    /// <returns></returns>
    private IEnumerator HitFlashCoroutine()
    {
        if (_playerSpriteRenderer == null) yield break;

        // 记录原始透明度
        float originalAlpha = _playerSpriteRenderer.color.a;

        // 循环执行闪烁
        for (int i = 0; i < flashTimes; i++)
        {
            // 降低透明度
            SetSpriteAlpha(targetAlpha);
            yield return new WaitForSeconds(singleFlashTime);

            // 恢复透明度
            SetSpriteAlpha(originalAlpha);
            yield return new WaitForSeconds(singleFlashTime);
        }

        // 最终强制恢复原始透明度
        SetSpriteAlpha(originalAlpha);
    }

    /// <summary>
    /// 协程：受击无敌逻辑（与闪烁时长同步）
    /// </summary>
    /// <returns></returns>
    private IEnumerator InvincibleCoroutine()
    {
        // 开启无敌
        _isInvincible = true;
        Debug.Log("进入无敌状态！");

        // 无敌时长 = 闪烁总时长（单次时长 * 2 * 次数）
        float invincibleDuration = singleFlashTime * 2 * flashTimes;
        yield return new WaitForSeconds(invincibleDuration);

        // 关闭无敌
        _isInvincible = false;
        Debug.Log("无敌状态结束！");
    }

    /// <summary>
    /// 辅助方法：设置主角透明度
    /// </summary>
    /// <param name="alpha">目标透明度（0~1）</param>
    private void SetSpriteAlpha(float alpha)
    {
        if (_playerSpriteRenderer == null) return;

        Color currentColor = _playerSpriteRenderer.color;
        currentColor.a = Mathf.Clamp01(alpha); // 确保透明度在0~1之间
        _playerSpriteRenderer.color = currentColor;
    }
    #endregion
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 敌人状态枚举（独立定义，不再依赖Enemy2DController）
public enum EnemyState
{
    Patrol,     // 巡逻
    Chase,      // 追击
    Attack,     // 攻击
    Hurt,       // 受击
    Dead        // 死亡
}

public class FlyingEyeController : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("基础属性")]
    public float moveSpeed = 2f;

    [Header("二维巡逻设置")]
    public Vector2 patrolAreaSize = new Vector2(8f, 6f);
    private Vector2 currentPatrolTarget;
    public float minPatrolWaitTime = 2f;
    public float maxPatrolWaitTime = 4f;
    private float patrolWaitTimer;
    private bool isWaitingAtPatrolPoint = false;
    private Vector2 patrolStartPos;

    [Header("圆形侦测设置")]
    public float detectRadius = 8f;
    public Transform detectCenter;
    public LayerMask targetLayer;
    public LayerMask obstacleLayer;

    [Header("攻击设置")]
    public float attackRange = 1.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("攻击判定框")]
    public GameObject attackColliderObject;

    [Header("受击设置")]
    public float hurtForce = 5f;
    public float hurtDuration = 0.07f;

    [Header("死亡设置")]
    public float deathDestroyDelay = 0f;
    public GameObject deathEffect;

    private bool isDying = false;
    private bool isInHurtAnimation = false;

    // 核心：记录当前攻击周期已受伤的目标
    private HashSet<Collider2D> damagedTargets = new HashSet<Collider2D>();

    public EnemyState currentState;
    private Rigidbody2D rb;
    private Animator animator;
    private Transform target;
    private FlyingEyeAttackTrigger attackTrigger;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (detectCenter == null) detectCenter = transform;

        currentHealth = maxHealth;
        patrolStartPos = transform.position;
        lastAttackTime = -attackCooldown;
        patrolWaitTimer = 0f;
        currentPatrolTarget = GetRandomPatrolPoint();

        // 初始化攻击判定框
        if (attackColliderObject != null)
        {
            attackTrigger = attackColliderObject.GetComponent<FlyingEyeAttackTrigger>();
            if (attackTrigger == null)
            {
                Debug.LogError("攻击判定框上未挂载FlyingEyeAttackTrigger脚本！");
            }
            else
            {
                attackTrigger.OnAttackHit += OnAttackHit;
                Debug.Log("[初始化] 成功订阅攻击事件");
            }
            attackColliderObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDying || currentState == EnemyState.Dead) return;

        CheckStateTransitions();
        UpdateAnimationParameters();

        if (!isInHurtAnimation)
        {
            switch (currentState)
            {
                case EnemyState.Patrol:
                    PatrolBehaviour();
                    break;
                case EnemyState.Chase:
                    // 追击行为无额外逻辑，仅在FixedUpdate处理移动
                    break;
                case EnemyState.Attack:
                    AttackBehaviour();
                    break;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDying || currentState == EnemyState.Dead || isInHurtAnimation)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (currentState == EnemyState.Patrol)
            PatrolMovement();
        else if (currentState == EnemyState.Chase)
            ChaseMovement();
    }

    #region 动画参数更新
    private void UpdateAnimationParameters()
    {
        if (isDying) return;

        animator.SetBool("isPatrolling", currentState == EnemyState.Patrol);
        animator.SetBool("isChasing", currentState == EnemyState.Chase);
        animator.SetBool("isAttacking", currentState == EnemyState.Attack);
    }
    #endregion

    #region 巡逻逻辑
    private void PatrolBehaviour()
    {
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= Random.Range(minPatrolWaitTime, maxPatrolWaitTime))
            {
                currentPatrolTarget = GetRandomPatrolPoint();
                isWaitingAtPatrolPoint = false;
                patrolWaitTimer = 0f;
            }
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget);
        if (distanceToTarget <= 0.1f && !isWaitingAtPatrolPoint)
        {
            isWaitingAtPatrolPoint = true;
            rb.velocity = Vector2.zero;
        }
    }

    private void PatrolMovement()
    {
        if (isWaitingAtPatrolPoint) return;

        Vector2 moveDirection = (currentPatrolTarget - (Vector2)transform.position).normalized;
        rb.velocity = moveDirection * moveSpeed;
        FlipSprite(moveDirection.x);
    }

    private Vector2 GetRandomPatrolPoint()
    {
        Vector2 randomPoint;
        float minDistance = 2f;
        do
        {
            float randomX = patrolStartPos.x + Random.Range(-patrolAreaSize.x / 2, patrolAreaSize.x / 2);
            float randomY = patrolStartPos.y + Random.Range(-patrolAreaSize.y / 2, patrolAreaSize.y / 2);
            randomPoint = new Vector2(randomX, randomY);
        } while (Vector2.Distance(transform.position, randomPoint) < minDistance);

        return randomPoint;
    }
    #endregion

    #region 追击逻辑
    private void ChaseMovement()
    {
        if (target == null) return;

        Vector2 moveDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
        rb.velocity = moveDirection * moveSpeed * 1.5f;
        FlipSprite(moveDirection.x);
    }
    #endregion

    #region 攻击逻辑
    private void AttackBehaviour()
    {
        if (target == null) return;

        rb.velocity = Vector2.zero;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            Debug.Log($"执行攻击，攻击冷却已重置");
            lastAttackTime = Time.time;
        }
    }

    public void EnableAttackCollider()
    {
        if (isDying || currentState == EnemyState.Dead) return;
        if (attackColliderObject != null && currentState == EnemyState.Attack)
        {
            // 开始新攻击周期，清空记录
            damagedTargets.Clear();

            attackColliderObject.SetActive(true);
            Debug.Log($"[{Time.frameCount}] 攻击判定框已激活");
        }
    }

    public void DisableAttackCollider()
    {
        if (isDying) return;
        if (attackColliderObject != null)
        {
            attackColliderObject.SetActive(false);
            Debug.Log($"[{Time.frameCount}] 攻击判定框已禁用");
        }
    }

    /// <summary>
    /// 核心：调用PlayerController.TakeDamage()造成伤害
    /// </summary>
    private void OnAttackHit(Collider2D hitTarget)
    {
        // 同一攻击周期内，同一目标只受伤一次
        if (damagedTargets.Contains(hitTarget)) return;

        Debug.Log($"[事件回调] OnAttackHit 触发，目标: {hitTarget.name}");

        if (isDying || currentState != EnemyState.Attack) return;

        // 关键：获取PlayerController组件，调用其TakeDamage方法
        PlayerController playerController = hitTarget.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // 记录目标，防止重复伤害
            damagedTargets.Add(hitTarget);

            // 调用玩家的TakeDamage方法，触发受击逻辑
            playerController.TakeDamage(attackDamage);

            Debug.Log($"<color=red>【玩家受到伤害】尝试造成 {attackDamage} 点伤害</color>");
        }
        else
        {
            Debug.LogWarning($"[警告] 目标 {hitTarget.name} 没有PlayerController组件！");
        }
    }
    #endregion

    #region 受击与死亡
    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (isDying || isInHurtAnimation) return;

        currentHealth -= damage;
        Debug.Log($"敌人受击，剩余生命值：{currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        isInHurtAnimation = true;
        animator.SetTrigger("hurtTrigger");

        if (hitDirection != Vector2.zero)
        {
            currentState = EnemyState.Hurt;
            rb.velocity = Vector2.zero;
            rb.AddForce(hitDirection.normalized * hurtForce, ForceMode2D.Impulse);
        }

        StartCoroutine(UnlockAfterAnimation());
    }

    private IEnumerator UnlockAfterAnimation()
    {
        yield return new WaitForSeconds(hurtDuration);
        isInHurtAnimation = false;

        if (!isDying && currentHealth > 0)
        {
            currentState = EnemyState.Patrol;
        }
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        Debug.Log($"[死亡] 开始执行，血量: {currentHealth}");

        currentState = EnemyState.Dead;
        rb.velocity = Vector2.zero;

        GetComponent<Collider2D>().enabled = false;
        rb.isKinematic = true;

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger("dieTrigger");
        }

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject, deathDestroyDelay);
        Debug.Log("敌人死亡");
    }
    #endregion

    #region 状态检测
    private void CheckStateTransitions()
    {
        if (isDying) return;

        if (currentHealth <= 0 && currentState != EnemyState.Dead)
        {
            Die();
            return;
        }

        if (isInHurtAnimation) return;

        bool isPlayerInDetect = IsPlayerInDetectRange();
        bool isPlayerInAttack = isPlayerInDetect && IsPlayerInAttackRange();

        if (isPlayerInAttack)
        {
            if (currentState != EnemyState.Attack)
            {
                lastAttackTime = Time.time - attackCooldown;
                rb.velocity = Vector2.zero;
                Debug.Log($"[{Time.frameCount}] 进入攻击范围，立即切换到攻击状态");
            }
            currentState = EnemyState.Attack;
        }
        else if (isPlayerInDetect)
        {
            if (currentState == EnemyState.Attack)
            {
                animator.SetBool("isAttacking", false);
            }
            currentState = EnemyState.Chase;
        }
        else
        {
            if (currentState == EnemyState.Attack)
            {
                animator.SetBool("isAttacking", false);
            }
            target = null;
            currentState = EnemyState.Patrol;
        }
    }

    private bool IsPlayerInDetectRange()
    {
        if (isDying) return false;

        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(detectCenter.position, detectRadius, targetLayer);
        foreach (var collider in collidersInRange)
        {
            RaycastHit2D hit = Physics2D.Linecast(detectCenter.position, collider.transform.position, obstacleLayer);
            if (!hit)
            {
                target = collider.transform;
                return true;
            }
        }
        target = null;
        return false;
    }

    private bool IsPlayerInAttackRange()
    {
        if (target == null) return false;
        return Vector2.Distance(transform.position, target.position) <= attackRange;
    }

    private void FlipSprite(float moveDirectionX)
    {
        if (moveDirectionX != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * Mathf.Sign(moveDirectionX),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(patrolStartPos, patrolAreaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentPatrolTarget, 0.2f);

        if (detectCenter != null && !isDying)
        {
            Gizmos.color = IsPlayerInDetectRange() ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(detectCenter.position, detectRadius);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion

    private void OnDestroy()
    {
        if (attackTrigger != null)
        {
            attackTrigger.OnAttackHit -= OnAttackHit;
        }
    }
}
using System.Collections;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;
using OnePulse.Player;

namespace OnePulse.Entities
{
    public enum BullState
    {
        Idle,
        Alert,
        Charging,
        Stunned
    }

    /// <summary>
    /// 滚石蛮牛 (Battering Ram Creature)
    /// 与玩家眼神接触（直线无遮挡）即发怒冲锋。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Creature_BatteringRam : MonoBehaviour, IShareDamageable, IPushable, ISwappable
    {
        [Header("外观与切片")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite angrySprite;
        [SerializeField] private GameObject exclamationIndicator;

        [Header("AI 视线与索敌")]
        [Tooltip("索敌视线距离（米/格数）")]
        [SerializeField] private float sightDistance = 7f;
        [Tooltip("是否向下侦测 (Level 2 守门牛核心朝向)")]
        [SerializeField] private bool lookDown = true;
        [Tooltip("是否向上侦测")]
        [SerializeField] private bool lookUp = false;
        [Tooltip("是否向左侦测")]
        [SerializeField] private bool lookLeft = false;
        [Tooltip("是否向右侦测")]
        [SerializeField] private bool lookRight = false;

        [Header("冲锋与手感参数")]
        [SerializeField] private float chargeSpeed = 11f;
        [Tooltip("发现玩家后的蓄力预警时间（秒），给玩家留出闪避反应时间")]
        [SerializeField] private float windupDelay = 0.65f;
        [Tooltip("撞击硬直/眩晕冷却时间（秒），撞墙或撞击后原地晕头转向")]
        [SerializeField] private float stunDuration = 3.0f;
        [Tooltip("开局安全发呆缓冲期（秒），防止一进场景瞬间猝死")]
        [SerializeField] private float spawnGracePeriod = 1.0f;
        [SerializeField] private LayerMask visionBlockLayer;
        [SerializeField] private LayerMask hitLayers;

        [Header("反噬伤害（扣除全局世界脉搏）")]
        [SerializeField] private float wallImpactDamage = 30f;
        [SerializeField] private float blockImpactDamage = 20f;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private JuiceSquash _squashJuice;
        private BullState _state = BullState.Idle;
        private Vector2 _chargeDir = Vector2.zero;
        private Coroutine _stateRoutine;
        private float _graceTimer = 0f;

        public BullState CurrentState => _state;
        public bool IsPushing => false;
        public bool CanSwap => true;
        public Transform Transform => transform;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
                _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                _rb.bodyType = RigidbodyType2D.Kinematic; // 设为 Kinematic，石牛固若金汤，绝不被外力推移漂移
            }

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _squashJuice = GetComponentInChildren<JuiceSquash>();

            if (visionBlockLayer.value == 0) visionBlockLayer = ~0;
            if (hitLayers.value == 0) hitLayers = ~0;

            if (exclamationIndicator != null)
            {
                exclamationIndicator.SetActive(false);
            }

            SetVisualState(BullState.Idle);
        }

        private void OnEnable()
        {
            _graceTimer = 0f;
            _state = BullState.Idle;
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
            SetVisualState(BullState.Idle);
        }

        public void Initialize(Sprite idle, Sprite angry, GameObject indicator)
        {
            idleSprite = idle;
            angrySprite = angry;
            exclamationIndicator = indicator;
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_squashJuice == null) _squashJuice = GetComponentInChildren<JuiceSquash>();
            if (visionBlockLayer.value == 0) visionBlockLayer = ~0;
            if (hitLayers.value == 0) hitLayers = ~0;
            SetVisualState(BullState.Idle);
        }

        private void Update()
        {
            if (WorldHealthManager.IsDepleted) return;

            // 若非游玩状态（如进入传送门关卡突破、游戏结束），彻底停止 AI 行为并关闭警报
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                if (exclamationIndicator != null) exclamationIndicator.SetActive(false);
                return;
            }

            // 开局安全缓冲期：给玩家看清场景与反应的窗口
            if (_graceTimer < spawnGracePeriod)
            {
                _graceTimer += Time.deltaTime;
                return;
            }

            if (_state == BullState.Idle)
            {
                CheckLineOfSightForPlayer();
            }
            else if (_state == BullState.Charging)
            {
                ExecuteChargeMovement();
            }
        }

        private void FixedUpdate()
        {
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
        }

        private void CheckLineOfSightForPlayer()
        {
            Vector2 castSize = new Vector2(0.85f, 0.85f);

            if (lookDown) CheckDirection(Vector2.down, castSize);
            if (_state != BullState.Idle) return;

            if (lookUp) CheckDirection(Vector2.up, castSize);
            if (_state != BullState.Idle) return;

            if (lookLeft) CheckDirection(Vector2.left, castSize);
            if (_state != BullState.Idle) return;

            if (lookRight) CheckDirection(Vector2.right, castSize);
        }

        private void CheckDirection(Vector2 dir, Vector2 castSize)
        {
            RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, castSize, 0f, dir, sightDistance, visionBlockLayer);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.isTrigger) continue;

                PlayerController2D player = hit.collider.GetComponentInParent<PlayerController2D>();
                if (player != null && player.isActiveAndEnabled)
                {
                    TriggerCharge(dir);
                    return;
                }

                // 遇到实心障碍挡住视线
                break;
            }
        }

        public void TriggerCharge(Vector2 direction)
        {
            if (_state != BullState.Idle) return;

            _chargeDir = direction.normalized;
            if (_stateRoutine != null) StopCoroutine(_stateRoutine);
            _stateRoutine = StartCoroutine(ChargeWindupRoutine());
        }

        private IEnumerator ChargeWindupRoutine()
        {
            _state = BullState.Alert;
            SetVisualState(BullState.Alert);

            if (_spriteRenderer != null && _chargeDir.x != 0)
            {
                _spriteRenderer.flipX = _chargeDir.x < 0;
            }

            _squashJuice?.PlaySquash(0.8f, 1.25f, windupDelay);
            CameraShake.Trigger(0.12f, windupDelay);

            float timer = 0f;
            while (timer < windupDelay)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                {
                    if (exclamationIndicator != null) exclamationIndicator.SetActive(false);
                    yield break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            if (exclamationIndicator != null) exclamationIndicator.SetActive(false);
            _state = BullState.Charging;
            SetVisualState(BullState.Charging);
        }

        private void ExecuteChargeMovement()
        {
            float stepDistance = chargeSpeed * Time.deltaTime;
            RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, new Vector2(0.9f, 0.9f), 0f, _chargeDir, stepDistance + 0.35f, hitLayers);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.isTrigger) continue;

                HandleImpact(hit.collider);
                return;
            }

            transform.position += (Vector3)(_chargeDir * stepDistance);
        }

        private void HandleImpact(Collider2D collider)
        {
            CameraShake.Trigger(0.4f, 0.25f);
            _squashJuice?.PlaySquash(1.35f, 0.7f, 0.2f);
            OnePulse.Audio.ProceduralAudioManager.PlayImpact();

            // 1. 撞击水晶碎裂墙
            BreakableWall breakable = collider.GetComponentInParent<BreakableWall>();
            if (breakable != null)
            {
                breakable.Break();
                WorldHealthManager.ApplyDamage(wallImpactDamage, transform.position);
                EnterStunState();
                return;
            }

            // 2. 撞击可推箱子
            Block_Pushable pushableBlock = collider.GetComponentInParent<Block_Pushable>();
            if (pushableBlock != null)
            {
                WorldHealthManager.ApplyDamage(blockImpactDamage, transform.position);
                pushableBlock.LaunchInDirection(_chargeDir);
                EnterStunState();
                return;
            }

            // 3. 撞击治愈花朵
            Plant_HealingFlower flower = collider.GetComponentInParent<Plant_HealingFlower>();
            if (flower != null)
            {
                if (flower.SacredProtection)
                {
                    // 圣殿关卡：神圣花不可被冲毁，蛮牛撞击受阻进入硬直眩晕
                    EnterStunState();
                    return;
                }
                flower.Consume();
                return;
            }

            // 4. 撞击炸药桶
            Hazard_Bomb bomb = collider.GetComponentInParent<Hazard_Bomb>();
            if (bomb != null)
            {
                bomb.Detonate();
                EnterStunState();
                return;
            }

            // 5. 撞击玩家
            PlayerController2D player = collider.GetComponentInParent<PlayerController2D>();
            if (player != null)
            {
                player.TakeDamage(30f);
                EnterStunState();
                return;
            }

            // 6. 撞击不可摧毁墙体
            EnterStunState();
        }

        private void EnterStunState()
        {
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            _state = BullState.Stunned;
            SetVisualState(BullState.Stunned);
            if (_stateRoutine != null) StopCoroutine(_stateRoutine);
            _stateRoutine = StartCoroutine(ReboundAndStunRoutine());
        }

        private IEnumerator ReboundAndStunRoutine()
        {
            // 1. 撞击受挫反弹：向冲锋反方向微退
            Vector2 reboundDir = -_chargeDir;
            float reboundDistance = 0.45f;
            float reboundDuration = 0.14f;
            float reboundElapsed = 0f;

            // 检测反方向是否有阻挡，防止后退穿入后方墙体
            RaycastHit2D hitBehind = Physics2D.Raycast(transform.position, reboundDir, reboundDistance, visionBlockLayer);
            if (hitBehind.collider != null && !hitBehind.collider.isTrigger && hitBehind.collider.transform != transform)
            {
                reboundDistance = Mathf.Max(0f, hitBehind.distance - 0.15f);
            }

            Vector3 startPos = transform.position;
            Vector3 targetReboundPos = startPos + (Vector3)(reboundDir * reboundDistance);

            while (reboundElapsed < reboundDuration)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                {
                    yield break;
                }

                reboundElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(reboundElapsed / reboundDuration);
                // 缓动曲线：平滑反弹刹停
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(startPos, targetReboundPos, smoothT);
                if (_rb != null) _rb.velocity = Vector2.zero;

                yield return null;
            }

            transform.position = targetReboundPos;
            if (_rb != null) _rb.velocity = Vector2.zero;

            // 2. 原地眩晕硬直
            float elapsed = 0f;
            while (elapsed < stunDuration)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                if (_rb != null) _rb.velocity = Vector2.zero;

                // 眩晕微晃头
                if (_spriteRenderer != null)
                {
                    float angle = Mathf.Sin(elapsed * 16f) * 4f;
                    _spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }

                yield return null;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.transform.localRotation = Quaternion.identity;
            }

            _state = BullState.Idle;
            SetVisualState(BullState.Idle);
        }

        private void SetVisualState(BullState state)
        {
            if (_spriteRenderer == null) return;

            switch (state)
            {
                case BullState.Idle:
                case BullState.Stunned:
                    if (idleSprite != null) _spriteRenderer.sprite = idleSprite;
                    if (exclamationIndicator != null) exclamationIndicator.SetActive(false);
                    break;
                case BullState.Alert:
                    if (angrySprite != null) _spriteRenderer.sprite = angrySprite;
                    if (exclamationIndicator != null) exclamationIndicator.SetActive(true);
                    break;
                case BullState.Charging:
                    if (angrySprite != null) _spriteRenderer.sprite = angrySprite;
                    if (exclamationIndicator != null) exclamationIndicator.SetActive(false);
                    break;
            }
        }

        #region IPushable 实现
        public bool TryPush(Vector2 direction, float force = 1f)
        {
            // 石牛如山岳般沉重，不可被主角肉身推移
            return false;
        }
        #endregion

        #region ISwappable 实现
        public void OnSwapped(Vector3 newPosition, Vector3 oldPosition)
        {
            transform.position = newPosition;
            _squashJuice?.PlayPop(1.3f, 0.2f);
        }
        #endregion

        #region IShareDamageable 实现
        public void TakeDamage(float amount)
        {
            WorldHealthManager.ApplyDamage(amount, transform.position);
            _squashJuice?.PlaySquash(1.3f, 0.7f, 0.2f);
        }

        public void Heal(float amount)
        {
            WorldHealthManager.ApplyHeal(amount, transform.position);
            _squashJuice?.PlayPop(1.2f, 0.2f);
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.7f);
            Vector3 p = transform.position;
            if (lookDown) Gizmos.DrawRay(p, Vector3.down * sightDistance);
            if (lookUp) Gizmos.DrawRay(p, Vector3.up * sightDistance);
            if (lookLeft) Gizmos.DrawRay(p, Vector3.left * sightDistance);
            if (lookRight) Gizmos.DrawRay(p, Vector3.right * sightDistance);
        }
    }
}

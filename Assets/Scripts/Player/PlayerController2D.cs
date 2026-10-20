using System.Collections;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Entities;
using OnePulse.Juice;
using OnePulse.Level;

namespace OnePulse.Player
{
    /// <summary>
    /// 玩家2D角色控制器
    /// 正交4方向灵敏微操移动、松手即停（零溜冰）、推物判定与生命共享
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController2D : MonoBehaviour, IShareDamageable, ISwappable
    {
        [Header("移动手感设置")]
        [Tooltip("移动速度（推荐 2.4 ~ 2.8）")]
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private LayerMask pushableLayer;

        [Header("推箱设定")]
        [Tooltip("推箱子判定距离")]
        [SerializeField] private float pushRaycastDistance = 0.55f;
        [Tooltip("顶住箱子多久后触发推移（秒）")]
        [SerializeField] private float pushDelay = 0.12f;

        [Header("视觉与动画")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private JuiceSquash squashJuice;

        private Rigidbody2D _rb;
        private Collider2D _myCollider;
        private Vector2 _currentMoveDir = Vector2.zero;
        private Vector2 _lastFacingDir = Vector2.down;
        private bool _preferHorizontal = true;
        private float _pushTimer = 0f;
        private float _stepJuiceTimer = 0f;

        public bool IsMoving => _currentMoveDir != Vector2.zero;
        public Vector2 LastFacingDirection => _lastFacingDir;

        // ISwappable接口实现（静止或未正在推物时即可换位）
        public bool CanSwap => _currentMoveDir == Vector2.zero;
        public Transform Transform => transform;

        private void Awake()
        {
            _myCollider = GetComponent<Collider2D>();
            _rb = GetComponent<Rigidbody2D>();

            // 确保物理Rigidbody2D参数适配顶视角正交手感
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody2D>();
            }
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (squashJuice == null) squashJuice = GetComponentInChildren<JuiceSquash>();

            if ((obstacleLayer.value | pushableLayer.value) == 0)
            {
                obstacleLayer = ~0;
            }

            // 平滑兼容旧 Prefab 的过快速度 (3.8 -> 2.6)
            if (moveSpeed >= 3.7f)
            {
                moveSpeed = 2.6f;
            }

            // 优化为2.5D脚底受力圈
            if (_myCollider is CircleCollider2D circle && (circle.radius > 0.35f || circle.offset.y > -0.2f))
            {
                circle.offset = new Vector2(0f, -0.32f);
                circle.radius = 0.28f;
            }
        }

        private void OnDisable()
        {
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
            }
            _currentMoveDir = Vector2.zero;
        }

        private void Update()
        {
            if (WorldHealthManager.IsDepleted)
            {
                HaltMovement();
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                HaltMovement();
                return;
            }

            HandleInput();
            HandleWalkingJuice();
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                HaltMovement();
                return;
            }

            if (_currentMoveDir != Vector2.zero)
            {
                // 正交速度驱动移动
                _rb.velocity = _currentMoveDir * moveSpeed;

                // 检测是否正面对推箱物体
                CheckPushInteraction(_currentMoveDir);
            }
            else
            {
                // 松手即刹停：清零速度，绝不滑行
                _rb.velocity = Vector2.zero;
                _pushTimer = 0f;
            }

            // 边缘安全双重防护：确保主角绝不穿出石板地面
            ClampToFloorBounds();
        }

        private void ClampToFloorBounds()
        {
            if (FloorBounds.AllFloors.Count == 0) return;

            FloorBounds targetFloor = null;
            float minDistanceSqr = float.MaxValue;
            Vector2 pos = transform.position;

            foreach (var floor in FloorBounds.AllFloors)
            {
                if (floor == null || !floor.isActiveAndEnabled) continue;
                Bounds b = floor.WorldBounds;

                // 如果玩家就在该石板范围内，直接选用该石板
                if (pos.x >= b.min.x - 0.2f && pos.x <= b.max.x + 0.2f &&
                    pos.y >= b.min.y - 0.2f && pos.y <= b.max.y + 0.2f)
                {
                    targetFloor = floor;
                    break;
                }

                // 否则寻找离玩家最近的石板
                float distSqr = (b.ClosestPoint(pos) - (Vector3)pos).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    targetFloor = floor;
                }
            }

            if (targetFloor != null)
            {
                Bounds b = targetFloor.WorldBounds;
                float clampedX = transform.position.x;
                float clampedY = transform.position.y;

                if (targetFloor.BlockTop && clampedY > b.max.y)
                {
                    clampedY = b.max.y;
                    if (_currentMoveDir.y > 0) _rb.velocity = new Vector2(_rb.velocity.x, 0f);
                }
                else if (targetFloor.BlockBottom && clampedY < b.min.y)
                {
                    clampedY = b.min.y;
                    if (_currentMoveDir.y < 0) _rb.velocity = new Vector2(_rb.velocity.x, 0f);
                }

                if (targetFloor.BlockLeft && clampedX < b.min.x)
                {
                    clampedX = b.min.x;
                    if (_currentMoveDir.x < 0) _rb.velocity = new Vector2(0f, _rb.velocity.y);
                }
                else if (targetFloor.BlockRight && clampedX > b.max.x)
                {
                    clampedX = b.max.x;
                    if (_currentMoveDir.x > 0) _rb.velocity = new Vector2(0f, _rb.velocity.y);
                }

                if (transform.position.y != clampedY || transform.position.x != clampedX)
                {
                    _rb.position = new Vector2(clampedX, clampedY);
                }
            }
        }

        /// <summary>
        /// 输入监听：保持正交4方向，最新输入键优先响应，松键就停
        /// </summary>
        private void HandleInput()
        {
            // 检测最新按下的方向轴，决定当前轴优先级
            if (Input.GetButtonDown("Horizontal") || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                _preferHorizontal = true;
            }
            else if (Input.GetButtonDown("Vertical") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                _preferHorizontal = false;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector2 inputDir = Vector2.zero;

            // 裁决只保留单一正交轴，严禁斜向 8 方向
            if (_preferHorizontal)
            {
                if (Mathf.Abs(horizontal) > 0.1f)
                {
                    inputDir = new Vector2(Mathf.Sign(horizontal), 0f);
                }
                else if (Mathf.Abs(vertical) > 0.1f)
                {
                    inputDir = new Vector2(0f, Mathf.Sign(vertical));
                }
            }
            else
            {
                if (Mathf.Abs(vertical) > 0.1f)
                {
                    inputDir = new Vector2(0f, Mathf.Sign(vertical));
                }
                else if (Mathf.Abs(horizontal) > 0.1f)
                {
                    inputDir = new Vector2(Mathf.Sign(horizontal), 0f);
                }
            }

            _currentMoveDir = inputDir;

            if (inputDir != Vector2.zero)
            {
                _lastFacingDir = inputDir;

                // 面向翻转
                if (spriteRenderer != null && inputDir.x != 0f)
                {
                    spriteRenderer.flipX = inputDir.x < 0f;
                }
            }
        }

        /// <summary>
        /// 正对箱子持续推移判定
        /// </summary>
        private void CheckPushInteraction(Vector2 dir)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, dir, pushRaycastDistance, obstacleLayer | pushableLayer);
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider == _myCollider || hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.collider.isTrigger) continue;

                IPushable pushable = hit.collider.GetComponentInParent<IPushable>();
                if (pushable != null && !pushable.IsPushing)
                {
                    _pushTimer += Time.fixedDeltaTime;
                    if (_pushTimer >= pushDelay)
                    {
                        _pushTimer = 0f;
                        if (pushable.TryPush(dir, 1f))
                        {
                            squashJuice?.PlaySquash(0.85f, 1.15f, 0.15f);
                        }
                    }
                    return;
                }
            }

            _pushTimer = 0f;
        }

        private void HandleWalkingJuice()
        {
            if (_currentMoveDir != Vector2.zero)
            {
                _stepJuiceTimer += Time.deltaTime;
                if (_stepJuiceTimer >= 0.22f)
                {
                    _stepJuiceTimer = 0f;
                    squashJuice?.PlaySquash(0.94f, 1.06f, 0.1f);
                }
            }
            else
            {
                _stepJuiceTimer = 0f;
            }
        }

        private void HaltMovement()
        {
            _currentMoveDir = Vector2.zero;
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
            }
        }

        #region IShareDamageable 实现
        public void TakeDamage(float amount)
        {
            WorldHealthManager.ApplyDamage(amount, transform.position);
            squashJuice?.PlaySquash(1.25f, 0.75f, 0.2f);
        }

        public void Heal(float amount)
        {
            WorldHealthManager.ApplyHeal(amount, transform.position);
            squashJuice?.PlayPop(1.25f, 0.2f);
        }
        #endregion

        #region ISwappable 实现
        public void OnSwapped(Vector3 newPosition, Vector3 oldPosition)
        {
            HaltMovement();
            transform.position = newPosition;
            if (_rb != null)
            {
                _rb.position = newPosition;
                _rb.velocity = Vector2.zero;
            }
            squashJuice?.PlayPop(1.3f, 0.2f);
        }
        #endregion

        public void TeleportTo(Vector2 position)
        {
            HaltMovement();
            transform.position = position;
            if (_rb != null)
            {
                _rb.position = position;
                _rb.velocity = Vector2.zero;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + _lastFacingDir * pushRaycastDistance);
        }
    }
}

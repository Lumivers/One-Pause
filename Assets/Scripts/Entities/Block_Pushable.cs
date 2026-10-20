using System.Collections;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;

namespace OnePulse.Entities
{
    /// <summary>
    /// 推力碎石箱 / 铁箱 (Pushable Block)
    /// 可被玩家单格轻推；若被暴躁石牛冲撞，会产生巨大冲量飞出，破除水晶墙或构筑防御线
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Block_Pushable : MonoBehaviour, IPushable, ISwappable
    {
        [Header("移动与物理设定")]
        [SerializeField] private float slideSpeed = 12f;
        [SerializeField] private LayerMask obstacleLayer;

        private JuiceSquash _squash;
        private bool _isSliding = false;
        private bool _isConsumed = false;

        public bool IsPushing => _isSliding;
        public bool IsConsumed => _isConsumed;
        public bool CanSwap => !_isSliding && !_isConsumed;
        public Transform Transform => transform;

        private void Awake()
        {
            _squash = GetComponentInChildren<JuiceSquash>();
            if (obstacleLayer.value == 0) obstacleLayer = ~0;
        }

        #region IPushable 实现
        public bool TryPush(Vector2 direction, float force = 1f)
        {
            if (_isSliding || _isConsumed) return false;

            Vector2 targetPos = (Vector2)transform.position + direction;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, 1f, obstacleLayer);

            bool blocked = false;
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform) || hit.collider.isTrigger) continue;
                blocked = true;
                break;
            }

            if (blocked)
            {
                // 前方有坚固实体阻挡，无法推动
                _squash?.PlaySquash(1.1f, 0.9f, 0.1f);
                return false;
            }

            transform.position = targetPos;
            _squash?.PlaySquash(0.85f, 1.15f, 0.15f);
            return true;
        }
        #endregion

        /// <summary>
        /// 压入地刺，献祭箱子化为平整安全石踏板
        /// </summary>
        public void CrushAndSettle()
        {
            if (_isConsumed) return;
            _isConsumed = true;
            _isSliding = false;

            // 1. 关闭碰撞与刚体阻挡，允许主角踩踏通行
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
                rb.simulated = false;
            }

            // 2. 视觉沉降：降低排序层级至地面层，颜色微调为稳固石砖地
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 2; // 主角为 10，地砖为 0，垫石为 2
                sr.color = new Color(0.72f, 0.75f, 0.78f, 0.95f);
            }

            // 3. 压扁顿挫与屏幕微震
            _squash?.PlaySquash(1.35f, 0.65f, 0.25f);
            CameraShake.Trigger(0.28f, 0.2f);
            Debug.Log("<color=#77FFAA><b>[推箱压刺] 箱子已嵌平地刺，化作安全通道！</b></color>");
        }

        /// <summary>
        /// 被蛮牛冲撞发射，沿方向滑行直至撞墙或击碎水晶
        /// </summary>
        public void LaunchInDirection(Vector2 direction)
        {
            if (_isSliding) return;
            StartCoroutine(SlideRoutine(direction.normalized));
        }

        private IEnumerator SlideRoutine(Vector2 direction)
        {
            _isSliding = true;
            _squash?.PlaySquash(0.7f, 1.3f, 0.2f);

            while (_isSliding)
            {
                float step = slideSpeed * Time.deltaTime;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, step + 0.3f, obstacleLayer);

                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    // 撞击到物体
                    BreakableWall breakable = hit.collider.GetComponentInParent<BreakableWall>();
                    if (breakable != null)
                    {
                        breakable.Break();
                    }

                    Plant_HealingFlower flower = hit.collider.GetComponentInParent<Plant_HealingFlower>();
                    if (flower != null)
                    {
                        flower.Consume();
                    }

                    CameraShake.Trigger(0.3f, 0.2f);
                    _squash?.PlaySquash(1.3f, 0.7f, 0.18f);
                    _isSliding = false;
                    yield break;
                }

                transform.position += (Vector3)(direction * step);
                yield return null;
            }

            _isSliding = false;
        }

        #region ISwappable 实现
        public void OnSwapped(Vector3 newPosition, Vector3 oldPosition)
        {
            transform.position = newPosition;
            _squash?.PlayPop(1.3f, 0.2f);
        }
        #endregion
    }
}

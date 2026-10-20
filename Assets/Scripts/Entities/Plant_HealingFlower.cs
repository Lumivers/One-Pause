using System.Collections;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;
using OnePulse.Player;

namespace OnePulse.Entities
{
    /// <summary>
    /// 发光治愈蓝草 (Healing Flower)
    /// 被主角、蛮牛或飞行的箱子触碰后，汲取其生命精华，为世界脉搏补充 30% 生命！
    /// 同时也是可被轻推与换位的战术垫脚石。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Plant_HealingFlower : MonoBehaviour, IPushable, ISwappable
    {
        [Header("治愈数值")]
        [SerializeField] private float healAmount = 30f;
        [SerializeField] private LayerMask obstacleLayer;

        private SpriteRenderer _renderer;
        private JuiceSquash _squash;
        private Collider2D _col;
        private bool _isConsumed = false;

        [Header("终章圣殿模式 (Level 6)")]
        [Tooltip("勾选后花朵成为神圣生灵，不可被玩家或蛮牛触碰吞噬，只能被推/换位并送入圣坛光圈吸收")]
        [SerializeField] private bool sacredProtection = false;

        public bool SacredProtection => sacredProtection;
        public bool IsConsumed => _isConsumed;
        public bool IsPushing => false;
        public bool CanSwap => !_isConsumed;
        public Transform Transform => transform;

        private void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _squash = GetComponentInChildren<JuiceSquash>();
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
            if (obstacleLayer.value == 0) obstacleLayer = ~0;

            // 若本关存在圣殿祭坛，自动开启圣灵保护
            if (FindObjectOfType<SanctuaryAltar>() != null)
            {
                sacredProtection = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isConsumed) return;

            // 终章圣灵模式下，免疫玩家与触碰消耗
            if (sacredProtection) return;

            // 如果被玩家触碰
            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player != null)
            {
                Consume();
            }
        }

        public void Consume()
        {
            if (_isConsumed) return;
            _isConsumed = true;

            WorldHealthManager.ApplyHeal(healAmount, transform.position);
            CameraShake.Trigger(0.15f, 0.15f);

            StartCoroutine(ConsumeRoutine());
        }

        private IEnumerator ConsumeRoutine()
        {
            _squash?.PlayPop(1.4f, 0.2f);

            // 淡出效果
            if (_renderer != null)
            {
                Color startColor = _renderer.color;
                float elapsed = 0f;
                float duration = 0.25f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                    _renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                    yield return null;
                }
            }

            gameObject.SetActive(false);
        }

        #region IPushable 实现
        public bool TryPush(Vector2 direction, float force = 1f)
        {
            if (_isConsumed) return false;

            Vector2 targetPos = (Vector2)transform.position + direction;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1f, obstacleLayer);
            if (hit.collider != null) return false;

            transform.position = targetPos;
            _squash?.PlaySquash(0.85f, 1.15f, 0.15f);
            return true;
        }
        #endregion

        #region ISwappable 实现
        public void OnSwapped(Vector3 newPosition, Vector3 oldPosition)
        {
            transform.position = newPosition;
            _squash?.PlayPop(1.3f, 0.2f);
        }
        #endregion
    }
}

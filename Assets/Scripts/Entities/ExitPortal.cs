using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;
using OnePulse.Player;

namespace OnePulse.Entities
{
    /// <summary>
    /// 关卡传送漩涡门 (Exit Portal)
    /// 关卡终点出口，玩家踏入后触发通关逻辑。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitPortal : MonoBehaviour
    {
        [Header("解谜门禁设置")]
        [Tooltip("若大门周围存在尚未撞碎的水晶墙，保持封印状态，禁止直接通关")]
        [SerializeField] private bool requireNearbyCrystalsBroken = true;
        [Tooltip("搜索阻挡水晶墙的感应半径（米）")]
        [SerializeField] private float crystalCheckRadius = 2.5f;

        private JuiceSquash _squash;
        private bool _isTriggered = false;
        private float _lastSealWarnTime = -99f;

        private void Awake()
        {
            _squash = GetComponentInChildren<JuiceSquash>();
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
            if (col is BoxCollider2D box)
            {
                // 紧密聚焦于石门内部的门槛/漩涡中心（居中偏下 -0.22m），上沿绝不向上探出误吸
                box.size = new Vector2(0.85f, 0.7f);
                box.offset = new Vector2(0f, -0.22f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryTriggerPortal(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryTriggerPortal(other);
        }

        private void TryTriggerPortal(Collider2D other)
        {
            if (_isTriggered) return;

            PlayerController2D player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;

            // 1. 核心解谜判定：若附近有尚未被石牛撞破的水晶墙，门处于封印锁定状态
            if (IsSealed())
            {
                if (Time.time - _lastSealWarnTime > 1.2f)
                {
                    _lastSealWarnTime = Time.time;
                    _squash?.PlaySquash(1.15f, 0.85f, 0.15f);
                    CameraShake.Trigger(0.12f, 0.1f);
                    Debug.Log("<color=#FF77AA><b>[传送门封印中]</b> 通道被水晶阻挡封印！请诱导蛮牛撞碎水晶后再踏入门内！</color>");
                }
                return;
            }

            // 2. 水晶已破，通道畅通，触发胜利传送！
            _isTriggered = true;

            // 立即关闭主角的碰撞体与操作组件，防止吸入结算过程中继续受击或被怪物索敌
            Collider2D[] colliders = player.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
            player.enabled = false;

            _squash?.PlayPop(1.4f, 0.25f);
            CameraShake.Trigger(0.2f, 0.2f);

            StartCoroutine(AbsorbPlayerCoroutine(player.transform));

            Debug.Log("<color=#55FF55><b>[踏入传送门]</b> 关卡胜利！</color>");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }

        /// <summary>
        /// 检测门前或周围是否存在未被撞破的水晶阻挡
        /// </summary>
        public bool IsSealed()
        {
            if (!requireNearbyCrystalsBroken) return false;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, crystalCheckRadius);
            foreach (var col in colliders)
            {
                if (col == null) continue;
                BreakableWall wall = col.GetComponentInParent<BreakableWall>();
                if (wall != null && !wall.IsBroken)
                {
                    return true;
                }
            }
            return false;
        }

        private System.Collections.IEnumerator AbsorbPlayerCoroutine(Transform playerTransform)
        {
            if (playerTransform == null) yield break;
            Vector3 startPos = playerTransform.position;
            Vector3 targetPos = transform.position;
            Vector3 startScale = playerTransform.localScale;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (playerTransform != null)
                {
                    playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
                    playerTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                }
                yield return null;
            }

            if (playerTransform != null)
            {
                playerTransform.gameObject.SetActive(false);
            }
        }
    }
}

using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;
using OnePulse.Player;

namespace OnePulse.Entities
{
    /// <summary>
    /// 伤害红水晶刺 (Red Crystal Spike Hazard)
    /// 踩踏或触碰造成 30% 扣血。
    /// 在特定关卡中是逆向控血的工具
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard_Spike : MonoBehaviour
    {
        [Header("伤害设置")]
        [SerializeField] private float damageAmount = 30f;
        [SerializeField] private float damageCooldown = 0.8f;

        private float _lastDamageTime = -99f;
        private JuiceSquash _squash;
        private bool _isCrushed = false;

        private void Awake()
        {
            _squash = GetComponentInChildren<JuiceSquash>();
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckSpikeInteraction(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckSpikeInteraction(other.gameObject);
        }

        private void CheckSpikeInteraction(GameObject target)
        {
            if (_isCrushed) return;

            // 1. 优先检测推力石箱：触发【一换一压刺垫道】奇迹
            Block_Pushable pushableBlock = target.GetComponentInParent<Block_Pushable>();
            if (pushableBlock != null && !pushableBlock.IsConsumed)
            {
                CrushSpike(pushableBlock);
                return;
            }

            // 2. 玩家或生物踩踏：造成正常脉搏伤害
            if (Time.time - _lastDamageTime < damageCooldown) return;

            IShareDamageable damageable = target.GetComponentInParent<IShareDamageable>();
            if (damageable != null)
            {
                _lastDamageTime = Time.time;
                damageable.TakeDamage(damageAmount);
                _squash?.PlaySquash(1.2f, 0.8f, 0.15f);
                CameraShake.Trigger(0.25f, 0.18f);
            }
        }

        private void CrushSpike(Block_Pushable block)
        {
            if (_isCrushed) return;
            _isCrushed = true;

            // 立即禁用刺的触发器，彻底杜绝误伤
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // 命令箱子就地下沉化为石阶
            block.CrushAndSettle();

            // 地刺压缩碎裂收起
            StartCoroutine(CrushSpikeRoutine());
        }

        private System.Collections.IEnumerator CrushSpikeRoutine()
        {
            _squash?.PlaySquash(0.3f, 0.2f, 0.18f);
            yield return new WaitForSeconds(0.18f);
            gameObject.SetActive(false);
        }
    }
}

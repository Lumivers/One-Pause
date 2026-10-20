using System.Collections;
using UnityEngine;
using OnePulse.Juice;

namespace OnePulse.Entities
{
    /// <summary>
    /// 水晶硬墙 (Breakable Crystal Wall)
    /// 玩家自身无法击破，唯有石牛冲撞或飞石箱可轰碎
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BreakableWall : MonoBehaviour
    {
        private Collider2D _col;
        private SpriteRenderer _renderer;
        private JuiceSquash _squash;
        private bool _isBroken = false;

        public bool IsBroken => _isBroken;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _squash = GetComponentInChildren<JuiceSquash>();

            if (_col is BoxCollider2D box)
            {
                // 降低高度至底座，边缘带圆角防卡死
                box.size = new Vector2(0.8f, 0.5f);
                box.offset = new Vector2(0f, -0.15f);
                box.edgeRadius = 0.05f;
            }
        }

        public void Break()
        {
            if (_isBroken) return;
            _isBroken = true;

            if (_col != null) _col.enabled = false;
            CameraShake.Trigger(0.4f, 0.25f);

            StartCoroutine(BreakAnimationRoutine());
        }

        private IEnumerator BreakAnimationRoutine()
        {
            _squash?.PlayPop(1.4f, 0.15f);

            if (_renderer != null)
            {
                Color startColor = _renderer.color;
                float elapsed = 0f;
                float duration = 0.2f;
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
    }
}

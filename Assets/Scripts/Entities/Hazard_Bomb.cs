using System.Collections;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;

namespace OnePulse.Entities
{
    /// <summary>
    /// 危险炸药桶 (Explosive Hazard)
    /// 受到撞击或触发展开倒计时，爆炸将对世界脉搏造成致命伤害。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard_Bomb : MonoBehaviour, IPushable, ISwappable
    {
        [Header("爆炸设定")]
        [SerializeField] private float explosionDamage = 100f;
        [SerializeField] private float fuseTime = 3f;
        [SerializeField] private bool autoFuseOnTouch = false;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite ignitedSprite;

        private SpriteRenderer _sr;
        private JuiceSquash _squash;
        private bool _isExploded = false;
        private bool _isFuseActive = false;

        public bool IsPushing => false;
        public bool CanSwap => !_isExploded;
        public Transform Transform => transform;

        private void Awake()
        {
            _squash = GetComponentInChildren<JuiceSquash>();
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null && normalSprite != null)
            {
                _sr.sprite = normalSprite;
            }
        }

        public void StartFuse()
        {
            if (_isFuseActive || _isExploded) return;
            if (_sr != null && ignitedSprite != null)
            {
                _sr.sprite = ignitedSprite;
            }
            StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            _isFuseActive = true;
            float elapsed = 0f;
            while (elapsed < fuseTime)
            {
                elapsed += 0.5f;
                _squash?.PlaySquash(1.2f, 0.8f, 0.15f);
                yield return new WaitForSeconds(0.5f);
            }

            Detonate();
        }

        public void Detonate()
        {
            if (_isExploded) return;
            _isExploded = true;

            WorldHealthManager.ApplyDamage(explosionDamage, transform.position);
            CameraShake.Trigger(0.6f, 0.35f);

            Debug.Log("<color=#FF0000><b>[炸药桶引爆！]</b></color>");
            gameObject.SetActive(false);
        }

        #region IPushable 实现
        public bool TryPush(Vector2 direction, float force = 1f)
        {
            if (_isExploded) return false;

            transform.position += (Vector3)direction;
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

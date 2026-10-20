using System.Collections;
using UnityEngine;
using OnePulse.Core;

namespace OnePulse.Juice
{
    /// <summary>
    /// 全局屏幕微震控制器
    /// 响应受击、撞击与爆炸
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float damageShakeIntensity = 0.25f;
        [SerializeField] private float damageShakeDuration = 0.2f;

        private Vector3 _initialPosition;
        private Coroutine _shakeRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            _initialPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            WorldHealthManager.OnDamageTaken += HandleDamageShake;
        }

        private void OnDisable()
        {
            WorldHealthManager.OnDamageTaken -= HandleDamageShake;
        }

        private void HandleDamageShake(float amount, Vector3 sourcePos)
        {
            float intensity = damageShakeIntensity * Mathf.Clamp(amount / 30f, 0.5f, 2f);
            Shake(intensity, damageShakeDuration);
        }

        public static void Trigger(float intensity = 0.2f, float duration = 0.15f)
        {
            if (Instance != null)
            {
                Instance.Shake(intensity, duration);
            }
        }

        public void Shake(float intensity, float duration)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(DoShake(intensity, duration));
        }

        private IEnumerator DoShake(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - (elapsed / duration);
                Vector2 randomOffset = Random.insideUnitCircle * (intensity * damp);
                transform.localPosition = _initialPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
                yield return null;
            }

            transform.localPosition = _initialPosition;
            _shakeRoutine = null;
        }
    }
}

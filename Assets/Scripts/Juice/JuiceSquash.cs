using System.Collections;
using UnityEngine;

namespace OnePulse.Juice
{
    /// <summary>
    /// 物体挤压拉伸反馈组件 (Squash and Stretch Juice)
    /// 赋予角色与交互物件生动 Q 弹的手感反馈
    /// </summary>
    public class JuiceSquash : MonoBehaviour
    {
        [Header("默认参数")]
        [SerializeField] private float defaultDuration = 0.15f;

        private Vector3 _originalScale;
        private Coroutine _squashRoutine;

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void PlaySquash(float xMult = 1.25f, float yMult = 0.8f, float duration = -1f)
        {
            if (duration < 0f) duration = defaultDuration;
            if (_squashRoutine != null) StopCoroutine(_squashRoutine);
            _squashRoutine = StartCoroutine(AnimateScale(new Vector3(_originalScale.x * xMult, _originalScale.y * yMult, _originalScale.z), duration));
        }

        public void PlayPop(float multiplier = 1.3f, float duration = -1f)
        {
            if (duration < 0f) duration = defaultDuration;
            if (_squashRoutine != null) StopCoroutine(_squashRoutine);
            _squashRoutine = StartCoroutine(AnimateScale(_originalScale * multiplier, duration));
        }

        private IEnumerator AnimateScale(Vector3 targetScale, float duration)
        {
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            // 变形阶段
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            elapsed = 0f;
            // 回弹阶段
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                transform.localScale = Vector3.Lerp(targetScale, _originalScale, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            transform.localScale = _originalScale;
            _squashRoutine = null;
        }

        public void ResetScale()
        {
            if (_squashRoutine != null)
            {
                StopCoroutine(_squashRoutine);
                _squashRoutine = null;
            }
            transform.localScale = _originalScale;
        }
    }
}

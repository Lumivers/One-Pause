using System.Collections;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;

namespace OnePulse.Entities
{
    /// <summary>
    /// 残血古门 (Ancient Gate of Bloodline)
    /// 刻有神秘铭文：“生命 <= 40% 方可开启”。
    /// 低于 40% 时神门消散隐形让出通道；血量恢复时神门重新实体化凝聚封死。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Gate_AncientDoor : MonoBehaviour
    {
        [Header("开门脉搏阈值")]
        [SerializeField] private float requiredMaxPulse = 40f;

        [Header("视觉与消失效果")]
        [Tooltip("开启时是否使神门完全淡出消散，彻底清空通道")]
        [SerializeField] private bool vanishOnOpen = true;
        [SerializeField] private float fadeDuration = 0.22f;

        [Header("外观切片 (备用)")]
        [SerializeField] private Sprite closedDoorSprite;
        [SerializeField] private Sprite openDoorSprite;

        private Collider2D _col;
        private SpriteRenderer _renderer;
        private JuiceSquash _squash;
        private bool _isOpen = false;
        private Coroutine _fadeRoutine;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _squash = GetComponentInChildren<JuiceSquash>();
        }

        public void Initialize(Sprite closed, Sprite open, float threshold = 40f)
        {
            closedDoorSprite = closed;
            openDoorSprite = open;
            requiredMaxPulse = threshold;
            if (_col == null) _col = GetComponent<Collider2D>();
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_squash == null) _squash = GetComponentInChildren<JuiceSquash>();
            ApplyDoorStateImmediate(WorldHealthManager.CurrentPulse <= requiredMaxPulse);
        }

        private void OnEnable()
        {
            WorldHealthManager.OnPulseChanged += CheckDoorCondition;
            ApplyDoorStateImmediate(WorldHealthManager.CurrentPulse <= requiredMaxPulse);
        }

        private void Start()
        {
            ApplyDoorStateImmediate(WorldHealthManager.CurrentPulse <= requiredMaxPulse);
        }

        private void OnDisable()
        {
            WorldHealthManager.OnPulseChanged -= CheckDoorCondition;
        }

        private void CheckDoorCondition(float currentPulse, float maxPulse)
        {
            bool shouldOpen = currentPulse <= requiredMaxPulse;

            if (shouldOpen && !_isOpen)
            {
                OpenDoor();
            }
            else if (!shouldOpen && _isOpen)
            {
                CloseDoor();
            }
        }

        private void ApplyDoorStateImmediate(bool shouldOpen)
        {
            _isOpen = shouldOpen;
            if (_col != null) _col.enabled = !shouldOpen;

            if (_renderer != null)
            {
                if (vanishOnOpen)
                {
                    Color c = _renderer.color;
                    c.a = shouldOpen ? 0f : 1f;
                    _renderer.color = c;
                    _renderer.enabled = !shouldOpen;
                }
                else
                {
                    _renderer.enabled = true;
                    if (shouldOpen && openDoorSprite != null) _renderer.sprite = openDoorSprite;
                    else if (!shouldOpen && closedDoorSprite != null) _renderer.sprite = closedDoorSprite;
                }
            }
        }

        private void OpenDoor()
        {
            _isOpen = true;
            if (_col != null) _col.enabled = false;

            _squash?.PlayPop(1.3f, 0.2f);
            CameraShake.Trigger(0.25f, 0.18f);
            Debug.Log("<color=#FFEE55><b>[古代神门开启]</b> 满足残血要求！神门消散，通道畅通无阻！</color>");

            if (vanishOnOpen)
            {
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeRoutine(0f, false));
            }
            else
            {
                if (_renderer != null && openDoorSprite != null) _renderer.sprite = openDoorSprite;
            }
        }

        private void CloseDoor()
        {
            _isOpen = false;
            if (_col != null) _col.enabled = true;

            _squash?.PlaySquash(1.15f, 0.85f, 0.15f);
            Debug.Log("<color=#AAAAAA>[古代神门关闭] 生命过于充沛，神门重新凝聚封死！</color>");

            if (vanishOnOpen)
            {
                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                _fadeRoutine = StartCoroutine(FadeRoutine(1f, true));
            }
            else
            {
                if (_renderer != null && closedDoorSprite != null) _renderer.sprite = closedDoorSprite;
            }
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool enableAtStart)
        {
            if (_renderer == null) yield break;

            if (enableAtStart) _renderer.enabled = true;

            Color c = _renderer.color;
            float startAlpha = c.a;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                _renderer.color = c;
                yield return null;
            }

            c.a = targetAlpha;
            _renderer.color = c;
            _renderer.enabled = targetAlpha > 0.01f;
        }
    }
}

using System.Collections;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Entities;
using OnePulse.Juice;

namespace OnePulse.Player
{
    /// <summary>
    /// 玩家技能：移形换位
    /// 隔空点击生物/物体，对调彼此时空坐标
    /// </summary>
    public class PlayerSwapAbility : MonoBehaviour
    {
        [Header("换位设置")]
        [SerializeField] private LayerMask swappableLayer;
        [SerializeField] private float maxRange = 20f;
        [SerializeField] private KeyCode swapKey = KeyCode.Mouse0; // 鼠标左键触发
        [SerializeField] private KeyCode alternativeKey = KeyCode.F; // 备用快捷键
        [Tooltip("移形换位仅在指定关卡及以上解锁生效（第 5、6 关）")]
        [SerializeField] private int unlockLevel = 5;

        public bool IsUnlocked
        {
            get
            {
                int currentLevel = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
                return currentLevel >= unlockLevel;
            }
        }

        [Header("视觉与连线特效")]
        [SerializeField] private LineRenderer swapBeamRenderer;
        [SerializeField] private Color beamColor = new Color(0.3f, 0.85f, 1f, 0.9f);
        [SerializeField] private float beamDisplayDuration = 0.18f;
        [SerializeField] private Sprite aimReticleSprite;

        private Camera _mainCamera;
        private PlayerController2D _player;
        private ISwappable _hoveredTarget;
        private Transform _hoveredTransform;
        private GameObject _reticleObj;
        private SpriteRenderer _reticleRenderer;

        private void Awake()
        {
            if (swappableLayer.value == 0) swappableLayer = ~0;
            _mainCamera = Camera.main;
            _player = GetComponent<PlayerController2D>();
            SetupBeamRenderer();
            SetupReticle();
        }

        private void SetupReticle()
        {
            if (aimReticleSprite == null) return;
            _reticleObj = new GameObject("SwapAimReticle");
            _reticleRenderer = _reticleObj.AddComponent<SpriteRenderer>();
            _reticleRenderer.sprite = aimReticleSprite;
            _reticleRenderer.sortingOrder = 50;
            _reticleObj.transform.localScale = Vector3.one * 0.8f;
            _reticleObj.SetActive(false);
        }

        private void SetupBeamRenderer()
        {
            if (swapBeamRenderer == null)
            {
                swapBeamRenderer = GetComponentInChildren<LineRenderer>();
            }

            if (swapBeamRenderer == null)
            {
                GameObject beamObj = new GameObject("SwapBeamFX");
                beamObj.transform.SetParent(transform);
                swapBeamRenderer = beamObj.AddComponent<LineRenderer>();
                swapBeamRenderer.startWidth = 0.15f;
                swapBeamRenderer.endWidth = 0.15f;
                swapBeamRenderer.positionCount = 2;
                swapBeamRenderer.material = new Material(Shader.Find("Sprites/Default"));
                swapBeamRenderer.startColor = beamColor;
                swapBeamRenderer.endColor = beamColor;
                swapBeamRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (WorldHealthManager.IsDepleted) return;

            // 移形换位仅在第5，6关解锁生效
            if (!IsUnlocked)
            {
                if (_reticleObj != null && _reticleObj.activeSelf)
                {
                    _reticleObj.SetActive(false);
                }
                _hoveredTarget = null;
                _hoveredTransform = null;
                return;
            }

            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            DetectHoverTarget();

            if (Input.GetKeyDown(swapKey) || Input.GetKeyDown(alternativeKey))
            {
                ExecuteSwap();
            }
        }

        private void DetectHoverTarget()
        {
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            Collider2D hitCol = Physics2D.OverlapPoint(mousePos2D, swappableLayer);

            if (hitCol != null && hitCol.transform != transform)
            {
                ISwappable swappable = hitCol.GetComponentInParent<ISwappable>();
                if (swappable != null && swappable.CanSwap)
                {
                    _hoveredTarget = swappable;
                    _hoveredTransform = swappable.Transform;

                    if (_reticleObj != null)
                    {
                        _reticleObj.SetActive(true);
                        _reticleObj.transform.position = _hoveredTransform.position;
                        _reticleObj.transform.Rotate(0, 0, -60f * Time.deltaTime);
                    }
                    return;
                }
            }

            _hoveredTarget = null;
            _hoveredTransform = null;
            if (_reticleObj != null)
            {
                _reticleObj.SetActive(false);
            }
        }

        private void ExecuteSwap()
        {
            if (_hoveredTarget == null || !_hoveredTarget.CanSwap || !_player.CanSwap) return;

            Vector3 playerPos = transform.position;
            Vector3 targetPos = _hoveredTransform.position;

            float distance = Vector2.Distance(playerPos, targetPos);
            if (distance > maxRange) return;

            // 1. 播放连接光束
            StartCoroutine(ShowSwapBeam(playerPos, targetPos));

            // 2. 坐标对调
            _hoveredTarget.OnSwapped(playerPos, targetPos);
            _player.OnSwapped(targetPos, playerPos);

            // 3. 屏幕与音画反馈
            CameraShake.Trigger(0.25f, 0.15f);
            OnePulse.Audio.ProceduralAudioManager.PlaySwap();

            Debug.Log($"<color=#33FFFF>[移形换位触发]</color> 玩家与 [{_hoveredTransform.name}] 互换坐标！");

            _hoveredTarget = null;
            _hoveredTransform = null;
        }

        private IEnumerator ShowSwapBeam(Vector3 start, Vector3 end)
        {
            if (swapBeamRenderer == null) yield break;

            swapBeamRenderer.enabled = true;
            swapBeamRenderer.SetPosition(0, start);
            swapBeamRenderer.SetPosition(1, end);

            yield return new WaitForSeconds(beamDisplayDuration);

            swapBeamRenderer.enabled = false;
        }

        private void OnGUI()
        {
            if (!IsUnlocked) return;

            // 如果鼠标悬停在有效目标上，绘制微型准星提示
            if (_hoveredTransform != null && _mainCamera != null)
            {
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(_hoveredTransform.position);
                if (screenPos.z > 0)
                {
                    Rect labelRect = new Rect(screenPos.x - 60, Screen.height - screenPos.y - 45, 120, 25);
                    GUIStyle style = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold
                    };
                    style.normal.textColor = Color.cyan;
                    GUI.Label(labelRect, "[ 点击对调 ]", style);
                }
            }
        }
    }
}

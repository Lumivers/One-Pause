using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Juice;

namespace OnePulse.Entities
{

    // Level6圣殿核心祭坛光圈 (Sanctuary Altar Heart)

    [RequireComponent(typeof(Collider2D))]
    public class SanctuaryAltar : MonoBehaviour
    {
        [Header("共生目标")]
        [SerializeField] private int requiredCreatures = 4; // 2 蛮牛 + 2 治愈花
        [SerializeField] private float absorbRadius = 1.6f;  // 圣坛吸收判定半径（确保换位/推入能秒检）
        [SerializeField] private float decayInterval = 1.0f; // 每 1 秒流逝一次
        [SerializeField] private float decayAmount = 1.5f;    // 每次流逝扣除 1.5% 全局脉搏 (生存倒计时约 66 秒)
        [SerializeField] private float healPerDelivery = 20.0f; // 每送入一个生灵，圣坛反哺回血 20% (续命强心剂)
        [SerializeField] private float absorbDuration = 0.35f; // 吸收动画时长

        private readonly HashSet<GameObject> _collectedEntities = new HashSet<GameObject>();
        private bool _isCompleted = false;
        private Coroutine _decayRoutine;
        private JuiceSquash _squash;
        private Collider2D _col;

        public int CurrentCollected => _collectedEntities.Count;
        public int TotalRequired => requiredCreatures;
        public bool IsCompleted => _isCompleted;

        private void Awake()
        {
            _squash = GetComponentInChildren<JuiceSquash>();
            _col = GetComponent<Collider2D>();
            if (_col != null) _col.isTrigger = true;

            // 确保自身挂载 Kinematic Rigidbody2D，使物理引擎始终能侦测触发器事件
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }

        private void Start()
        {
            _decayRoutine = StartCoroutine(HealthDecayRoutine());
        }

        private void Update()
        {
            if (_isCompleted) return;

            // 每帧主动进行半径范围重叠检测，保证即使通过【移形换位】直接对调落入光圈，也能瞬间检测并吸收
            DetectCreaturesInRadius();
        }

        private void DetectCreaturesInRadius()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, absorbRadius);
            foreach (var hit in colliders)
            {
                if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                TryAbsorbCollider(hit);
            }
        }

        private IEnumerator HealthDecayRoutine()
        {
            while (!_isCompleted && !WorldHealthManager.IsDepleted)
            {
                yield return new WaitForSeconds(decayInterval);
                if (!_isCompleted && !WorldHealthManager.IsDepleted)
                {
                    WorldHealthManager.ApplyDamage(decayAmount, transform.position);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAbsorbCollider(other);
        }

        private void TryAbsorbCollider(Collider2D other)
        {
            if (_isCompleted || other == null) return;

            Creature_BatteringRam bull = other.GetComponentInParent<Creature_BatteringRam>();
            Plant_HealingFlower flower = other.GetComponentInParent<Plant_HealingFlower>();

            if (bull != null && !_collectedEntities.Contains(bull.gameObject))
            {
                _collectedEntities.Add(bull.gameObject);
                StartCoroutine(AbsorbEntityRoutine(bull.gameObject, "石牛"));
            }
            else if (flower != null && !_collectedEntities.Contains(flower.gameObject))
            {
                _collectedEntities.Add(flower.gameObject);
                StartCoroutine(AbsorbEntityRoutine(flower.gameObject, "治愈花"));
            }
        }

        private IEnumerator AbsorbEntityRoutine(GameObject entity, string typeName)
        {
            // 1. 立即剥夺物理判定与 AI 冲锋，防止穿模或二次碰撞
            Collider2D[] colliders = entity.GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Rigidbody2D rb = entity.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true;
            }

            MonoBehaviour[] scripts = entity.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in scripts)
            {
                if (s != null && (s.GetType().Name.Contains("Creature") || s.GetType().Name.Contains("Plant")))
                {
                    s.enabled = false;
                }
            }

            // 2. 播放旋转缩小并被吸向祭坛中心的动画
            Vector3 startPos = entity.transform.position;
            Vector3 targetPos = transform.position;
            Vector3 startScale = entity.transform.localScale;
            SpriteRenderer sr = entity.GetComponentInChildren<SpriteRenderer>();
            Color startColor = sr != null ? sr.color : Color.white;

            float elapsed = 0f;
            while (elapsed < absorbDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / absorbDuration);

                // 缓动吸入
                entity.transform.position = Vector3.Lerp(startPos, targetPos, t * t);
                entity.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (sr != null)
                {
                    sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                }

                yield return null;
            }

            entity.SetActive(false);

            // 3. 圣坛受能反馈
            OnEntityDelivered(entity, typeName);
        }

        private void OnEntityDelivered(GameObject entity, string typeName)
        {
            _squash?.PlayPop(1.4f, 0.25f);
            CameraShake.Trigger(0.25f, 0.2f);
            OnePulse.Audio.ProceduralAudioManager.PlayAbsorb();
            WorldHealthManager.ApplyHeal(healPerDelivery, transform.position);

            Debug.Log($"<color=#77FFAA>[生灵归位吸收] {typeName} 已融入圣坛！当前进度: {_collectedEntities.Count}/{requiredCreatures}</color>");

            if (_collectedEntities.Count >= requiredCreatures)
            {
                TriggerSanctuaryMiracle();
            }
        }

        private void TriggerSanctuaryMiracle()
        {
            _isCompleted = true;
            if (_decayRoutine != null) StopCoroutine(_decayRoutine);

            WorldHealthManager.ResetHealth();
            CameraShake.Trigger(0.6f, 0.5f);
            OnePulse.Audio.ProceduralAudioManager.PlayVictory();

            Debug.Log("<color=#FFEE55><b>[大共生奇迹诞生！]</b> 4 大生灵齐聚，圣坛完全唤醒，世界脉搏回满！</color>");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }

        private void OnGUI()
        {
            if (_isCompleted || WorldHealthManager.IsDepleted) return;

            // 在屏幕中上方血条下方绘制【圣灵归位进度】
            float w = 240f;
            float h = 30f;
            float x = (Screen.width - w) * 0.5f;
            float y = 78f;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = new Color(1f, 0.95f, 0.6f);

            GUI.Label(new Rect(x, y, w, h), $"圣灵归位进度: {_collectedEntities.Count} / {requiredCreatures}", style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, absorbRadius);
        }
    }
}

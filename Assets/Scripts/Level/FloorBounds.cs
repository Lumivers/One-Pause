using UnityEngine;

namespace OnePulse.Level
{
    /// <summary>
    /// 地面/走廊自动护栏组件
    /// 自动根据 SpriteRenderer 的实际尺寸，在地板四周生成实心强力空气墙，
    /// 彻底阻断任何走出石板、飘入背景草地的可能。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FloorBounds : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<FloorBounds> AllFloors = new System.Collections.Generic.List<FloorBounds>();
        public static FloorBounds Current { get; private set; }

        [Header("护栏开关")]
        [Tooltip("阻挡上方（防止走出石板上方草地）")]
        [SerializeField] private bool blockTop = true;

        [Tooltip("阻挡下方（防止走出石板下方深渊/草地）")]
        [SerializeField] private bool blockBottom = true;

        [Tooltip("阻挡左侧（防止走出石板左端）")]
        [SerializeField] private bool blockLeft = true;

        [Tooltip("阻挡右侧（默认为 false 留出传送门或通道）")]
        [SerializeField] private bool blockRight = false;

        public bool BlockTop => blockTop;
        public bool BlockBottom => blockBottom;
        public bool BlockLeft => blockLeft;
        public bool BlockRight => blockRight;

        [Header("参数微调")]
        [Tooltip("空气墙厚度（米），默认 5.0 米坚固厚墙，绝不穿模")]
        [SerializeField] private float wallThickness = 5.0f;

        [Tooltip("边界内缩微调（米），微调贴合视觉边缘")]
        [SerializeField] private float verticalInset = 0.05f;

        private GameObject _containerObj;
        private BoxCollider2D _topWall;
        private BoxCollider2D _bottomWall;
        private BoxCollider2D _leftWall;
        private BoxCollider2D _rightWall;
        private SpriteRenderer _sr;

        public Bounds WorldBounds => _sr != null ? _sr.bounds : new Bounds(transform.position, Vector3.one * 10f);

        private void Awake()
        {
            if (!AllFloors.Contains(this)) AllFloors.Add(this);
            Current = this;
            _sr = GetComponent<SpriteRenderer>();
            RebuildBounds();
        }

        private void OnEnable()
        {
            if (!AllFloors.Contains(this)) AllFloors.Add(this);
            Current = this;
            RebuildBounds();
        }

        private void OnDisable()
        {
            AllFloors.Remove(this);
            if (Current == this) Current = AllFloors.Count > 0 ? AllFloors[0] : null;
        }

        private void Start()
        {
            RebuildBounds();
        }

        private void LateUpdate()
        {
            // 如果石板位置发生变动，动态校准
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                RebuildBounds();
            }
        }

        private void OnDestroy()
        {
            AllFloors.Remove(this);
            if (_containerObj != null)
            {
                Destroy(_containerObj);
            }
            if (Current == this) Current = AllFloors.Count > 0 ? AllFloors[0] : null;
        }

        [ContextMenu("立即刷新边缘护栏")]
        public void RebuildBounds()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;

            Bounds bounds = _sr.bounds;
            float worldW = bounds.size.x;
            float worldH = bounds.size.y;

            // 建立独立于缩放的世界容器（Scale 恒为 1，不受父级非等比缩放污染）
            EnsureContainer();

            float halfThick = wallThickness * 0.5f;

            // 1. 上方护栏 (厚度 5 米，从石板顶边 bounds.max.y 紧密向上覆盖整片草地)
            if (blockTop)
            {
                if (_topWall == null) _topWall = CreateWorldWall("Boundary_Top");
                _topWall.transform.position = new Vector3(bounds.center.x, bounds.max.y - verticalInset + halfThick, 0f);
                _topWall.size = new Vector2(worldW + (wallThickness * 2f), wallThickness);
                _topWall.enabled = true;
            }
            else if (_topWall != null) _topWall.enabled = false;

            // 2. 下方护栏 (厚度 5 米，从石板底边向外封死)
            if (blockBottom)
            {
                if (_bottomWall == null) _bottomWall = CreateWorldWall("Boundary_Bottom");
                _bottomWall.transform.position = new Vector3(bounds.center.x, bounds.min.y + verticalInset - halfThick, 0f);
                _bottomWall.size = new Vector2(worldW + (wallThickness * 2f), wallThickness);
                _bottomWall.enabled = true;
            }
            else if (_bottomWall != null) _bottomWall.enabled = false;

            // 3. 左侧护栏 (厚度 5 米，从石板左边缘向外封死)
            if (blockLeft)
            {
                if (_leftWall == null) _leftWall = CreateWorldWall("Boundary_Left");
                _leftWall.transform.position = new Vector3(bounds.min.x - halfThick, bounds.center.y, 0f);
                _leftWall.size = new Vector2(wallThickness, worldH + (wallThickness * 2f));
                _leftWall.enabled = true;
            }
            else if (_leftWall != null) _leftWall.enabled = false;

            // 4. 右侧护栏
            if (blockRight)
            {
                if (_rightWall == null) _rightWall = CreateWorldWall("Boundary_Right");
                _rightWall.transform.position = new Vector3(bounds.max.x + halfThick, bounds.center.y, 0f);
                _rightWall.size = new Vector2(wallThickness, worldH + (wallThickness * 2f));
                _rightWall.enabled = true;
            }
            else if (_rightWall != null) _rightWall.enabled = false;
        }

        private void EnsureContainer()
        {
            if (_containerObj == null)
            {
                _containerObj = new GameObject($"[FloorBounds_Walls_{gameObject.name}]");
                _containerObj.transform.position = Vector3.zero;
                _containerObj.transform.rotation = Quaternion.identity;
                _containerObj.transform.localScale = Vector3.one;
            }
        }

        private BoxCollider2D CreateWorldWall(string wallName)
        {
            EnsureContainer();
            Transform existing = _containerObj.transform.Find(wallName);
            GameObject wallObj;
            if (existing != null)
            {
                wallObj = existing.gameObject;
            }
            else
            {
                wallObj = new GameObject(wallName);
                wallObj.transform.SetParent(_containerObj.transform, false);
            }

            wallObj.layer = gameObject.layer;
            BoxCollider2D col = wallObj.GetComponent<BoxCollider2D>();
            if (col == null) col = wallObj.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            return col;
        }

        private void OnDrawGizmosSelected()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.7f);
            Gizmos.DrawWireCube(sr.bounds.center, sr.bounds.size);
        }
    }
}

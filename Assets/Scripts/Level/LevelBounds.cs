using UnityEngine;

namespace OnePulse.Level
{
    /// <summary>
    /// 全局关卡边界结界 (Level Bounds)
    /// 自动根据 Main Camera 视野在屏幕上下左右生成4面空气墙，
    /// 确保玩家、蛮牛与箱子无法穿出当前关卡视口，不用手动拼墙。
    /// </summary>
    public class LevelBounds : MonoBehaviour
    {
        [Header("边界内缩边距 (留出适当屏幕安全区)")]
        [SerializeField] private float horizontalPadding = 0.5f;
        [SerializeField] private float verticalPadding = 0.5f;

        [Header("是否显示编辑视口辅助线")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.5f);

        private BoxCollider2D _topWall;
        private BoxCollider2D _bottomWall;
        private BoxCollider2D _leftWall;
        private BoxCollider2D _rightWall;
        private Camera _cam;

        private void Awake()
        {
            _cam = Camera.main;
            SetupBoundaryColliders();
        }

        private void Start()
        {
            UpdateBoundaryPositions();
        }

        private void SetupBoundaryColliders()
        {
            _topWall = CreateWall("Bound_Top");
            _bottomWall = CreateWall("Bound_Bottom");
            _leftWall = CreateWall("Bound_Left");
            _rightWall = CreateWall("Bound_Right");
        }

        private BoxCollider2D CreateWall(string wallName)
        {
            GameObject wallObj = new GameObject(wallName);
            wallObj.transform.SetParent(transform);
            wallObj.layer = gameObject.layer;
            
            BoxCollider2D col = wallObj.AddComponent<BoxCollider2D>();
            col.isTrigger = false; // 实心空气墙，物理碰撞阻挡
            return col;
        }

        public void UpdateBoundaryPositions()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            float camHeight = _cam.orthographicSize * 2f;
            float camWidth = camHeight * _cam.aspect;
            Vector3 camPos = _cam.transform.position;

            float wallThickness = 2f; // 空气墙厚度，防止高速冲撞穿模

            float halfW = (camWidth * 0.5f) - horizontalPadding;
            float halfH = (camHeight * 0.5f) - verticalPadding;

            // 1. 顶部空气墙
            if (_topWall != null)
            {
                _topWall.transform.position = new Vector3(camPos.x, camPos.y + halfH + (wallThickness * 0.5f), 0f);
                _topWall.size = new Vector2(camWidth + 4f, wallThickness);
            }

            // 2. 底部空气墙
            if (_bottomWall != null)
            {
                _bottomWall.transform.position = new Vector3(camPos.x, camPos.y - halfH - (wallThickness * 0.5f), 0f);
                _bottomWall.size = new Vector2(camWidth + 4f, wallThickness);
            }

            // 3. 左侧空气墙
            if (_leftWall != null)
            {
                _leftWall.transform.position = new Vector3(camPos.x - halfW - (wallThickness * 0.5f), camPos.y, 0f);
                _leftWall.size = new Vector2(wallThickness, camHeight + 4f);
            }

            // 4. 右侧空气墙
            if (_rightWall != null)
            {
                _rightWall.transform.position = new Vector3(camPos.x + halfW + (wallThickness * 0.5f), camPos.y, 0f);
                _rightWall.size = new Vector2(wallThickness, camHeight + 4f);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            Camera cam = _cam != null ? _cam : Camera.main;
            if (cam == null) return;

            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;
            Vector3 camPos = cam.transform.position;

            float w = camWidth - (horizontalPadding * 2f);
            float h = camHeight - (verticalPadding * 2f);

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(new Vector3(camPos.x, camPos.y, 0f), new Vector3(w, h, 0f));
        }
    }
}

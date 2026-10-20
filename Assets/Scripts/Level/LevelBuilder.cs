using System.Collections.Generic;
using UnityEngine;
using OnePulse.Core;
using OnePulse.Entities;
using OnePulse.Juice;
using OnePulse.Player;

namespace OnePulse.Level
{
    /// <summary>
    /// 关卡程序化构建器 (Level Builder)
    /// 解析关卡规格案中的 ASCII 字符网格，极速装配并生成所有物理与交互实体
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public static LevelBuilder Instance { get; private set; }

        [Header("运行选项")]
        [Tooltip("是否在运行启动时自动程序化生成 ASCII 关卡。若自己在场景中摆放关卡，请保持 false")]
        [SerializeField] private bool autoBuildOnStart = false;

        [Header("网格规格")]
        [SerializeField] private float cellSize = 1.2f;

        [Header("美术切片资产 (自动注入或手动绑定)")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite bullIdleSprite;
        [SerializeField] private Sprite bullAngrySprite;
        [SerializeField] private Sprite exclamationSprite;
        [SerializeField] private Sprite flowerSprite;
        [SerializeField] private Sprite spikeSprite;
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private Sprite blockSprite;
        [SerializeField] private Sprite closedDoorSprite;
        [SerializeField] private Sprite openDoorSprite;
        [SerializeField] private Sprite backgroundSprite;

        private GameObject _levelRoot;
        private Camera _mainCamera;

        #region ASCII 关卡设计图
        // Level 1: 初醒之森
        private readonly string[] Level1_Map = {
            "#################",
            "# P             #",
            "#               #",
            "#      S   H    #",
            "#               #",
            "#             E #",
            "#################"
        };

        // Level 2: 蛮荒之角
        private readonly string[] Level2_Map = {
            "#################",
            "# P             #",
            "#               #",
            "#       B       #",
            "#               #",
            "#       W       #",
            "#       E       #",
            "#################"
        };

        // Level 3: 濒死门禁
        private readonly string[] Level3_Map = {
            "#################",
            "# P             #",
            "#       S       #",
            "#       S       #",
            "#   H           #",
            "#           G   #",
            "#             E #",
            "#################"
        };

        // Level 4: 多米诺安全链
        private readonly string[] Level4_Map = {
            "#################",
            "# P             #",
            "#     T   H     #",
            "#               #",
            "# B       X   W #",
            "#             E #",
            "#################"
        };

        // Level 5: 移形换影
        private readonly string[] Level5_Map = {
            "#################",
            "# P   S #       #",
            "#       #   B   #",
            "#   X   #   H   #",
            "# # # # #       #",
            "#       #     E #",
            "#################"
        };

        // Level 6: 共生圣殿
        private readonly string[] Level6_Map = {
            "#################",
            "#       A       #",
            "#               #",
            "#   B       B   #",
            "#      H   H    #",
            "#               #",
            "#       P       #",
            "#################"
        };
        #endregion

        private void Awake()
        {
            Instance = this;
            _mainCamera = Camera.main;
            AutoLoadSpritesIfMissing();
        }

        private void OnEnable()
        {
            if (autoBuildOnStart && GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLoaded += BuildLevel;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLoaded -= BuildLevel;
            }
        }

        private void Start()
        {
            if (Application.isPlaying && autoBuildOnStart)
            {
                int startingLevel = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
                BuildLevel(startingLevel);
            }
        }

        [ContextMenu("Build Current Level (Preview)")]
        public void EditorBuildCurrentLevel()
        {
            AutoLoadSpritesIfMissing();
            int lvl = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
            BuildLevel(lvl);
        }

        [ContextMenu("Clear Level")]
        public void ClearLevel()
        {
            if (_levelRoot != null)
            {
                DestroyImmediate(_levelRoot);
            }
        }

        public void BuildLevel(int levelIndex)
        {
            // 清理上一关实体
            if (_levelRoot != null)
            {
                if (Application.isPlaying) Destroy(_levelRoot);
                else DestroyImmediate(_levelRoot);
            }
            _levelRoot = new GameObject($"Level_{levelIndex}_Root");

            string[] map = GetMapForLevel(levelIndex);
            if (map == null || map.Length == 0) return;

            int height = map.Length;
            int width = map[0].Length;

            Vector2 originOffset = new Vector2(-width * 0.5f * cellSize, height * 0.5f * cellSize);

            // 调整相机位置与视野
            if (_mainCamera != null)
            {
                _mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                _mainCamera.orthographic = true;
                _mainCamera.orthographicSize = Mathf.Max((height * cellSize * 0.65f), 5.5f);
            }

            SpawnBackground();

            // 解析网格生成实体
            for (int r = 0; r < height; r++)
            {
                string line = map[r];
                for (int c = 0; c < line.Length; c++)
                {
                    char tile = line[c];
                    Vector2 worldPos = originOffset + new Vector2(c * cellSize, -r * cellSize);

                    switch (tile)
                    {
                        case '#':
                            CreateWall(worldPos);
                            break;
                        case 'P':
                            CreatePlayer(worldPos);
                            break;
                        case 'B':
                            CreateBull(worldPos);
                            break;
                        case 'H':
                            CreateHealingFlower(worldPos);
                            break;
                        case 'S':
                            CreateSpike(worldPos);
                            break;
                        case 'W':
                            CreateBreakableWall(worldPos);
                            break;
                        case 'T':
                            CreatePushableBlock(worldPos);
                            break;
                        case 'X':
                            CreateBomb(worldPos);
                            break;
                        case 'G': // 古门
                            CreateAncientGate(worldPos);
                            break;
                        case 'A': // 圣殿祭坛核心
                            CreateSanctuaryAltar(worldPos);
                            break;
                        case 'E': // 出口
                            CreateExitPortal(worldPos);
                            break;
                    }
                }
            }

            Debug.Log($"<color=#77FFAA>[关卡构建完成] 关卡 {levelIndex} 成功生成 {width}x{height} 网格！</color>");
        }

        private string[] GetMapForLevel(int index)
        {
            switch (index)
            {
                case 1: return Level1_Map;
                case 2: return Level2_Map;
                case 3: return Level3_Map;
                case 4: return Level4_Map;
                case 5: return Level5_Map;
                case 6: return Level6_Map;
                default: return Level1_Map;
            }
        }

        #region 实体工厂方法
        private void SpawnBackground()
        {
            if (backgroundSprite == null) return;
            GameObject bgObj = new GameObject("Background_SunriseTemple");
            bgObj.transform.SetParent(_levelRoot.transform);
            bgObj.transform.position = new Vector3(0, 0, 5);

            SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = backgroundSprite;
            sr.sortingOrder = -100;

            // 自适应缩放背景
            float camHeight = _mainCamera != null ? _mainCamera.orthographicSize * 2f : 12f;
            float camWidth = camHeight * (_mainCamera != null ? _mainCamera.aspect : 1.777f);
            float bgScale = Mathf.Max(camWidth / backgroundSprite.bounds.size.x, camHeight / backgroundSprite.bounds.size.y);
            bgObj.transform.localScale = Vector3.one * bgScale * 1.05f;
        }

        private GameObject CreateWall(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Wall_Brick", pos, wallSprite, 0);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.95f);
            obj.layer = LayerMask.NameToLayer("Default");
            return obj;
        }

        private GameObject CreatePlayer(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Player_Hero", pos, playerSprite, 10);
            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = cellSize * 0.35f;

            obj.AddComponent<JuiceSquash>();
            PlayerController2D controller = obj.AddComponent<PlayerController2D>();
            PlayerSwapAbility swap = obj.AddComponent<PlayerSwapAbility>();

            return obj;
        }

        private GameObject CreateBull(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Creature_Bull", pos, bullIdleSprite, 5);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.85f);

            obj.AddComponent<JuiceSquash>();
            Creature_BatteringRam bull = obj.AddComponent<Creature_BatteringRam>();

            // 创建感叹号指示器子物体
            GameObject exObj = null;
            if (exclamationSprite != null)
            {
                exObj = new GameObject("ExclamationFX");
                exObj.transform.SetParent(obj.transform);
                exObj.transform.localPosition = new Vector3(0f, cellSize * 0.7f, 0f);
                SpriteRenderer exSr = exObj.AddComponent<SpriteRenderer>();
                exSr.sprite = exclamationSprite;
                exSr.sortingOrder = 20;
                ScaleToFit(exObj, exclamationSprite, cellSize * 0.5f);
                exObj.SetActive(false);
            }

            bull.Initialize(bullIdleSprite, bullAngrySprite, exObj);

            return obj;
        }

        private GameObject CreateHealingFlower(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Plant_HealingFlower", pos, flowerSprite, 2);
            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = cellSize * 0.4f;
            col.isTrigger = true;

            obj.AddComponent<JuiceSquash>();
            obj.AddComponent<Plant_HealingFlower>();
            return obj;
        }

        private GameObject CreateSpike(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Hazard_Spike", pos, spikeSprite, 1);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.75f);
            col.isTrigger = true;

            obj.AddComponent<JuiceSquash>();
            obj.AddComponent<Hazard_Spike>();
            return obj;
        }

        private GameObject CreateBreakableWall(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Breakable_Wall", pos, blockSprite != null ? blockSprite : wallSprite, 0);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.95f);

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.9f, 0.95f, 1f, 0.95f);

            obj.AddComponent<JuiceSquash>();
            obj.AddComponent<BreakableWall>();
            return obj;
        }

        private GameObject CreatePushableBlock(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Block_Pushable", pos, blockSprite, 3);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.85f);

            obj.AddComponent<JuiceSquash>();
            obj.AddComponent<Block_Pushable>();
            return obj;
        }

        private GameObject CreateBomb(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Hazard_Bomb", pos, spikeSprite, 2);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.8f);

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.4f, 0.2f); // 鲜明警示橙红色

            obj.AddComponent<JuiceSquash>();
            obj.AddComponent<Hazard_Bomb>();
            return obj;
        }

        private GameObject CreateAncientGate(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Gate_AncientDoor", pos, closedDoorSprite, 1);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.95f);

            obj.AddComponent<JuiceSquash>();
            Gate_AncientDoor gate = obj.AddComponent<Gate_AncientDoor>();
            gate.Initialize(closedDoorSprite, openDoorSprite, 40f);
            return obj;
        }

        private GameObject CreateExitPortal(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Exit_Portal", pos, openDoorSprite, 1);
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * (cellSize * 0.85f);
            col.isTrigger = true;

            obj.AddComponent<JuiceSquash>();
            obj.AddComponent<ExitPortal>();
            return obj;
        }

        private GameObject CreateSanctuaryAltar(Vector2 pos)
        {
            GameObject obj = CreateBaseEntity("Sanctuary_Altar", pos, openDoorSprite, 1);
            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = cellSize * 0.85f;
            col.isTrigger = true;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.9f, 1f, 0.7f, 0.9f);

            obj.AddComponent<JuiceSquash>();
            obj.AddComponent<SanctuaryAltar>();
            return obj;
        }

        private GameObject CreateBaseEntity(string name, Vector2 pos, Sprite sprite, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_levelRoot.transform);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);

            if (sprite != null)
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = sortingOrder;
                ScaleToFit(go, sprite, cellSize);
            }

            return go;
        }

        private void ScaleToFit(GameObject go, Sprite sprite, float targetDimension)
        {
            if (sprite == null) return;
            float maxNaturalDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (maxNaturalDim > 0.001f)
            {
                float scale = targetDimension / maxNaturalDim;
                go.transform.localScale = Vector3.one * scale;
            }
        }
        #endregion

        private void AutoLoadSpritesIfMissing()
        {
#if UNITY_EDITOR
            // 在 Unity 编辑器下自动从 Sprites 文件夹绑定切片
            if (playerSprite == null) playerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Player_Hero.png");
            if (bullIdleSprite == null) bullIdleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Bull_Idle.png");
            if (bullAngrySprite == null) bullAngrySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Bull_Angry.png");
            if (exclamationSprite == null) exclamationSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/FX_Exclamation.png");
            if (flowerSprite == null) flowerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Plant_HealingFlower.png");
            if (spikeSprite == null) spikeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Hazard_RedSpike.png");
            if (wallSprite == null) wallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Wall_StoneBrick.png");
            if (blockSprite == null) blockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Block_Pushable.png");
            if (closedDoorSprite == null) closedDoorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Door_Closed.png");
            if (openDoorSprite == null) openDoorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Door_PortalOpen.png");
            if (backgroundSprite == null) backgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/BG_SunriseTemple.png");
#endif
        }
    }
}

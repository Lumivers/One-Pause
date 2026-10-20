using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OnePulse.Core
{
    public enum GameState
    {
        Playing,
        LevelComplete,
        GameOver
    }

    /// <summary>
    /// 全局游戏流程管理器 (GameManager)
    /// 统筹关卡状态、重置流程、通关判定与按键监听
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("关卡状态")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int maxLevels = 6;

        public int CurrentLevel => currentLevel;
        public int MaxLevels => maxLevels;
        public GameState CurrentState { get; private set; } = GameState.Playing;

        public event Action<int> OnLevelLoaded;
        public event Action<int> OnLevelCompleted;
        public event Action OnGameOver;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            WorldHealthManager.OnPulseDepleted += HandleGameOver;
        }

        private void OnDisable()
        {
            WorldHealthManager.OnPulseDepleted -= HandleGameOver;
        }

        private void Start()
        {
            // 如果在编辑器中直接测试某个关卡场景，自动根据当前场景名推断关卡序号
            string sceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(sceneName) && (sceneName.StartsWith("Level_") || sceneName.StartsWith("Level")))
            {
                string numStr = sceneName.Replace("Level_", "").Replace("Level", "");
                if (int.TryParse(numStr, out int parsedLevel))
                {
                    currentLevel = parsedLevel;
                }
            }

            StartLevel(currentLevel);
        }

        private void Update()
        {
            // 按 R 键重置关卡
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartCurrentLevel();
            }

            // 通关后按空格键或回车键前往下一关
            if (CurrentState == GameState.LevelComplete && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                AdvanceToNextLevel();
            }
        }

        public void StartLevel(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, maxLevels);
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            WorldHealthManager.ResetHealth();
            OptimizeWallColliders();
            OnLevelLoaded?.Invoke(currentLevel);
            Debug.Log($"<color=#77FFAA><b>[进入关卡 {currentLevel}]</b></color>");
        }

        private void OptimizeWallColliders()
        {
            var colliders = FindObjectsOfType<BoxCollider2D>();
            foreach (var col in colliders)
            {
                if (col == null) continue;
                if (col.gameObject.name.StartsWith("Wall_StoneBrick_Horizontal"))
                {
                    col.size = new Vector2(0.96f, 0.45f);
                    col.offset = new Vector2(0f, -0.1f);
                    col.edgeRadius = 0.05f;
                }
                else if (col.gameObject.name.StartsWith("Wall_StoneBrick_Vertical"))
                {
                    col.size = new Vector2(0.35f, 0.7f);
                    col.offset = new Vector2(0f, -0.05f);
                    col.edgeRadius = 0.05f;
                }
                else if (col.gameObject.name.StartsWith("Wall_StoneBrick"))
                {
                    col.size = new Vector2(0.8f, 0.5f);
                    col.offset = new Vector2(0f, -0.15f);
                    col.edgeRadius = 0.05f;
                }
            }
        }

        public void CompleteLevel()
        {
            if (CurrentState != GameState.Playing) return;

            CurrentState = GameState.LevelComplete;
            Time.timeScale = 0f;
            OnLevelCompleted?.Invoke(currentLevel);
            Debug.Log($"<color=#FFFF55><b>[关卡 {currentLevel} 胜利！]</b></color>");
        }

        public void AdvanceToNextLevel()
        {
            Time.timeScale = 1f;

            if (currentLevel >= maxLevels)
            {
                Debug.Log("<color=#FFDD55><b>[全通关！恭喜完成《共生之界》全部关卡！]</b></color>");
                return;
            }

            int nextLevel = currentLevel + 1;

            // 1. 尝试按规范场景名加载 (如 Level_02, Level_2)
            string[] candidateScenes = new string[]
            {
                $"Level_{nextLevel:D2}",
                $"Level_{nextLevel}",
                $"Level{nextLevel:D2}",
                $"Level{nextLevel}"
            };

            foreach (var sceneName in candidateScenes)
            {
                if (Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.Log($"<color=#77FFAA><b>[加载下一关场景: {sceneName}]</b></color>");
                    currentLevel = nextLevel;
                    CurrentState = GameState.Playing;
                    WorldHealthManager.ResetHealth();
                    SceneManager.LoadScene(sceneName);
                    return;
                }
            }

            // 2. 尝试按 Build Index 加载下一个已启用的场景
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex > 0 && nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                currentLevel = nextLevel;
                CurrentState = GameState.Playing;
                WorldHealthManager.ResetHealth();
                SceneManager.LoadScene(nextIndex);
                return;
            }

            // 3. 若下一关场景尚未创建，打印清晰提示并重启当前关
            Debug.LogWarning($"<color=#FFAA44><b>[提示]</b> 未找到第 {nextLevel} 关场景（例如 Level_02.unity）。你可以将场景另存为 Level_02，并在 Build Settings 中添加它！</color>");
            RestartCurrentLevel();
        }

        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f;
            CurrentState = GameState.Playing;
            WorldHealthManager.ResetHealth();
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded && !string.IsNullOrEmpty(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
            }
            else
            {
                StartLevel(currentLevel);
            }
        }

        private void HandleGameOver()
        {
            CurrentState = GameState.GameOver;
            Time.timeScale = 0f;
            OnGameOver?.Invoke();
        }
    }
}

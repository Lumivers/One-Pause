using UnityEngine;
using OnePulse.Core;

namespace OnePulse.UI
{
    /// <summary>
    /// 《共生之界》启动界面与操作指南浮层 (TitleScreenUI)
    /// 可以在 Inspector 中通过 showOnStart 随时开关，关卡调试时直接关闭即可跳过。
    /// 游戏中随时按 ESC 即可调出设置与暂停弹窗。
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("启动配置")]
        [Tooltip("游戏启动时是否显示开始界面。关卡设计调试时可取消勾选，直接进游戏。")]
        [SerializeField] private bool showOnStart = true;

        [Header("视觉与动画")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private Texture2D coverBackgroundTexture;

        [Header("主界面文本 (直接在 Inspector 中修改即可)")]
        [SerializeField] private string gameTitle = "共 生 之 界";
        [SerializeField] private string englishTitle = "— O N E   P U L S E —";
        [SerializeField] private string startButtonText = "⚡ 开始共生之旅";
        [SerializeField] private string rulesButtonText = "📖 游戏法则与操作指南";
        [SerializeField] private string settingsButtonText = "⚙️ 游戏系统设置";
        [SerializeField] private string quitButtonText = "🚪 退出游戏";

        [Header("玩法法则说明文本")]
        [SerializeField] private string rulesTitle = "📜 世界法则与行动准则";
        [TextArea(6, 12)]
        [SerializeField] private string rulesContent =
            "<b>1. 【单一脉搏 · 全员共享生命】</b>\n" +
            "屏幕顶部只有唯一一条【世界脉搏】。主角、蛮牛、机关全员共享此血条！\n" +
            "任何人受伤世界脉搏下跌，任何人治愈脉搏上升。归零则时空重置。\n\n" +
            "<b>2. 【主角能力 · 零攻击力】</b>\n" +
            "• <b>[ W A S D / 方向键 ]</b>：控制白袍小精灵 4 向移动与推物。\n" +
            "• <b>[ 鼠标左键 / F 键 ]</b>：<b>【移形换位】</b>，隔空互换与生物或箱子的时空坐标！\n\n" +
            "<b>3. 【涌现生机】</b>\n" +
            "• <b>暴躁石牛</b>：眼神接触即刻冲撞！利用它撞碎坚墙，但撞碎等同自残。\n" +
            "• <b>残血古门</b>：唯有血量压低至危急线（<=40%）时才会古老开启。\n" +
            "• <b>推石垫脚</b>：把碎石箱推到刺上垫脚，或隔空换位救人。";

        private bool _isActive = false;
        private bool _showRulesModal = false;
        private bool _showSettingsModal = false;
        private float _fadeAlpha = 1f;
        private bool _isFadingOut = false;
        private Texture2D _pixelTex;
        private float _masterVolume = 1f;

        public bool IsActive => _isActive;

        private void Awake()
        {
            _pixelTex = new Texture2D(1, 1);
            _pixelTex.SetPixel(0, 0, Color.white);
            _pixelTex.Apply();

            _isActive = showOnStart;
            _fadeAlpha = showOnStart ? 1f : 0f;
            _masterVolume = AudioListener.volume;
            if (showOnStart)
            {
                Time.timeScale = 0f;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (_pixelTex != null)
            {
                Destroy(_pixelTex);
            }
        }

        private void Update()
        {
            // 全局监听 ESC 键调出/关闭设置菜单
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isActive)
                {
                    if (_showRulesModal || _showSettingsModal)
                    {
                        _showRulesModal = false;
                        _showSettingsModal = false;
                    }
                    else
                    {
                        StartGame();
                    }
                }
                else
                {
                    // 游戏中暂停呼出设置
                    _isActive = true;
                    _fadeAlpha = 1f;
                    _showSettingsModal = true;
                    Time.timeScale = 0f;
                }
            }

            if (!_isActive) return;

            // 监听回车键或空格键快速开始
            if (!_showRulesModal && !_showSettingsModal && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            {
                StartGame();
            }

            if (_isFadingOut)
            {
                _fadeAlpha -= Time.unscaledDeltaTime / Mathf.Max(fadeDuration, 0.05f);
                if (_fadeAlpha <= 0f)
                {
                    _fadeAlpha = 0f;
                    _isActive = false;
                    _isFadingOut = false;
                    Time.timeScale = 1f;
                }
            }
        }

        public void StartGame()
        {
            if (_isFadingOut) return;
            _isFadingOut = true;
            _showRulesModal = false;
            _showSettingsModal = false;
            Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!_isActive && _fadeAlpha <= 0f) return;

            Color originalColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _fadeAlpha);

            float sw = Screen.width;
            float sh = Screen.height;

            // 1. 全屏底图或渐变遮罩
            if (coverBackgroundTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, _fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, sw, sh), coverBackgroundTexture, ScaleMode.ScaleAndCrop);
                DrawRect(new Rect(0, 0, sw, sh), new Color(0.02f, 0.04f, 0.08f, 0.35f * _fadeAlpha));
            }
            else
            {
                DrawRect(new Rect(0, 0, sw, sh), new Color(0.04f, 0.06f, 0.1f, 0.72f * _fadeAlpha));
            }

            if (_showRulesModal)
            {
                DrawRulesModal(sw, sh);
            }
            else if (_showSettingsModal)
            {
                DrawSettingsModal(sw, sh);
            }
            else
            {
                DrawMainTitle(sw, sh);
            }

            GUI.color = originalColor;
        }

        private void DrawMainTitle(float sw, float sh)
        {
            // 标题面板居中
            float panelW = Mathf.Min(650f, sw * 0.9f);
            float panelH = 500f;
            float startX = (sw - panelW) * 0.5f;
            float startY = (sh - panelH) * 0.45f;

            // 标题发光卡片背景
            DrawRect(new Rect(startX, startY, panelW, panelH), new Color(0.08f, 0.11f, 0.16f, 0.88f * _fadeAlpha));
            DrawBorder(new Rect(startX, startY, panelW, panelH), new Color(0.35f, 0.85f, 0.95f, 0.65f * _fadeAlpha), 2);

            // 主标题
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(sw * 0.038f), 32, 52),
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.95f, 0.98f, 1f, _fadeAlpha);

            GUIStyle engStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(sw * 0.016f), 14, 20),
                fontStyle = FontStyle.Normal
            };
            engStyle.normal.textColor = new Color(0.45f, 0.88f, 1f, 0.85f * _fadeAlpha);

            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(sw * 0.014f), 13, 16),
                fontStyle = FontStyle.Normal
            };
            subtitleStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f, 0.8f * _fadeAlpha);

            GUI.Label(new Rect(startX, startY + 25, panelW, 55), gameTitle, titleStyle);
            GUI.Label(new Rect(startX, startY + 80, panelW, 25), englishTitle, engStyle);

            // 分割金线
            DrawRect(new Rect(startX + 60, startY + 150, panelW - 120, 2), new Color(0.3f, 0.8f, 0.9f, 0.4f * _fadeAlpha));

            // 按钮样式
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            btnStyle.normal.textColor = Color.white;

            float btnW = 280f;
            float btnH = 44f;
            float btnX = startX + (panelW - btnW) * 0.5f;

            // 按钮 1：开始游戏
            if (GUI.Button(new Rect(btnX, startY + 175, btnW, btnH), startButtonText, btnStyle))
            {
                StartGame();
            }

            // 按钮 2：操作与规则指南
            if (GUI.Button(new Rect(btnX, startY + 230, btnW, btnH), rulesButtonText, btnStyle))
            {
                _showRulesModal = true;
            }

            // 按钮 3：系统设置
            if (GUI.Button(new Rect(btnX, startY + 285, btnW, btnH), settingsButtonText, btnStyle))
            {
                _showSettingsModal = true;
            }

            // 按钮 4：退出
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                if (GUI.Button(new Rect(btnX, startY + 345, btnW, 38), quitButtonText, btnStyle))
                {
                    Application.Quit();
                }
            }

            // 快捷提示
            GUIStyle tipStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            tipStyle.normal.textColor = new Color(0.65f, 0.75f, 0.85f, 0.6f * _fadeAlpha);
            GUI.Label(new Rect(startX, startY + panelH - 28, panelW, 25), "提示：按 [Space] 或 [Enter] 快速开始 · 随时按 [ESC] 暂停", tipStyle);
        }

        private void DrawSettingsModal(float sw, float sh)
        {
            float modalW = Mathf.Min(620f, sw * 0.9f);
            float modalH = 460f;
            float modalX = (sw - modalW) * 0.5f;
            float modalY = (sh - modalH) * 0.45f;

            DrawRect(new Rect(modalX, modalY, modalW, modalH), new Color(0.06f, 0.09f, 0.14f, 0.96f * _fadeAlpha));
            DrawBorder(new Rect(modalX, modalY, modalW, modalH), new Color(0.3f, 0.8f, 0.9f, 0.8f * _fadeAlpha), 2);

            GUIStyle hStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            hStyle.normal.textColor = new Color(0.35f, 0.9f, 1f);
            GUI.Label(new Rect(modalX, modalY + 25, modalW, 35), "⚙️ 系统与音画设置", hStyle);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;

            // 1. 音量滑块
            float contentY = modalY + 80;
            GUI.Label(new Rect(modalX + 50, contentY, 200, 25), $"🔊 全局音量: {Mathf.RoundToInt(_masterVolume * 100)}%", labelStyle);
            _masterVolume = GUI.HorizontalSlider(new Rect(modalX + 260, contentY + 6, 260, 20), _masterVolume, 0f, 1f);
            AudioListener.volume = _masterVolume;

            // 2. 全屏/窗口切换
            contentY += 55;
            string screenModeText = Screen.fullScreen ? "🖥️ 切换为窗口模式" : "🖥️ 切换为全屏模式";
            GUIStyle btnToggleStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
            btnToggleStyle.normal.textColor = Color.white;
            if (GUI.Button(new Rect(modalX + 50, contentY, 240, 36), screenModeText, btnToggleStyle))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }

            // 3. 重置当前关卡
            if (GUI.Button(new Rect(modalX + 310, contentY, 210, 36), "🔄 重新开始本关 (R键)", btnToggleStyle))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartCurrentLevel();
                }
                StartGame();
            }

            // 4. 操作键位速查
            contentY += 60;
            DrawRect(new Rect(modalX + 45, contentY, modalW - 90, 110), new Color(0.1f, 0.14f, 0.2f, 0.7f));
            GUIStyle keyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };
            keyStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
            string keyInfo = 
                "• <b>[ W A S D / 方向键 ]</b>：控制白袍小精灵移动与推箱\n" +
                "• <b>[ 鼠标左键 / F 键 ]</b>：鼠标悬停在石牛或机关上隔空移形换位\n" +
                "• <b>[ R 键 ]</b>：随时秒速重置当前解谜房间\n" +
                "• <b>[ ESC 键 ]</b>：随时暂停并呼出本设置菜单";
            GUI.Label(new Rect(modalX + 55, contentY + 10, modalW - 110, 95), keyInfo, keyStyle);

            // 返回按钮
            GUIStyle backBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            backBtnStyle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(modalX + (modalW - 180) * 0.5f, modalY + modalH - 58, 180, 40), "✔ 保存并返回", backBtnStyle))
            {
                _showSettingsModal = false;
            }
        }

        private void DrawRulesModal(float sw, float sh)
        {
            float modalW = Mathf.Min(680f, sw * 0.92f);
            float modalH = 490f;
            float modalX = (sw - modalW) * 0.5f;
            float modalY = (sh - modalH) * 0.45f;

            DrawRect(new Rect(modalX, modalY, modalW, modalH), new Color(0.06f, 0.09f, 0.14f, 0.96f * _fadeAlpha));
            DrawBorder(new Rect(modalX, modalY, modalW, modalH), new Color(0.3f, 0.8f, 0.9f, 0.8f * _fadeAlpha), 2);

            GUIStyle hStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            hStyle.normal.textColor = new Color(0.35f, 0.9f, 1f);
            GUI.Label(new Rect(modalX, modalY + 25, modalW, 35), rulesTitle, hStyle);

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                richText = true
            };
            textStyle.normal.textColor = new Color(0.9f, 0.92f, 0.95f);

            GUI.Label(new Rect(modalX + 35, modalY + 75, modalW - 70, 310), rulesContent, textStyle);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            btnStyle.normal.textColor = Color.white;

            if (GUI.Button(new Rect(modalX + (modalW - 180) * 0.5f, modalY + modalH - 60, 180, 42), "✔ 我明白了，返回", btnStyle))
            {
                _showRulesModal = false;
            }
        }

        private void DrawRect(Rect rect, Color color)
        {
            if (_pixelTex == null) return;
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _pixelTex);
            GUI.color = prev;
        }

        private void DrawBorder(Rect rect, Color color, int thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
        }
    }
}

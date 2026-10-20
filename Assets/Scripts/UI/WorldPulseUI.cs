using UnityEngine;
using OnePulse.Core;

namespace OnePulse.UI
{
    /// <summary>
    /// 屏幕顶部世界脉搏HUD 与全局反馈 UI
    /// 实时渲染共享生命值、低血量危急呼吸红边、通关与重置提示
    /// </summary>
    public class WorldPulseUI : MonoBehaviour
    {
        [Header("样式与配色")]
        [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.9f, 0.6f);
        [SerializeField] private Color warningHealthColor = new Color(1f, 0.75f, 0.2f);
        [SerializeField] private Color dangerHealthColor = new Color(0.95f, 0.2f, 0.25f);

        [Header("美术贴图 (可选)")]
        [SerializeField] private Texture2D pulseBarFrameTexture;
        [SerializeField] private Texture2D pulseBarFillTexture;

        private float _displayPulse = 100f;
        private Texture2D _whiteTex;
        private bool _showHintModal = false;

        private void Awake()
        {
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
            _displayPulse = WorldHealthManager.CurrentPulse;
        }

        private void OnDestroy()
        {
            if (_whiteTex != null)
            {
                Destroy(_whiteTex);
            }
        }

        private void Update()
        {
            // 平滑缓动显示血条
            _displayPulse = Mathf.Lerp(_displayPulse, WorldHealthManager.CurrentPulse, Time.unscaledDeltaTime * 10f);

            // 监听快捷键 [H] 随时唤出/收起通关提示弹窗
            if (Input.GetKeyDown(KeyCode.H))
            {
                _showHintModal = !_showHintModal;
            }

            // 监听通关后的继续按键
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.LevelComplete)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    _showHintModal = false;
                    GameManager.Instance.AdvanceToNextLevel();
                }
            }
        }

        private void OnGUI()
        {
            DrawHealthBar();
            DrawHintButton();
            DrawLevelInfoAndControls();
            DrawLowHealthVignette();
            DrawHintModal();
            DrawStateModals();
        }

        private void DrawHealthBar()
        {
            float max = WorldHealthManager.MaxPulse;
            float percent = Mathf.Clamp01(_displayPulse / max);
            float screenW = Screen.width;

            if (pulseBarFrameTexture != null && pulseBarFillTexture != null)
            {
                // 使用美术交付的心电图唯美装饰外框与发光填充条
                float frameW = Mathf.Min(560f, screenW * 0.72f);
                float frameH = frameW * (pulseBarFrameTexture.height / (float)pulseBarFrameTexture.width);
                float frameX = (screenW - frameW) * 0.5f;
                float frameY = 15f;

                // 填充条区域位于装饰框内侧（留出两端心电图翼展）
                float fillPaddingX = frameW * 0.088f;
                float fillPaddingY = frameH * 0.16f;
                float fillW = frameW - fillPaddingX * 2f;
                float fillH = frameH - fillPaddingY * 2f;
                float fillX = frameX + fillPaddingX;
                float fillY = frameY + fillPaddingY;

                // 1. 绘制底框 (两端心电图翼与暗红底槽)
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(frameX, frameY, frameW, frameH), pulseBarFrameTexture, ScaleMode.StretchToFill, true);

                // 2. 绘制发光填充条 (通过 UV 裁剪实现真实的条形填充)
                if (percent > 0.001f)
                {
                    Color fillTint = Color.white;
                    if (percent <= 0.4f)
                    {
                        float flash = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
                        fillTint = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), flash);
                    }
                    GUI.color = fillTint;
                    Rect fillScreenRect = new Rect(fillX, fillY, fillW * percent, fillH);
                    Rect fillTexCoords = new Rect(0f, 0f, percent, 1f);
                    GUI.DrawTextureWithTexCoords(fillScreenRect, pulseBarFillTexture, fillTexCoords, true);
                }

                // 3. 文字标题与百分比
                GUI.color = Color.white;
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(Mathf.Clamp(frameH * 0.38f, 12f, 18f)),
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = Color.white;

                string heartIcon = percent <= 0.4f ? "❤!" : "❤";
                string text = $"{heartIcon} 世界脉搏: {Mathf.CeilToInt(WorldHealthManager.CurrentPulse)}% / 100%";
                GUI.Label(new Rect(fillX, fillY, fillW, fillH), text, labelStyle);
            }
            else
            {
                // 回退原生纯色模式
                float barW = Mathf.Min(420f, screenW * 0.7f);
                float barH = 26f;
                float barX = (screenW - barW) * 0.5f;
                float barY = 20f;

                // 1. 底框阴影
                GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
                GUI.DrawTexture(new Rect(barX - 4, barY - 4, barW + 8, barH + 8), _whiteTex);

                // 2. 灰色底槽
                GUI.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), _whiteTex);

                // 3. 动态血量颜色计算
                Color barColor = fullHealthColor;
                if (percent <= 0.4f)
                {
                    float flash = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
                    barColor = Color.Lerp(dangerHealthColor, new Color(1f, 0.4f, 0.4f), flash);
                }
                else if (percent <= 0.65f)
                {
                    barColor = warningHealthColor;
                }

                GUI.color = barColor;
                GUI.DrawTexture(new Rect(barX, barY, barW * percent, barH), _whiteTex);

                // 4. 文字标题与百分比
                GUI.color = Color.white;
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = Color.white;

                string heartIcon = percent <= 0.4f ? "❤!" : "❤";
                string text = $"{heartIcon} 世界脉搏: {Mathf.CeilToInt(WorldHealthManager.CurrentPulse)}% / 100%";
                GUI.Label(new Rect(barX, barY, barW, barH), text, labelStyle);
            }
        }

        private void DrawHintButton()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            float screenW = Screen.width;
            float frameW = pulseBarFrameTexture != null ? Mathf.Min(560f, screenW * 0.72f) : Mathf.Min(420f, screenW * 0.7f);
            float frameX = (screenW - frameW) * 0.5f;
            float frameY = pulseBarFrameTexture != null ? 15f : 20f;
            float frameH = pulseBarFrameTexture != null ? frameW * (pulseBarFrameTexture.height / (float)pulseBarFrameTexture.width) : 26f;

            float btnW = 92f;
            float btnH = 30f;
            // 放置在中央血条左侧
            float btnX = Mathf.Max(16f, frameX - btnW - 14f);
            float btnY = frameY + (frameH - btnH) * 0.5f;

            GUI.color = Color.white;
            string btnText = _showHintModal ? "收 起 [H]" : "💡 提示 [H]";
            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), btnText))
            {
                OnePulse.Audio.ProceduralAudioManager.PlayClick();
                _showHintModal = !_showHintModal;
            }
        }

        private void DrawHintModal()
        {
            if (!_showHintModal) return;

            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
            string levelName = GetLevelName(level);
            string hint = GetLevelHint(level);

            float modalW = Mathf.Min(500f, Screen.width * 0.88f);
            float modalH = 160f;
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = 85f; // 正好悬浮在中央血条下方

            // 1. 半透明暗色磨砂玻璃底
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            GUI.DrawTexture(new Rect(modalX, modalY, modalW, modalH), _whiteTex);

            // 2. 金色细框
            GUI.color = new Color(1f, 0.85f, 0.35f, 0.85f);
            float b = 2f;
            GUI.DrawTexture(new Rect(modalX, modalY, modalW, b), _whiteTex); // 顶
            GUI.DrawTexture(new Rect(modalX, modalY + modalH - b, modalW, b), _whiteTex); // 底
            GUI.DrawTexture(new Rect(modalX, modalY, b, modalH), _whiteTex); // 左
            GUI.DrawTexture(new Rect(modalX + modalW - b, modalY, b, modalH), _whiteTex); // 右

            // 3. 标题
            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.9f, 0.45f);
            GUI.Label(new Rect(modalX, modalY + 14, modalW, 25), $"💡 关卡 {level}: {levelName} · 通关线索", titleStyle);

            // 4. 详细提示正文 (WordWrap 自动换行)
            GUIStyle contentStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                wordWrap = true
            };
            contentStyle.normal.textColor = new Color(0.95f, 0.97f, 1f);
            GUI.Label(new Rect(modalX + 24, modalY + 44, modalW - 48, 65), hint, contentStyle);

            // 5. 知道了按钮
            float closeBtnW = 110f;
            float closeBtnH = 28f;
            if (GUI.Button(new Rect(modalX + (modalW - closeBtnW) * 0.5f, modalY + modalH - 38, closeBtnW, closeBtnH), "知道了 [H]"))
            {
                OnePulse.Audio.ProceduralAudioManager.PlayClick();
                _showHintModal = false;
            }
        }

        private void DrawLevelInfoAndControls()
        {
            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
            string levelName = GetLevelName(level);

            GUIStyle levelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            levelStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 0.95f);
            GUI.Label(new Rect(20, 16, 450, 22), $"《共生之界》- 关卡 {level}: {levelName}", levelStyle);

            GUIStyle controlStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 10
            };
            controlStyle.normal.textColor = new Color(0.75f, 0.82f, 0.9f, 0.7f);
            string controlText = level >= 5
                ? "[WASD/方向键] 移动/推物  |  [鼠标左键/F] 移形换位  |  [H] 关卡提示  |  [R] 重置关卡"
                : "[WASD/方向键] 移动/推物  |  [H] 关卡提示  |  [R] 重置关卡";
            GUI.Label(new Rect(20, 38, 550, 18), controlText, controlStyle);
        }

        private void DrawLowHealthVignette()
        {
            if (WorldHealthManager.PulsePercent > 0.4f || WorldHealthManager.IsDepleted) return;

            // 濒死全屏红光呼吸闪烁
            float breath = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.08f, 0.28f, breath);

            GUI.color = new Color(1f, 0.05f, 0.05f, alpha);
            float borderSize = 35f;

            // 四周红色警示框
            GUI.DrawTexture(new Rect(0, 0, Screen.width, borderSize), _whiteTex); // 顶
            GUI.DrawTexture(new Rect(0, Screen.height - borderSize, Screen.width, borderSize), _whiteTex); // 底
            GUI.DrawTexture(new Rect(0, 0, borderSize, Screen.height), _whiteTex); // 左
            GUI.DrawTexture(new Rect(Screen.width - borderSize, 0, borderSize, Screen.height), _whiteTex); // 右
        }

        private void DrawStateModals()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                DrawGameOverModal();
            }
            else if (GameManager.Instance.CurrentState == GameState.LevelComplete)
            {
                DrawLevelCompleteModal();
            }
        }

        private void DrawGameOverModal()
        {
            float modalW = 380f;
            float modalH = 160f;
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;

            GUI.color = new Color(0.1f, 0.02f, 0.02f, 0.92f);
            GUI.DrawTexture(new Rect(modalX, modalY, modalW, modalH), _whiteTex);

            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(modalX, modalY + 20, modalW, 35), "世界脉搏枯竭", titleStyle);

            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            descStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(modalX, modalY + 58, modalW, 25), "生灵共灭，心跳归于沉寂...", descStyle);

            if (GUI.Button(new Rect(modalX + 110, modalY + 98, 160, 35), "按 [R] 重新唤醒"))
            {
                GameManager.Instance.RestartCurrentLevel();
            }
        }

        private void DrawLevelCompleteModal()
        {
            float modalW = 420f;
            float modalH = 175f;
            float modalX = (Screen.width - modalW) * 0.5f;
            float modalY = (Screen.height - modalH) * 0.5f;

            bool isGrandFinale = GameManager.Instance.CurrentLevel >= 6;

            GUI.color = isGrandFinale ? new Color(0.08f, 0.06f, 0.02f, 0.95f) : new Color(0.02f, 0.1f, 0.08f, 0.92f);
            GUI.DrawTexture(new Rect(modalX, modalY, modalW, modalH), _whiteTex);

            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = isGrandFinale ? 22 : 20,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = isGrandFinale ? new Color(1f, 0.88f, 0.3f) : new Color(0.3f, 1f, 0.6f);
            string titleText = isGrandFinale ? "【万象共生 · 奇迹涌现】" : "关卡突破！";
            GUI.Label(new Rect(modalX, modalY + 18, modalW, 35), titleText, titleStyle);

            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            descStyle.normal.textColor = Color.white;
            string descText = isGrandFinale ? "生灵齐聚，世界脉搏彻底复苏！恭喜全通关！" : "传送漩涡平稳运转...";
            GUI.Label(new Rect(modalX, modalY + 58, modalW, 25), descText, descStyle);

            if (isGrandFinale)
            {
                if (GUI.Button(new Rect(modalX + 130, modalY + 105, 160, 38), "重新回味圣殿 [R]"))
                {
                    GameManager.Instance.RestartCurrentLevel();
                }
            }
            else
            {
                if (GUI.Button(new Rect(modalX + 130, modalY + 105, 160, 38), "前往下一关 [空格]"))
                {
                    GameManager.Instance.AdvanceToNextLevel();
                }
            }
        }

        private string GetLevelName(int level)
        {
            switch (level)
            {
                case 1: return "初醒之森 (The Awakening)";
                case 2: return "蛮荒之角 (Borrowed Might)";
                case 3: return "濒死门禁 (The Brink of Death)";
                case 4: return "多米诺安全链 (The Domino Guard)";
                case 5: return "移形换影 (Soul Swap)";
                case 6: return "共生圣殿 (Heart of the World)";
                default: return "未名秘境";
            }
        }

        private string GetLevelHint(int level)
        {
            switch (level)
            {
                case 1: return "万物同心，角色受创将直接扣除共享脉搏；谨慎踏过荆棘前往传送门！";
                case 2: return "蛮牛拥有摧坚蛮力；走位诱导其视线冲锋，为你撞破阻挡去路的坚固石墙！";
                case 3: return "向死而生方见生机；精算受创血量，用生命之火解锁门禁通向未来！";
                case 4: return "巧妙推移石箱构筑掩体，引导蛮牛冲撞引发安全的多米诺连锁！";
                case 5: return "掌握【移形换位】！隔空点击生灵互换坐标，化天堑为通途！";
                case 6: return "虚空侵蚀下脉搏持续流逝！利用移形换位将 4 大生灵全部送入圣坛光圈以拯救世界！";
                default: return "顺应万象共生的法则，找寻通向未来的路径。";
            }
        }
    }
}

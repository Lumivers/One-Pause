using System;
using System.Collections;
using UnityEngine;
using OnePulse.Core;

namespace OnePulse.Audio
{
    /// <summary>
    /// 代码程序化音频合成管理器 (Procedural Audio Synthesizer)
    /// 游戏启动即自启全局单例，自动监听战斗、换位与关卡事件
    /// </summary>
    public class ProceduralAudioManager : MonoBehaviour
    {
        public static ProceduralAudioManager Instance { get; private set; }

        private const int SampleRate = 44100;

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;

        private AudioClip _clipSwap;
        private AudioClip _clipHeal;
        private AudioClip _clipAbsorb;
        private AudioClip _clipDamage;
        private AudioClip _clipImpact;
        private AudioClip _clipVictory;
        private AudioClip _clipClick;
        private AudioClip _clipBGM;

        private bool _isMuted = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject audioRoot = new GameObject("[ProceduralAudioManager]");
                audioRoot.AddComponent<ProceduralAudioManager>();
                DontDestroyOnLoad(audioRoot);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化 AudioSource 组件
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.75f;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = 0.22f;

            // 预合成所有音效波形
            GenerateAllClips();

            // 开始播放背景环境轻音乐
            StartBGM();
        }

        private void OnEnable()
        {
            WorldHealthManager.OnDamageTaken += HandleDamage;
            WorldHealthManager.OnHealed += HandleHeal;
            WorldHealthManager.OnPulseDepleted += HandleGameOver;
        }

        private void OnDisable()
        {
            WorldHealthManager.OnDamageTaken -= HandleDamage;
            WorldHealthManager.OnHealed -= HandleHeal;
            WorldHealthManager.OnPulseDepleted -= HandleGameOver;
        }

        private void Update()
        {
            // 按 M 键快捷静音/取消静音
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMute();
            }
        }

        public void ToggleMute()
        {
            _isMuted = !_isMuted;
            if (_bgmSource != null) _bgmSource.mute = _isMuted;
            if (_sfxSource != null) _sfxSource.mute = _isMuted;
            Debug.Log($"<color=#77DDFF>[音频管理器] {(_isMuted ? "已静音" : "已开启声音")}</color>");
        }

        #region 事件监听处理
        private void HandleDamage(float amount, Vector3 pos)
        {
            PlaySFX(_clipDamage, 0.8f);
        }

        private void HandleHeal(float amount, Vector3 pos)
        {
            PlaySFX(_clipHeal, 0.75f);
        }

        private void HandleGameOver()
        {
            PlaySFX(_clipDamage, 0.9f);
        }
        #endregion

        #region 外部调用接口
        public static void PlaySwap()
        {
            Instance?.PlaySFX(Instance._clipSwap, 0.7f);
        }

        public static void PlayAbsorb()
        {
            Instance?.PlaySFX(Instance._clipAbsorb, 0.85f);
        }

        public static void PlayImpact()
        {
            Instance?.PlaySFX(Instance._clipImpact, 0.8f);
        }

        public static void PlayHeal()
        {
            Instance?.PlaySFX(Instance._clipHeal, 0.75f);
        }

        public static void PlayHealDirect()
        {
            Instance?.PlaySFX(Instance._clipHeal, 0.75f);
        }

        public static void PlayDamage()
        {
            Instance?.PlaySFX(Instance._clipDamage, 0.8f);
        }

        public static void PlayDamageDirect()
        {
            Instance?.PlaySFX(Instance._clipDamage, 0.8f);
        }

        public static void PlayVictory()
        {
            Instance?.PlaySFX(Instance._clipVictory, 0.95f);
        }

        public static void PlayClick()
        {
            Instance?.PlaySFX(Instance._clipClick, 0.5f);
        }
        #endregion

        private void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
        {
            if (_isMuted || clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, volumeScale);
        }

        private void StartBGM()
        {
            if (_bgmSource != null && _clipBGM != null)
            {
                _bgmSource.clip = _clipBGM;
                _bgmSource.Play();
            }
        }

        #region 数学波形合成器 (Procedural Audio Synthesis)
        private void GenerateAllClips()
        {
            _clipSwap = CreateSwapClip();
            _clipHeal = CreateHealClip();
            _clipAbsorb = CreateAbsorbClip();
            _clipDamage = CreateDamageClip();
            _clipImpact = CreateImpactClip();
            _clipVictory = CreateVictoryClip();
            _clipClick = CreateClickClip();
            _clipBGM = CreateBGMClip();
        }

        /// <summary>
        /// 移形换位：空灵上升滑音 (420Hz -> 1180Hz)
        /// </summary>
        private AudioClip CreateSwapClip()
        {
            float duration = 0.22f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            double phase = 0.0;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // 频率指数递增滑升
                float freq = Mathf.Lerp(420f, 1180f, Mathf.Pow(progress, 1.4f));
                phase += 2.0 * Math.PI * freq / SampleRate;

                // 柔和起振与渐隐包络
                float envelope = (1f - progress) * Mathf.Clamp01(t * 120f);

                // 正弦波 + 水晶泛音
                float s = (float)(Math.Sin(phase) * 0.75 + Math.Sin(phase * 2.0) * 0.25);
                samples[i] = s * envelope * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_Swap", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 汲取治愈/回血：大三和弦空灵三连音 (C5 -> E5 -> G5)
        /// </summary>
        private AudioClip CreateHealClip()
        {
            float duration = 0.35f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
            float noteDelay = 0.08f;

            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                int startIdx = (int)(n * noteDelay * SampleRate);

                for (int i = startIdx; i < numSamples; i++)
                {
                    float noteT = (float)(i - startIdx) / SampleRate;
                    float env = Mathf.Exp(-9f * noteT);
                    float wave = (float)(Math.Sin(2.0 * Math.PI * freq * noteT) + 0.3 * Math.Sin(4.0 * Math.PI * freq * noteT));
                    samples[i] += wave * env * 0.35f;
                }
            }

            AudioClip clip = AudioClip.Create("SFX_Heal", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 圣灵归位：神圣大和弦共鸣 (C5 + G5 + C6 辉光)
        /// </summary>
        private AudioClip CreateAbsorbClip()
        {
            float duration = 0.45f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float[] chord = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-5.5f * t) * Mathf.Clamp01(t * 80f);

                double sum = 0.0;
                foreach (float f in chord)
                {
                    sum += Math.Sin(2.0 * Math.PI * f * t);
                }

                samples[i] = (float)(sum * 0.22) * env;
            }

            AudioClip clip = AudioClip.Create("SFX_Absorb", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 受到伤害/脉搏受创：低沉心跳重击声 (85Hz -> 45Hz)
        /// </summary>
        private AudioClip CreateDamageClip()
        {
            float duration = 0.18f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            double phase = 0.0;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                float freq = Mathf.Lerp(85f, 42f, progress);
                phase += 2.0 * Math.PI * freq / SampleRate;

                float env = Mathf.Exp(-12f * t);
                // 少量沙质噪声模拟破损感
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Clamp01(1f - progress * 3f) * 0.3f;

                samples[i] = ((float)Math.Sin(phase) * 0.8f + noise) * env;
            }

            AudioClip clip = AudioClip.Create("SFX_Damage", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 蛮牛撞墙/水晶碎裂：低音重炮 + 破裂砂石声
        /// </summary>
        private AudioClip CreateImpactClip()
        {
            float duration = 0.28f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float lastNoise = 0f;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // 低通滤波低音轰鸣
                float rawNoise = UnityEngine.Random.value * 2f - 1f;
                lastNoise = Mathf.Lerp(lastNoise, rawNoise, 0.25f);

                float bass = (float)Math.Sin(2.0 * Math.PI * 65.0 * t) * Mathf.Exp(-14f * t);
                float crackle = lastNoise * Mathf.Exp(-8f * t) * 0.7f;

                samples[i] = Mathf.Clamp((bass * 0.7f + crackle), -1f, 1f) * (1f - progress);
            }

            AudioClip clip = AudioClip.Create("SFX_Impact", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 关卡突破 / 终极通关凯歌：四音连贯华丽胜势 (C5 -> E5 -> G5 -> C6)
        /// </summary>
        private AudioClip CreateVictoryClip()
        {
            float duration = 0.8f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            float step = 0.12f;

            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                int start = (int)(n * step * SampleRate);

                for (int i = start; i < numSamples; i++)
                {
                    float noteT = (float)(i - start) / SampleRate;
                    float env = Mathf.Exp(-4.5f * noteT);
                    float s = (float)(Math.Sin(2.0 * Math.PI * freq * noteT) + 0.35 * Math.Sin(4.0 * Math.PI * freq * noteT));
                    samples[i] += s * env * 0.28f;
                }
            }

            AudioClip clip = AudioClip.Create("SFX_Victory", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 按钮/UI 点按清脆木质敲击声
        /// </summary>
        private AudioClip CreateClickClip()
        {
            float duration = 0.05f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-40f * t);
                samples[i] = (float)Math.Sin(2.0 * Math.PI * 880.0 * t) * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create("SFX_Click", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 空灵治愈背景环境轻音乐 (12 秒完美无缝循环 Ambient)
        /// 采用 F 大调温和双耳和弦 + 治愈系八音盒随机五声音阶
        /// </summary>
        private AudioClip CreateBGMClip()
        {
            float duration = 12.0f;
            int numSamples = (int)(SampleRate * duration);
            float[] samples = new float[numSamples];

            // 1. 底层温暖和弦伴奏 (F -> C -> Dm -> C, 每 3 秒一换)
            float[][] chordProgression = new float[][]
            {
                new float[] { 174.61f, 220.00f, 261.63f }, // F3, A3, C4
                new float[] { 130.81f, 196.00f, 261.63f }, // C3, G3, C4
                new float[] { 146.83f, 220.00f, 293.66f }, // D3, A3, D4
                new float[] { 130.81f, 164.81f, 196.00f }  // C3, E3, G3
            };

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / SampleRate;
                int barIndex = Mathf.Clamp((int)(t / 3.0f), 0, 3);
                float[] curChord = chordProgression[barIndex];

                // 柔和的正弦叠加，带微弱的慢速呼吸颤音
                float lfo = 1f + 0.08f * (float)Math.Sin(2.0 * Math.PI * 0.6 * t);
                double chordSample = 0.0;
                foreach (float freq in curChord)
                {
                    chordSample += Math.Sin(2.0 * Math.PI * freq * t);
                }

                // 2. 顶层治愈八音盒散落音符 (固定伪随机种子确保生成确定性)
                samples[i] = (float)(chordSample * 0.065 * lfo);
            }

            // 叠加八音盒五声音阶单音 (Pentatonic notes: F4, G4, A4, C5, D5)
            float[] bellNotes = new float[] { 349.23f, 392.00f, 440.00f, 523.25f, 587.33f };
            float[] bellTimes = new float[] { 0.4f, 1.8f, 3.4f, 4.6f, 6.2f, 7.8f, 9.2f, 10.6f };

            for (int b = 0; b < bellTimes.Length; b++)
            {
                float noteFreq = bellNotes[b % bellNotes.Length];
                int startSample = (int)(bellTimes[b] * SampleRate);

                for (int i = startSample; i < numSamples && i < startSample + SampleRate * 2; i++)
                {
                    float noteT = (float)(i - startSample) / SampleRate;
                    float env = Mathf.Exp(-3.2f * noteT);
                    float bell = (float)(Math.Sin(2.0 * Math.PI * noteFreq * noteT) + 0.3 * Math.Sin(4.0 * Math.PI * noteFreq * noteT));
                    samples[i] += bell * env * 0.085f;
                }
            }

            // 首尾交叉淡化，消除循环爆音
            int fadeSamples = (int)(SampleRate * 0.1f);
            for (int i = 0; i < fadeSamples; i++)
            {
                float factor = (float)i / fadeSamples;
                samples[i] *= factor;
                samples[numSamples - 1 - i] *= factor;
            }

            AudioClip clip = AudioClip.Create("BGM_SanctuaryAmbient", numSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        #endregion
    }
}

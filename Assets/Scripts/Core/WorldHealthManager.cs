using System;
using UnityEngine;

namespace OnePulse.Core
{
    /// <summary>
    /// 全局世界脉搏管理器 (Shared World Health Pool)
    /// 游戏中所有生物、玩家、植物与机关共享同一道脉搏数值。
    /// </summary>
    public static class WorldHealthManager
    {
        public static float MaxPulse = 100f;
        public static float CurrentPulse { get; private set; } = 100f;

        public static float PulsePercent => MaxPulse > 0f ? Mathf.Clamp01(CurrentPulse / MaxPulse) : 0f;
        public static bool IsDepleted => CurrentPulse <= 0f;

        // 事件广播
        public static event Action<float, float> OnPulseChanged; // (current, max)
        public static event Action<float, Vector3> OnDamageTaken; // (amount, sourcePosition)
        public static event Action<float, Vector3> OnHealed;      // (amount, sourcePosition)
        public static event Action OnPulseDepleted;
        
        // 造成全局扣血
        public static void ApplyDamage(float amount, Vector3 sourcePosition = default)
        {
            if (amount <= 0f || IsDepleted) return;

            CurrentPulse = Mathf.Max(CurrentPulse - amount, 0f);
            OnDamageTaken?.Invoke(amount, sourcePosition);
            OnPulseChanged?.Invoke(CurrentPulse, MaxPulse);

            Debug.Log($"<color=#FF5555>[世界脉搏受创]</color> 扣除: {amount:F1} | 剩余: {CurrentPulse:F1}/{MaxPulse:F1}");

            if (CurrentPulse <= 0f)
            {
                Debug.Log("<color=#FF2222><b>[世界脉搏枯竭] 全体消亡！</b></color>");
                OnPulseDepleted?.Invoke();
            }
        }
        
        // 触发全局回血
        public static void ApplyHeal(float amount, Vector3 sourcePosition = default)
        {
            if (amount <= 0f) return;

            CurrentPulse = Mathf.Min(CurrentPulse + amount, MaxPulse);
            OnHealed?.Invoke(amount, sourcePosition);
            OnPulseChanged?.Invoke(CurrentPulse, MaxPulse);

            Debug.Log($"<color=#55FF88>[世界脉搏复苏]</color> 治愈: {amount:F1} | 剩余: {CurrentPulse:F1}/{MaxPulse:F1}");
        }

        // 重置脉搏至满额
        public static void ResetHealth()
        {
            CurrentPulse = MaxPulse;
            OnPulseChanged?.Invoke(CurrentPulse, MaxPulse);
            Debug.Log("<color=#77DDFF>[世界脉搏重置] 回满至 100%</color>");
        }
        
        // 手动设置当前脉搏（用于特殊关卡或关卡初始状态）
        public static void SetPulse(float value)
        {
            CurrentPulse = Mathf.Clamp(value, 0f, MaxPulse);
            OnPulseChanged?.Invoke(CurrentPulse, MaxPulse);
        }
    }
}

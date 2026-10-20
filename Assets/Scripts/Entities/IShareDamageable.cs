using UnityEngine;

namespace OnePulse.Entities
{
    /// <summary>
    /// 共享生命接口：受击或回血时直接影响全局唯一的世界脉搏
    /// </summary>
    public interface IShareDamageable
    {
        void TakeDamage(float amount);
        void Heal(float amount);
    }
}

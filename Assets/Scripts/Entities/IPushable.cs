using UnityEngine;

namespace OnePulse.Entities
{
    /// <summary>
    /// 可推动实体接口：支持玩家或蛮牛碰撞冲撞推动
    /// </summary>
    public interface IPushable
    {
        bool TryPush(Vector2 direction, float force = 1f);
        bool IsPushing { get; }
        Transform Transform { get; }
    }
}

using UnityEngine;

namespace OnePulse.Entities
{
    /// <summary>
    /// 可移形换位接口：支持玩家隔空对调位置
    /// </summary>
    public interface ISwappable
    {
        bool CanSwap { get; }
        Transform Transform { get; }
        void OnSwapped(Vector3 newPosition, Vector3 oldPosition);
    }
}

using UnityEngine;
using Resonance.Interfaces;

namespace Resonance.Enemies
{
    public enum WeakpointType 
    { 
        Physical, 
        Mental 
    }

    public class WeakpointHitbox : MonoBehaviour
    {
        public WeakpointType type;
        public float physicalMultiplier = 2f;     // 打到时对物理部分的倍率
        public float mentalMultiplier   = 2f;     // 打到时对精神部分的倍率
        
        [Range(0,1)] public float convertPhysicalToMental = 0f; // 例如核心被打时，把物理伤害按比例转为精神
        
        public float poiseBonus = 0f;             // 额外韧性值
        public GameObject hitVFX; public AudioClip hitSFX;

        // 供武器命中前调用：返回修改后的 DamageInfo
        public DamageInfo ModifyDamage(DamageInfo d, Vector3 hitPoint)
        {
            switch (d.type)
            {
                case DamageType.Physical:
                    if (convertPhysicalToMental > 0f && type == WeakpointType.Mental) {
                        d.type = DamageType.Mixed;
                        d.physicalRatio = Mathf.Clamp01(1f - convertPhysicalToMental);
                    } else {
                        d.amount *= physicalMultiplier;
                    }
                    break;
                case DamageType.Mental:
                    d.amount *= mentalMultiplier;
                    break;
                case DamageType.Mixed:
                    // 按比例分别乘，以免“头部弱点也放大精神伤害”这种迷惑
                    var phys = d.amount * d.physicalRatio  * physicalMultiplier;
                    var ment = d.amount * (1f - d.physicalRatio) * mentalMultiplier;
                    d.amount = phys + ment;
                    break;
            }
            d.sourcePosition = hitPoint;
            d.description = string.IsNullOrEmpty(d.description)
                ? $"Weakpoint:{type}"
                : $"{d.description}|Weakpoint:{type}";
            return d;
        }

        public void PlayHitFX(Vector3 at)
        {
            if (hitVFX) Instantiate(hitVFX, at, Quaternion.identity);
            if (hitSFX) AudioSource.PlayClipAtPoint(hitSFX, at);
        }
    }
}
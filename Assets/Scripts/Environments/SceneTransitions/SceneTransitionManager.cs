using UnityEngine;

namespace Resonance.Environments
{
    public class SceneTransitionManager : MonoBehaviour
    {
        // 在每个场景中自动检测并完成pending的场景切换
        void Start()
        {
            // var transitionService = ServiceRegistry.Get<ISceneTransitionService>();
            // transitionService?.CompleteTransition();
        }
    }
}


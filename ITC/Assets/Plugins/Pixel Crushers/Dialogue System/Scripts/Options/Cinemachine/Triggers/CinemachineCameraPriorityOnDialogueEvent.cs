using UnityEngine;
#if USE_CINEMACHINE //2
using Cinemachine;
using CinemachineCam = Cinemachine.CinemachineVirtualCamera;
#elif USE_CINEMACHINE_3
using Unity.Cinemachine;
using CinemachineCam = Unity.Cinemachine.CinemachineCamera;
#endif

namespace PixelCrushers.DialogueSystem
{

#if USE_CINEMACHINE || USE_CINEMACHINE_3

    [AddComponentMenu("")] // Use wrapper.
    public class CinemachineCameraPriorityOnDialogueEvent : ActOnDialogueEvent
    {

        [Tooltip("要控制优先级的 Cinemachine virtual camera。")]
        public CinemachineCam virtualCamera;

        [Tooltip("在 start 事件发生时，将 virtual camera 设为此优先级。")]
        public int onStart = 99;

        [Tooltip("在 end 事件发生时，将 virtual camera 设为此优先级。")]
        public int onEnd = 0;

        public override void TryStartActions(Transform actor)
        {
            if (virtualCamera == null) return;
            virtualCamera.Priority = onStart;
        }

        public override void TryEndActions(Transform actor)
        {
            if (virtualCamera == null) return;
            virtualCamera.Priority = onEnd;
        }
    }

#else

    [AddComponentMenu("")] // Use wrapper.
    public class CinemachineCameraPriorityOnDialogueEvent : ActOnDialogueEvent
    {
        public override void TryStartActions(Transform actor) { }
        public override void TryEndActions(Transform actor) { }
    }

#endif

}

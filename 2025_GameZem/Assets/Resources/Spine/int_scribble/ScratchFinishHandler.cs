using UnityEngine;
using Spine.Unity;

public class ScratchFinishHandler : MonoBehaviour
{
    public SkeletonGraphic spine;
    public string finishAnim = "finish";
    public bool finishLoop = false;

    public void OnScratchCleared()
    {
        if (spine && !string.IsNullOrEmpty(finishAnim))
            spine.AnimationState.SetAnimation(0, finishAnim, finishLoop);
    }
}

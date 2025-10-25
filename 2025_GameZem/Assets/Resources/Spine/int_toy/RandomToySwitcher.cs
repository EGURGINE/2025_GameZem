using UnityEngine;
using Spine.Unity;

[DisallowMultipleComponent]
public class RandomToySwitcher : MonoBehaviour
{
    [Header("Spine SkeletonData Assets")]
    public SkeletonDataAsset[] toyDataAssets;

    [Header("Target SkeletonGraphic (하나만)")]
    public SkeletonGraphic skeletonGraphic;

    [Tooltip("시작할 때 1회만 랜덤 적용")]
    public bool pickOnceOnStart = true;

    bool _applied;   // 중복 실행 가드

    void Start()
    {
        if (pickOnceOnStart) ApplyRandomOnce();
    }

    public void ApplyRandomOnce()
    {
        if (_applied) return;  // 이미 한 번 적용했음
        _applied = true;

        if (!skeletonGraphic || toyDataAssets == null || toyDataAssets.Length == 0)
        {
            Debug.LogWarning("[RandomToySwitcher] 데이터/타겟 누락", this);
            enabled = false;
            return;
        }

        var data = toyDataAssets[Random.Range(0, toyDataAssets.Length)];
        skeletonGraphic.skeletonDataAsset = data;
        skeletonGraphic.Initialize(true);   // ★ 데이터만 교체 (애니는 ToyAnimator가 처리)

        // 더 이상 재실행되지 않도록 컴포넌트 꺼두기
        enabled = false;
    }
}

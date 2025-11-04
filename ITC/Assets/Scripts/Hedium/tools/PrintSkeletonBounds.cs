using UnityEngine;
using Spine.Unity;

public class PrintSkeletonBounds : MonoBehaviour
{
    public SkeletonGraphic skeletonGraphic;

    [ContextMenu("Print Skeleton Bounds")]
    void PrintBounds()
    {
        if (skeletonGraphic == null || skeletonGraphic.Skeleton == null)
        {
            Debug.LogError("请确保 SkeletonGraphic 已初始化！");
            return;
        }

        var skeleton = skeletonGraphic.Skeleton;

        float x, y, width, height;
        float[] vertexBuffer = null; // 先给 null，Spine 内部会处理
        skeleton.GetBounds(out x, out y, out width, out height, ref vertexBuffer);

        Debug.Log($"Skeleton Bounds: MinX={x}, MinY={y}, Width={width}, Height={height}, MaxX={x + width}, MaxY={y + height}");
    }
}
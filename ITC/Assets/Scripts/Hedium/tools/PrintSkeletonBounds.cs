using Spine.Unity;
using System.Linq;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

public class PrintSkeletonBounds : MonoBehaviour
{
    public SkeletonGraphic sk;
    public Image image;
    public RectTransform rectTrans;


    [ContextMenu("Print Skeleton Bounds")]
    void PrintBounds()
    {


       
        if ((sk == null || sk.Skeleton == null)
            &&
            (image == null || image.sprite == null)
            &&
            (rectTrans == null)
            )
        {
            Debug.LogError("请确保已初始化或者类型正确与否！");
            return;
        }
        Vector4 ans = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue) ; 
        if (image)
        {
            Vector3[] localVertices = new Vector3[4];
            rectTrans.GetLocalCorners(localVertices);

            for (int i = 0; i < 4; i++)
            {
                string cornerName = GetCornerName(i);
                Vector3 localVertex = localVertices[i];


                Vector3 worldVertex = rectTrans.TransformPoint(localVertex);

                //Debug.Log($"{cornerName}：");
                //Debug.Log($"  本地模型坐标：{localVertex}");
                ans[0] = Mathf.Min(ans[0], worldVertex.x);
                ans[1] = Mathf.Min(ans[1], worldVertex.y);
                ans[2] = Mathf.Max(ans[0], worldVertex.x);
                ans[3] = Mathf.Max(ans[1], worldVertex.y);

            }


            Debug.Log($"Image Bounds: MinX={ans[0]}, MinY={ans[1]}, Width={ans[2]-ans[0]}, Height={ans[3]-ans[1]}, MaxX={ans[2]}, MaxY={ans[3]}");


        }
        else if (sk != null && sk.Skeleton != null)
        {
            var skeleton = sk.Skeleton;

            float x, y, width, height;
            float[] vertexBuffer = null; // 先给 null，Spine 内部会处理
            skeleton.GetBounds(out x, out y, out width, out height, ref vertexBuffer);

            Debug.Log($"Skeleton Bounds: MinX={x}, MinY={y}, Width={width}, Height={height}, MaxX={x + width}, MaxY={y + height}");

        }
        else if (rectTrans != null)
        {

            Vector3[] worldCorners = new Vector3[4];
            rectTrans.GetWorldCorners(worldCorners);


            Debug.Log($"Skeleton TransformPosition Corner: ");

            foreach (var i in worldCorners)
            {
                print($"{i},");
            }
        }


    }
           private string GetCornerName(int index)
    {
        return index switch
        {
            0 => "左下顶点",
            1 => "左上顶点",
            2 => "右上顶点",
            3 => "右下顶点",
            _ => "未知顶点"
        };
    }
}
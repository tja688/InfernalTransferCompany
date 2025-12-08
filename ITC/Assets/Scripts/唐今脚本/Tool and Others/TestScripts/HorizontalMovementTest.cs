using UnityEngine;

namespace ITC.Tools.Test
{
    /// <summary>
    /// 简单的水平移动测试脚本
    /// 挂载到物体上即可让物体在X轴上移动
    /// </summary>
    public class HorizontalMovementTest : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("移动速度")]
        public float speed = 2.0f;

        [Tooltip("移动模式：PingPong为往返移动，Constant为持续向一个方向移动")]
        public MovementType movementType = MovementType.PingPong;

        [Tooltip("往返移动的距离范围（仅在PingPong模式下有效）")]
        public float range = 3.0f;

        private Vector3 startPos;

        public enum MovementType
        {
            PingPong,   // 往返
            Constant    // 持续
        }

        private void Start()
        {
            startPos = transform.position;
        }

        private void Update()
        {
            switch (movementType)
            {
                case MovementType.PingPong:
                    // 使用Sin函数实现平滑往返
                    float xOffset = Mathf.Sin(Time.time * speed) * range;
                    transform.position = startPos + new Vector3(xOffset, 0, 0);
                    break;

                case MovementType.Constant:
                    // 持续向右移动
                    transform.Translate(Vector3.right * speed * Time.deltaTime);
                    break;
            }
        }

        // 在编辑器中绘制移动范围，方便观察
        private void OnDrawGizmosSelected()
        {
            if (movementType == MovementType.PingPong)
            {
                Gizmos.color = Color.green;
                Vector3 center = Application.isPlaying ? startPos : transform.position;
                Gizmos.DrawLine(center - Vector3.right * range, center + Vector3.right * range);
                Gizmos.DrawWireSphere(center - Vector3.right * range, 0.1f);
                Gizmos.DrawWireSphere(center + Vector3.right * range, 0.1f);
            }
        }
    }
}

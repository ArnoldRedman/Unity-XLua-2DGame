using UnityEngine;

namespace SkierFramework
{
    /// <summary>
    /// 2D 摄像机平滑跟随
    /// 挂在 Main Camera 上，将 target 拖入角色
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("跟随目标（角色 Transform）")]
        public Transform target;

        [Header("跟随参数")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

        [Header("边界限制（可选）")]
        [SerializeField] private bool useBounds;
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPos = target.position + offset;

            if (useBounds)
            {
                desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
                desiredPos.y = Mathf.Clamp(desiredPos.y, minBounds.y, maxBounds.y);
            }

            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 运行时设置跟随目标
        /// </summary>
        public void SetTarget(Transform t)
        {
            target = t;
        }
    }
}

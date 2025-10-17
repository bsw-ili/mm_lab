using UnityEngine;

public class AlignToOriginOnAwake : MonoBehaviour
{
    [ContextMenu("Align")]
    public void Align()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("No Renderer found on " + gameObject.name);
            return;
        }

        // 获取包围盒
        Bounds bounds = rend.bounds;

        // 底部中心点
        Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        // 偏移量（世界坐标）
        Vector3 offset = transform.position - bottomCenter;

        // 移动物体，使底部中心对齐到 (0,0,0)
        transform.position = offset;
    }
}

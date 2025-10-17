using UnityEngine;

public class AlignToOrigin : MonoBehaviour
{
    [ContextMenu("Align To World Origin (Bottom Center)")]
    void AlignToWorldOrigin()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("No Renderer found on this object or its children.");
            return;
        }

        // 获取物体包围盒
        Bounds bounds = rend.bounds;

        // 底部中心点
        Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        // 计算需要平移的偏移量
        Vector3 offset = transform.position - bottomCenter;

        // 把底部中心点移动到 (0,0,0)
        transform.position = offset;
    }
}

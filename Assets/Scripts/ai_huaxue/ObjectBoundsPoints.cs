using UnityEngine;

public class ObjectBoundsPoints : MonoBehaviour
{
    [ContextMenu("获取物体底部和顶部中心点")]
    public void GetBoundsPoints()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("未找到 Renderer 组件！");
            return;
        }

        Bounds bounds = rend.bounds;

        // 底部中心点
        Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        // 顶部中心点
        Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

        Debug.Log($"{gameObject.name} 底部中心点: {bottomCenter}");
        Debug.Log($"{gameObject.name} 顶部中心点: {topCenter}");

        // 在 Scene 视图中画一条红线
        Debug.DrawLine(bottomCenter, topCenter, Color.red, 5f);
    }
}

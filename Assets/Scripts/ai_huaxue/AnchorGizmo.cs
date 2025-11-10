using UnityEngine;

[ExecuteAlways]  // 在编辑模式也可显示
public class AnchorGizmo : MonoBehaviour
{
    public float arrowLength = 0.3f;      // 箭头长度
    public Color arrowColor = Color.green;

    void OnDrawGizmos()
    {
        Gizmos.color = arrowColor;
        // 从锚点位置画一条箭头表示法向量
        Gizmos.DrawRay(transform.position, transform.up * arrowLength);
        DrawArrowHead(transform.position, transform.up, arrowLength * 0.2f);
    }

    // 简单箭头头部
    void DrawArrowHead(Vector3 pos, Vector3 dir, float size)
    {
        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * size;
        Vector3 up = Vector3.Cross(dir, right).normalized * size;

        Gizmos.DrawLine(pos + dir, pos + dir - right + up);
        Gizmos.DrawLine(pos + dir, pos + dir - right - up);
        Gizmos.DrawLine(pos + dir, pos + dir + right + up);
        Gizmos.DrawLine(pos + dir, pos + dir + right - up);
    }
}

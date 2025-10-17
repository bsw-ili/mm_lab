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

        // ��ȡ�����Χ��
        Bounds bounds = rend.bounds;

        // �ײ����ĵ�
        Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        // ������Ҫƽ�Ƶ�ƫ����
        Vector3 offset = transform.position - bottomCenter;

        // �ѵײ����ĵ��ƶ��� (0,0,0)
        transform.position = offset;
    }
}

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

        // ��ȡ��Χ��
        Bounds bounds = rend.bounds;

        // �ײ����ĵ�
        Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        // ƫ�������������꣩
        Vector3 offset = transform.position - bottomCenter;

        // �ƶ����壬ʹ�ײ����Ķ��뵽 (0,0,0)
        transform.position = offset;
    }
}

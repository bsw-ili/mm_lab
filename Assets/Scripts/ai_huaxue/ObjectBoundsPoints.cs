using UnityEngine;

public class ObjectBoundsPoints : MonoBehaviour
{
    [ContextMenu("��ȡ����ײ��Ͷ������ĵ�")]
    public void GetBoundsPoints()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("δ�ҵ� Renderer �����");
            return;
        }

        Bounds bounds = rend.bounds;

        // �ײ����ĵ�
        Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        // �������ĵ�
        Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

        Debug.Log($"{gameObject.name} �ײ����ĵ�: {bottomCenter}");
        Debug.Log($"{gameObject.name} �������ĵ�: {topCenter}");

        // �� Scene ��ͼ�л�һ������
        Debug.DrawLine(bottomCenter, topCenter, Color.red, 5f);
    }
}

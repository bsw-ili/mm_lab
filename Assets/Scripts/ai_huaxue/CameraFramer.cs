using UnityEngine;
using System.Collections.Generic;

public class CameraFramer : MonoBehaviour
{
    public Camera cam;
    public List<Transform> targets = new List<Transform>(); // 组合里的物体

    [Header("相机控制参数")]
    public float padding = 1.2f;        // 物体边界放大比例
    public float smoothTime = 0.3f;     // 平滑时间
    public float pitchAngle = 30f;      // 俯角（往下看），默认30度
    public float yawAngle = 0f;         // 水平环绕角度（绕Y轴旋转）

    private Vector3 velocity = Vector3.zero;

    [ContextMenu("change camera")]
    public void camera_changing()
    {
        if (targets.Count == 0) return;

        Bounds bounds = GetTargetsBounds();
        Vector3 center = bounds.center;

        // 计算组合的大小半径
        float radius = bounds.extents.magnitude * padding;

        // 使用相机FOV计算合适的距离
        float dist = radius / Mathf.Sin(Mathf.Deg2Rad * cam.fieldOfView / 2f);

        // ====== 修改部分 ======
        // 基础方向（相机默认从前方看）
        Vector3 baseDir = Vector3.forward;

        // 先绕Y轴旋转（水平角度）
        Quaternion yawRot = Quaternion.AngleAxis(yawAngle, Vector3.up);
        Vector3 dirWithYaw = yawRot * baseDir;

        // 再绕右手轴旋转（俯角）
        Quaternion pitchRot = Quaternion.AngleAxis(-pitchAngle, Vector3.right);
        Vector3 finalDir = pitchRot * dirWithYaw;

        // 偏移位置
        Vector3 offset = finalDir.normalized * dist;
        Vector3 desiredPos = center + offset;

        // 平滑移动相机
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

        // 看向中心
        transform.LookAt(center);
    }


    Bounds GetTargetsBounds()
    {
        if (targets.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);

        Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
        foreach (Transform t in targets)
        {
            Renderer rend = t.GetComponentInChildren<Renderer>();
            if (rend != null)
                bounds.Encapsulate(rend.bounds);
            else
                bounds.Encapsulate(t.position);
        }
        return bounds;
    }
}

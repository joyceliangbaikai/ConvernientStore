using UnityEngine;

public class PanLookAndCursorArrow : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Transform rig;
    public CameraMover mover;
    public WaypointGraph graph;
    public Waypoint startWaypoint;

    [Header("Arrow Visual")]
    public Transform arrowRoot;        // 空物体，用来收纳箭头
    public GameObject arrowPrefab;     // 你的 Quad prefab
    public float arrowDistance = 1.2f;
    public float arrowHeight = 0.02f;
    public float arrowScale = 0.6f;

    [Header("Raycast Masks")]
    public LayerMask floorMask = ~0;   // ✅ 建议只选 Floor 层
    public float floorRayUp = 5f;
    public float floorRayDown = 50f;
    public float rayDistance = 500f;

    [Header("Look (Right Drag)")]
    public float lookSensitivity = 3f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Aiming")]
    [Range(-1f, 1f)]
    public float aimDotThreshold = 0.25f; // ✅ 越大越“严格”，0.25~0.4 比较舒服

    [Header("Behavior")]
    public bool hideArrowWhileMoving = true; // ✅ 移动/冷却时隐藏箭头（避免你觉得发疯）

    Waypoint current;
    Waypoint aimed;
    GameObject arrowInstance;

    float yaw, pitch;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        if (cam == null || rig == null || mover == null || graph == null || arrowRoot == null || arrowPrefab == null)
        {
            Debug.LogError("[PanLookAndCursorArrow] Missing refs!");
            enabled = false;
            return;
        }

        // 清空 Root 下旧残留
        for (int i = arrowRoot.childCount - 1; i >= 0; i--)
            Destroy(arrowRoot.GetChild(i).gameObject);

        if (graph.all == null || graph.all.Count == 0) graph.Build();

        current = startWaypoint != null ? startWaypoint : (graph.all.Count > 0 ? graph.all[0] : null);
        if (current == null)
        {
            Debug.LogError("[PanLookAndCursorArrow] No start waypoint and graph is empty.");
            enabled = false;
            return;
        }

        rig.position = current.transform.position;

        var e = rig.rotation.eulerAngles;
        pitch = NormalizeAngle(e.x);
        yaw = NormalizeAngle(e.y);

        arrowInstance = Instantiate(arrowPrefab, arrowRoot);
        arrowInstance.name = "CursorArrow";
        arrowInstance.transform.localScale = Vector3.one * arrowScale;

        // ✅ 建议把箭头 prefab 的 Layer 设成 NavArrow（你项目里已有）
        // 这样 floorMask 只选 Floor，就不会射线打到箭头自己
    }

    void Update()
    {
        if (InputLock.Locked) return;
        
        if (current == null) return;

        // ✅ 如果你做了 InputLock（UI打开时锁输入），这里接上：
        // 你按你自己的实现二选一：
        // if (InputLock.IsLocked) { SetArrowVisible(false); return; }
        // 或者：
        // if (InputLock.Instance != null && InputLock.Instance.Locked) { SetArrowVisible(false); return; }

        // 右键拖动旋转
        if (Input.GetMouseButton(1))
            HandleRightDragLook();

        // 移动/冷却中：可选隐藏箭头
        if (hideArrowWhileMoving && mover != null && mover.IsBusy)
        {
            SetArrowVisible(false);
            return;
        }

        UpdateCursorArrow();

        // 左键点击移动：只有“不忙”才允许
        if (Input.GetMouseButtonDown(0))
            TryMoveToAimedNeighbor();
    }

    void UpdateCursorArrow()
    {
        aimed = null;
        if (arrowInstance == null) return;

        Vector3 from = current.transform.position;
        Vector3 dir = GetMouseDirectionOnXZ(from);

        if (dir.sqrMagnitude < 0.0001f)
        {
            SetArrowVisible(false);
            return;
        }

        var neighbors = current.neighbors;
        if (neighbors == null || neighbors.Count == 0)
        {
            SetArrowVisible(false);
            return;
        }

        float bestDot = -1f;
        Waypoint best = null;

        for (int i = 0; i < neighbors.Count; i++)
        {
            var nb = neighbors[i];
            if (nb == null) continue;

            Vector3 d = nb.transform.position - from;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) continue;

            d.Normalize();
            float dot = Vector3.Dot(dir, d);

            if (dot > bestDot)
            {
                bestDot = dot;
                best = nb;
            }
        }

        // ✅ 阈值：不够“对准”就不显示箭头，避免乱选邻居导致跳
        if (best == null || bestDot < aimDotThreshold)
        {
            SetArrowVisible(false);
            return;
        }

        aimed = best;

        Vector3 dirTo = aimed.transform.position - from;
        dirTo.y = 0f;
        if (dirTo.sqrMagnitude < 0.0001f)
        {
            SetArrowVisible(false);
            return;
        }
        dirTo.Normalize();

        Vector3 pos = from + dirTo * arrowDistance;
        pos.y = SampleFloorY(pos) + arrowHeight;

        SetArrowVisible(true);
        arrowInstance.transform.position = pos;
        arrowInstance.transform.rotation = Quaternion.LookRotation(dirTo, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        arrowInstance.transform.localScale = Vector3.one * arrowScale;
    }

    Vector3 GetMouseDirectionOnXZ(Vector3 from)
    {
        if (TryGetMouseFloorPoint(out var hitPoint))
        {
            Vector3 v = hitPoint - from;
            v.y = 0f;
            if (v.sqrMagnitude > 0.0001f) return v.normalized;
        }

        Vector3 f = cam.transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude > 0.0001f) return f.normalized;

        return Vector3.zero;
    }

    void TryMoveToAimedNeighbor()
    {
        if (aimed == null) return;
        if (mover == null) return;
        if (mover.IsBusy) return; // ✅ 防连点（移动中+冷却中都禁）

        mover.MoveTo(aimed.transform);
        current = aimed;
    }

    void HandleRightDragLook()
    {
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw -= mx * lookSensitivity;
        pitch += my * lookSensitivity;

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        rig.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    bool TryGetMouseFloorPoint(out Vector3 hitPoint)
    {
        hitPoint = default;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // ✅ 关键：floorMask 只选 Floor，别把 NavArrow / Item 之类选进来
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, floorMask, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;
            return true;
        }
        return false;
    }

    float SampleFloorY(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * floorRayUp;
        if (Physics.Raycast(origin, Vector3.down, out var hit, floorRayUp + floorRayDown, floorMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        return pos.y;
    }

    void SetArrowVisible(bool v)
    {
        if (arrowInstance != null && arrowInstance.activeSelf != v)
            arrowInstance.SetActive(v);
    }

    static float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}

using UnityEngine;

public class PanLookAndCursorArrow : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // Main Camera
    public Transform rig;              // CameraRig
    public CameraMover mover;          // CameraMover (rig指向CameraRig)
    public WaypointGraph graph;        // WaypointGraph
    public Waypoint startWaypoint;     // WP_01

    [Header("Arrow Visual")]
    public Transform arrowRoot;        // NavArrowRoot (空物体)
    public GameObject arrowPrefab;     // 你的 Quad prefab
    public float arrowDistance = 1.2f;
    public float arrowHeight = 0.02f;
    public float arrowScale = 0.6f;

    [Header("Raycast Masks")]
    public LayerMask floorMask = ~0;   // 先用 Everything 排错
    public float floorRayUp = 5f;
    public float floorRayDown = 50f;
    public float rayDistance = 500f;

    [Header("Look (Right Drag)")]
    public float lookSensitivity = 3f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    Waypoint current;
    Waypoint aimed;          // 当前鼠标方向选中的邻居
    GameObject arrowInstance;

    float yaw, pitch;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        if (cam == null || rig == null || mover == null || graph == null || arrowRoot == null || arrowPrefab == null)
        {
            Debug.LogError("[PanLookAndCursorArrow] Missing refs! cam/rig/mover/graph/arrowRoot/arrowPrefab must be set.");
            enabled = false;
            return;
        }

        // 清空 Root 下旧残留（避免你场景里留了一堆 clone）
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

        // 开局对齐
        rig.position = current.transform.position;

        // 初始化视角
        var e = rig.rotation.eulerAngles;
        pitch = NormalizeAngle(e.x);
        yaw = NormalizeAngle(e.y);

        // 只生成一个箭头
        arrowInstance = Instantiate(arrowPrefab, arrowRoot);
        arrowInstance.name = "CursorArrow";
        arrowInstance.transform.localScale = Vector3.one * arrowScale;
    }

    void Update()
    {
        if (current == null) return;

        // 右键拖动旋转（不影响左键点击移动）
        if (Input.GetMouseButton(1))
            HandleRightDragLook();

        // 每帧更新箭头：你右键转视角时也会更新，不需要停下
        UpdateCursorArrow();

        // 左键点击移动（不再用“拖拽阈值”那套，因为旋转改成右键了）
        if (Input.GetMouseButtonDown(0))
            TryMoveToAimedNeighbor();
    }

    void UpdateCursorArrow()
    {
        aimed = null;
        if (arrowInstance == null) return;

        // 取“指向方向”：优先用鼠标在地面落点；打不到地面就用相机 forward
        Vector3 from = current.transform.position;
        Vector3 dir = GetMouseDirectionOnXZ(from);

        if (dir.sqrMagnitude < 0.0001f)
        {
            arrowInstance.SetActive(false);
            return;
        }

        // 从邻居里选最接近该方向的那个
        var neighbors = current.neighbors;
        if (neighbors == null || neighbors.Count == 0)
        {
            arrowInstance.SetActive(false);
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

        if (best == null)
        {
            arrowInstance.SetActive(false);
            return;
        }

        aimed = best;

        // 箭头放在“从当前点朝 aimed 方向”一小段距离的位置
        Vector3 dirTo = aimed.transform.position - from;
        dirTo.y = 0f;
        if (dirTo.sqrMagnitude < 0.0001f)
        {
            arrowInstance.SetActive(false);
            return;
        }
        dirTo.Normalize();

        Vector3 pos = from + dirTo * arrowDistance;
        pos.y = SampleFloorY(pos) + arrowHeight;

        arrowInstance.SetActive(true);
        arrowInstance.transform.position = pos;
        arrowInstance.transform.rotation = Quaternion.LookRotation(dirTo, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        arrowInstance.transform.localScale = Vector3.one * arrowScale;
    }

    // 鼠标指向方向（XZ 平面）
    Vector3 GetMouseDirectionOnXZ(Vector3 from)
    {
        // 1) 先用鼠标射线打地面
        if (TryGetMouseFloorPoint(out var hitPoint))
        {
            Vector3 v = hitPoint - from;
            v.y = 0f;
            if (v.sqrMagnitude > 0.0001f) return v.normalized;
        }

        // 2) 打不到地面就用相机 forward 投影到 XZ
        Vector3 f = cam.transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude > 0.0001f) return f.normalized;

        return Vector3.zero;
    }

    void TryMoveToAimedNeighbor()
    {
        if (aimed == null) return;

        mover.MoveTo(aimed.transform);
        current = aimed;
    }

    void HandleRightDragLook()
    {
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        // 你要“抓画面”那种手感：反向
        yaw -= mx * lookSensitivity;
        pitch += my * lookSensitivity;

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        rig.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    bool TryGetMouseFloorPoint(out Vector3 hitPoint)
    {
        hitPoint = default;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

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

    static float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
}

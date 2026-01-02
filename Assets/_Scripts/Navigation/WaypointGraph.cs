using System.Collections.Generic;
using UnityEngine;

public class WaypointGraph : MonoBehaviour
{
    public float stepDistance = 4f;   // 一格多远
    public float tolerance = 1f;     // 容差（点没摆很准也没事）
    public Transform waypointsRoot;     // 你的 WP 父物体（拖进来）
    public bool useLineOfSight = false; // 先关掉，后面再加墙体遮挡检测

    [HideInInspector] public List<Waypoint> all = new List<Waypoint>();

    void Awake()
    {
        Build();
    }

    [ContextMenu("Build Neighbors")]
    public void Build()
    {
        all.Clear();
        if (waypointsRoot == null) return;

        foreach (Transform t in waypointsRoot)
        {
            var wp = t.GetComponent<Waypoint>();
            if (wp != null) all.Add(wp);
        }

        foreach (var a in all)
        {
            a.neighbors.Clear();
            foreach (var b in all)
            {
                if (a == b) continue;

                float d = Vector3.Distance(a.transform.position, b.transform.position);
                if (Mathf.Abs(d - stepDistance) <= tolerance)
                {
                    a.neighbors.Add(b);
                }
            }
        }
    }
}

using System;
using UnityEngine;

public class NavArrow : MonoBehaviour
{
    [HideInInspector] public Waypoint target;
    [HideInInspector] public Action<Waypoint> onClick;

    void OnMouseDown()
    {
        if (target != null) onClick?.Invoke(target);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [HideInInspector]
    public List<Waypoint> neighbors = new List<Waypoint>();
}

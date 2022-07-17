using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawRayCast : MonoBehaviour
{
    public float TimeToLive;
    public LineRenderer myLineRenderer;

    void Awake()
    {
        myLineRenderer = gameObject.GetComponent<LineRenderer>();
        Destroy(gameObject,TimeToLive);
    }
}

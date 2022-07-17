using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform followTarget;
    public float CameraMoveSpeed = 5f;

    void Update()
    {
        if(followTarget!=null)
        {
            transform.position = Vector3.Lerp(transform.position,new Vector3(followTarget.transform.position.x,followTarget.transform.position.y,transform.position.z),CameraMoveSpeed*Time.deltaTime);
        }        
    }
}

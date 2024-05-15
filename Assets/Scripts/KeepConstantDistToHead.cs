using System;
using UnityEngine;

public class KeepConstantDistToTarget : MonoBehaviour
{
    public Transform head;
    public float distance;

    private void Start()
    {
        head = ProtocolManager.userHead;
    }

    void LateUpdate()
    {
        transform.position = head.position - (head.position - transform.position).normalized * distance;
    }
}

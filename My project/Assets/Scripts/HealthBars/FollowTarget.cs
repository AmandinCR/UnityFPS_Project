using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] private Transform Target;
    [SerializeField] private Vector3 Offset;
    [SerializeField] private bool worldCanvas = false;
    [SerializeField] private GameObject holder;

    private void LateUpdate()
    {
        if (worldCanvas)
        {
            if (Target != null)
                transform.position = Target.position + Offset;
        }
        else
        {
            if (Target != null)
            {
                Vector3 direction = (Target.position - Camera.main.transform.position).normalized;
                bool isBehind = Vector3.Dot(direction, Camera.main.transform.forward) <= 0.0f;
                holder.SetActive(!isBehind);
                transform.position = Camera.main.WorldToScreenPoint(Target.position + Offset);
            }
            
        }  
    }
}

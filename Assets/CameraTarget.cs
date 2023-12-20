using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform target;  // The target object for the camera to follow
    public float smoothSpeed = 0.125f;  // The speed at which the camera follows the target
    public Vector3 offset;  // The offset from the target position

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera target not set!");
            return;
        }

        // Calculate the desired position for the camera
        Vector3 desiredPosition = target.position + offset;

        // Use Mathf.SmoothDamp to smoothly interpolate between the current position and the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Update the camera's position
        transform.position = smoothedPosition;

        // Make the camera look at the target
        transform.LookAt(target);
    }
}

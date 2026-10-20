
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class CarFocusCamera : MonoBehaviour
{
    public CarTcpServer server;
    public Transform[] targets = new Transform[5];

    public float distance = 2.5f;
    public float height = 1.2f;
    public float smoothSpeed = 5f;
    public Vector3[] cameraOffset;
    public int focusIndex = 0;

    [Space(20)]
    public Transform car;

    public Vector3 offset = new Vector3(0f, 5f, -8f);
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;
    void LateUpdate()
    {
        if (server == null || targets.Length < 5)
            return;

        NetworkMessage cmd = server.GetCommand();

        if (cmd == null || targets[cmd.focus] == null)
            return;
        
        if(focusIndex >= 0 && focusIndex <= 3)
        {
            Transform target = targets[cmd.focus];

            Vector3 desiredPosition =
                target.position + cameraOffset[focusIndex]
                - target.forward * distance
                + Vector3.up * height;

            transform.position = Vector3.Lerp(
                (transform.position),
                desiredPosition,
                Time.deltaTime * smoothSpeed
            );

            Quaternion desiredRotation = Quaternion.LookRotation(
                target.position - transform.position,
                Vector3.up
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                Time.deltaTime * smoothSpeed
            );
        }
        else
        {
            if (car == null)
                return;

            // Calculate the desired camera position
            Vector3 desiredPosition = car.TransformPoint(offset);

            // Smoothly move the camera
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );

            // Look at the car
            Quaternion desiredRotation = Quaternion.LookRotation(
                car.position - transform.position
            );

            // Smoothly rotate the camera
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}


using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class CarPhysicsController : MonoBehaviour
{
    public CarTcpServer server;

    public Rigidbody rb;

    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    public Transform steeringWheel;
    public Transform gearSelector;

    public float maxSteerAngle = 30f;
    public float motorTorque = 800f;
    public float brakeTorque = 1500f;

    private float steeringWheelAngle;
   /* [Space(50)]
    public float steer  ;
    public float throttle  ;
    public float brake ;
    public float direction;
    public float torque;
    public float braking;*/
    void FixedUpdate()
    {
        if (server == null || rb == null) return;

        NetworkMessage cmd = server.GetCommand();
        if (cmd == null) return;

        float steer = cmd.steer;   
        float throttle = cmd.throttle;
        float brake = cmd.brake;

        frontLeft.steerAngle = steer * maxSteerAngle;
        frontRight.steerAngle = steer * maxSteerAngle;

        float direction = cmd.gear == -1 ? -1f :
                           cmd.gear == 1 ? 1f : 0f;

        float torque = throttle * motorTorque * direction;

        rearLeft.motorTorque = torque;
        rearRight.motorTorque = torque;

        float braking = brake * brakeTorque;

        frontLeft.brakeTorque = braking;
        frontRight.brakeTorque = braking;
        rearLeft.brakeTorque = braking;
        rearRight.brakeTorque = braking;

        steeringWheelAngle =
            Mathf.Lerp(steeringWheelAngle, -steer * 450f,
                       Time.fixedDeltaTime * 8f);

        if (steeringWheel != null)
            steeringWheel.localRotation =
                Quaternion.Euler(0f,0f, steeringWheelAngle);

        UpdateWheel(frontLeft, frontLeftMesh);
        UpdateWheel(frontRight, frontRightMesh);
        UpdateWheel(rearLeft, rearLeftMesh);
        UpdateWheel(rearRight, rearRightMesh);
    }

    void UpdateWheel(WheelCollider wheel, Transform mesh)
    {
        if (wheel == null || mesh == null) return;

        wheel.GetWorldPose(out Vector3 position,
                           out Quaternion rotation);

        mesh.SetPositionAndRotation(position, rotation);
    }

    public float SpeedKmh =>
        rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
}

using System;

[Serializable]
public class NetworkMessage
{
    public float steer;
    public float throttle;
    public float brake;
    public int gear;
    public int focus;

    public float speed;
    public bool connected;
}
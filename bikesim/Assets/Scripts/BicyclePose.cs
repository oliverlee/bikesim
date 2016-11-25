public class BicyclePose {
    public float x;
    public float y;
    public float pitch;
    public float yaw;
    public float roll;
    public float steer;
    public float rear_wheel;
    public float v;
    public byte timestamp;

    public void SetFromByteArray(byte[] buffer) {
        x = System.BitConverter.ToSingle(buffer, 0);
        y = System.BitConverter.ToSingle(buffer, 4);
        pitch = System.BitConverter.ToSingle(buffer, 8);
        yaw = System.BitConverter.ToSingle(buffer, 12);
        roll = System.BitConverter.ToSingle(buffer, 16);
        steer = System.BitConverter.ToSingle(buffer, 20);
        rear_wheel = System.BitConverter.ToSingle(buffer, 24);
        v = System.BitConverter.ToSingle(buffer, 28);
        timestamp = buffer[32];
    }
}


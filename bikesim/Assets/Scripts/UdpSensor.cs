using System;
using System.Diagnostics;
using System.Xml;
using System.IO;


public class Sensor {
    public double steerAngle, steerRate, wheelRate;
    public long timestamp_ms;
    public Sensor() : this(0.0, 0.0, 0.0, 0) { }
    public Sensor(double delta, double deltad, double thetad, long ts) {
        steerAngle = delta;
        steerRate = deltad;
        wheelRate = thetad;
        timestamp_ms = ts;
    }
    public void Update(double delta, double deltad, double thetad, long ts) {
        steerAngle = delta;
        steerRate = deltad;
        wheelRate = thetad;
        timestamp_ms = ts;
    }
}


public class UdpSensor {
    private Sensor _sensor;
    private UdpThread _udp;

    public UdpSensor(Stopwatch stopwatch, Int32 port = 9900) {
        _sensor = new Sensor();
        _udp = new UdpThread(stopwatch, port);
    }

    public void Start() {
        UnityEngine.Debug.Log(String.Format(
                    "udp server listening on port {0}", port));
        _udp.Start();
        _udp.StartReceiveData(UpdateSensor);
    }

    public void Stop() {
        _udp.StopReceiveData();
        _udp.Stop();
    }

    public Int32 port {
        get { return _udp.port; }
    }

    public Sensor sensor {
        get { return _sensor; }
    }

    private void UpdateSensor(byte[] b) {
        // Note: sizeof(char) = 2 (Unicode)
        if ((b[0] == UdpThread.packetPrefix) &&
                (b[sizeof(byte) + 2*sizeof(float)] == UdpThread.packetSuffix)) {
            _sensor.timestamp_ms = _udp.ElapsedMilliseconds();
            _sensor.steerAngle = BitConverter.ToSingle(b, sizeof(byte));
            _sensor.steerRate = BitConverter.ToSingle(b,
                    sizeof(byte) + sizeof(float));
            _sensor.wheelRate = -13.3333f; // 4m/s * TODO: remove hardcoded wheel rate
        }
    }
}

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

    private void UpdateSensor(string s) {
        _sensor.timestamp_ms = _udp.ElapsedMilliseconds();
        using (XmlReader reader = XmlReader.Create(new StringReader(s))) {
            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.Element) {
                    if (reader.Name == "delta") {
                        reader.Read(); // get next node with content
                        _sensor.steerAngle = Convert.ToSingle(reader.Value);
                    }
                    if (reader.Name == "deltad") {
                        reader.Read();
                        _sensor.steerRate = Convert.ToSingle(reader.Value);
                    }
                    if (reader.Name == "cadence") {
                        reader.Read();
                        _sensor.wheelRate = -Convert.ToSingle(reader.Value);
                        _sensor.wheelRate = -20.0; // TODO: remove hardcoded wheel rate
                    }
                    // TODO: incorporate brake signal
                }
            }
        }
    }
}

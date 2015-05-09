using System;
using System.Xml;
using System.IO;
using UnityEngine;


public class UdpSensor {
    private Sensor _sensor;
    private UdpThread _udp;

    public UdpSensor(Int32 port = 9900) {
        _sensor = new Sensor();
        _udp = new UdpThread(port);
    }

    public void Start() {
        Debug.Log(String.Format("udp server listening on port {0}", port));
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
                    }
                    // TODO: incorporate brake signal
                }
            }
            Debug.Log(String.Format("sensor {0} {1}", _sensor.steerAngle, _sensor.wheelRate));
        }
    }
}

using System;
using System.Diagnostics;
using System.Xml;
using System.Text;
using System.IO;


public class Actuator {
    public double envTorque; // torque the "environment" returns to user
    public long timestamp_ms;
    public Actuator() : this(0.0, 0) { }
    public Actuator(double tau, long ts) {
        envTorque = tau;
        timestamp_ms = ts;
    }
}


public class UdpActuator {
    private Actuator _actuator;
    private UdpThread _udp;

    public UdpActuator(Stopwatch stopwatch, Int32 port = 9901) {
        _actuator = new Actuator();
        _udp = new UdpThread(stopwatch, port);
    }

    public void Start() {
        UnityEngine.Debug.Log(String.Format(
                    "udp client broadcasting on port {0}", port));
        _udp.Start();
    }

    public void Stop() {
        SetTorque(0.0);
        _udp.Stop();
    }

    public Int32 port {
        get { return _udp.port; }
    }

    public Actuator actuator {
        get { return _actuator; }
    }

    public double torque {
        get { return _actuator.envTorque; }
    }

    public void SetTorque(double tau) {
        _actuator.envTorque = tau;
        _actuator.timestamp_ms= _udp.ElapsedMilliseconds();

        // Note: sizeof(char) = 2 (Unicode)
        byte[] data = new byte[2*sizeof(byte) + sizeof(float)];
        data[0] = UdpThread.packetPrefix;
        data[sizeof(byte) + sizeof(float)] = UdpThread.packetSuffix;

        byte[] torque = BitConverter.GetBytes(Convert.ToSingle(tau));
        Buffer.BlockCopy(torque, 0, data, sizeof(byte), sizeof(float));
        _udp.TransmitData(data);
    }
}

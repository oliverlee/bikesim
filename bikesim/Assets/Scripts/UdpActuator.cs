using System;
using System.Diagnostics;
using System.Xml;
using System.Text;
using System.IO;


public class Actuator {
    public double torque; // torque the "environment" returns to user
    // last sent model state
    public State state;
    public long timestamp_ms;
    public Actuator() : this(0.0, 0) { }
    public Actuator(double torque, long ts) {
        this.torque = torque;
        this.state = new State();
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
        SendTorque(0.0);
        _udp.Stop();
    }

    public Int32 port {
        get { return _udp.port; }
    }

    public Actuator actuator {
        get { return _actuator; }
    }

    public State state {
        get { return _actuator.state; }
    }

    public double torque {
        get { return _actuator.torque; }
    }

    private void SendTorque(double tau) {
        SendTorque(tau, new State());
    }

    public void SendTorque(double tau, State state) {
        _actuator.torque = tau;
        _actuator.state = state;
        _actuator.timestamp_ms= _udp.ElapsedMilliseconds();

        // Note: sizeof(char) = 2 (Unicode)
        int packetSize = 2*sizeof(byte) + 2*sizeof(float);
        byte[] data = new byte[packetSize];
        data[0] = UdpThread.packetPrefix;
        data[packetSize - 1] = UdpThread.packetSuffix;

        byte[] floatBytes =
            BitConverter.GetBytes(Convert.ToSingle(_actuator.torque));
        Buffer.BlockCopy(floatBytes, 0, data, sizeof(byte), sizeof(float));
        floatBytes =
            BitConverter.GetBytes(Convert.ToSingle(_actuator.state.lean));
        Buffer.BlockCopy(floatBytes, 0, data, sizeof(byte) + sizeof(float),
                sizeof(float));
        _udp.TransmitData(data);
    }
}

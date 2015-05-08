using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.Text;
using System.IO;


public class Actuator {
    public double envTorque; // torque the "environment" returns to user
    public Actuator() : this(0.0) { }
    public Actuator(double tau) {
        envTorque = tau;
    }
}


public class UdpActuator {
    private Actuator _actuator;
    private UdpThread _udp;

    public UdpActuator(Int32 port = 9901) {
        _actuator = new Actuator();
        _udp(port);
    }

    public void Start() {
        Debug.Log(String.Format("udp client broadcasting on port {0}", port));
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
        _udp.TransmitData(XmlDatagram(tau));
    }

    private string XmlDatagram(double tau) {
        StringBuilder sb = new StringBuilder();
        using (XmlWriter writer = XmlWriter.Create(sb)) {
            writer.WriteStartDocument();
            writer.WriteStartElement("root");
            writer.WriteStartElement("torque");
            writer.WriteString(tau.ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }
        return sb.ToString();
    }
}

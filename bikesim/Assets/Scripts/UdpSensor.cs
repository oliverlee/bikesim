using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Xml;
using System.IO;
using UnityEngine;


public class UdpSensor {
    private Thread _thread;
    private UdpClient _client;
    private IPEndPoint _endpoint;
    private Sensor _sensor;
    private bool _active;
    private Int32 _socketTimeout = 200; // in ms

    public Int32 port;

    public UdpSensor(Int32 port = 9900) {
        this.port = port;
        _thread = null;
        _client = null;
        _sensor = new Sensor();
    }

    ~UdpSensor() {
        if (_client != null) {
            _client.Close();
            _client = null;
        }
        
        if (_thread != null) {
            _thread.Abort();
            _thread = null;
        }
    }

    public void Start() {
        Debug.Log(String.Format("udp server listening on port {0}", port));
        _active = true;
        _client = null;
        _endpoint = new IPEndPoint(IPAddress.Any, port);
        _thread = new Thread(new ThreadStart(ReceiveData));
        _thread.Name = "UdpSensorThread";
        _thread.IsBackground = true;
        _thread.Start();
    }

    public void Stop() {
        _active = false;
        if (_client != null) {
            _client.Close();
            _client = null;
        }
        _thread.Abort();
        if (_thread.Join(_socketTimeout)) {
            _thread = null;
        }
    }

    public Sensor sensor {
        get { return _sensor; }
    }

    private void ReceiveData() {
        byte[] buffer;
        string data;

        _client = new UdpClient(_endpoint);
        while (_active) {
            try {
                buffer = _client.Receive(ref _endpoint);
            }
            catch (SocketException) {
                break;
            }
            data = Encoding.UTF8.GetString(buffer);
            UpdateSensor(data);
        }
        _client.Close();
    }

    private class UdpState {
        public UdpClient u;
        public IPEndPoint e;
        UdpState(UdpClient u, IPEndPoint e) {
            this.u = u;
            this.e = e;
        }
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
                        _sensor.wheelRate = Convert.ToSingle(reader.Value);
                    }
                    // TODO: incorporate brake signal
                }
            }
            Debug.Log(String.Format("sensor {0} {1}", _sensor.steerAngle, _sensor.wheelRate));
        }
    }
}

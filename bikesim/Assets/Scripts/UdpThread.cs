using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;


public class UdpThread {
    private Thread _thread;
    private string _threadname;
    private UdpClient _client;
    private IPEndPoint _endpoint;
    private bool _active;
    private Int32 _socketTimeout = 200; // in ms

    public Int32 port;

    public UdpThread(Int32 port, string name = "") {
        this.port = port;
        _thread = null;
        _client = null;
        _threadname = name;
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
        _active = true;
        _client = null;
        _endpoint = new IPEndPoint("localhost", port);
        _thread = new Thread(new
                ParameterizedThreadStart(ReceiveDataThreadFunc));
        _thread.Name = _threadname;
        _thread.IsBackground = true;
    }

    public void Stop() {
        _active = false;
        StopClient();
        _thread.Abort();
        if (_thread.Join(_socketTimeout)) {
            _thread = null;
        }
    }

    private void StopClient() {
        if (_client != null) {
            _client.Close();
            _client = null;
        }
    }

    public void TransmitData(string data) {
        if (!_active) {
            return;
        }
        if (_client == null) {
            _client = new UdpClient();
        }

        byte[] buffer = Encoding.UTF8.GetBytes(data);
        _client.Send(buffer, buffer.Length, _endpoint);
    }

    public void StartReceiveData(Func<string, void> receiveFunc) {
        _thread.Start(receiveFunc); // start thread and pass function argument
    }

    public void StopReceiveData() {
        StopClient();
    }

    private void ReceiveDataThreadFunc(Func<string, void> receiveFunc) {
        byte[] buffer;

        if (!_active) {
            return;
        }

        if (_client == null) {
            _client = new UdpClient();
        }
        while (_active) {
            try {
                // The Receive call is blocking but will terminate and throw
                // a SocketException if the client is closed.
                buffer = _client.Receive(ref _endpoint);
            }
            catch (SocketException) {
                break;
            }
            processDataF(Encoding.UTF8.GetString(buffer));
        }
    }
}

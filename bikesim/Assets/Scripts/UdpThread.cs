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
    private Int32 _port;

    public UdpThread(Int32 port, string name = "") {
        _port = port;
        _thread = null;
        _client = null;
        _threadname = name;
    }

    ~UdpThread() {
        if (_client != null) {
            _client.Close();
            _client = null;
        }
        if (_thread != null) {
            _thread.Abort();
            _thread = null;
        }
    }

    public Int32 port {
        get { return _port; }
    }

    public void Start() {
        _active = true;
        _client = null;
        _endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port);
        _thread = new Thread(new
                ParameterizedThreadStart(ReceiveDataThreadFunc));
        _thread.Name = _threadname;
        _thread.IsBackground = true;
    }

    public void Stop() {
        _active = false;
        StopClient();
        if ((_thread.ThreadState &
             (ThreadState.Stopped | ThreadState.Unstarted)) != 0) {
            _thread = null;
            return;
        }
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

    public void StartReceiveData(Action<string> receiveFunc) {
        _thread.Start(receiveFunc); // start thread and pass function argument
    }

    public void StopReceiveData() {
        StopClient();
    }

    private void ReceiveDataThreadFunc(Object obj) {
        byte[] buffer;

        if (!_active) {
            return;
        }

        Action<string> receiveFunc = (Action<string>)obj;
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
            receiveFunc(Encoding.UTF8.GetString(buffer));
        }
    }
}

using System;
using System.Threading;
using System.IO.Ports;


public class SerialThread {
    private string _portname;
    private Int32 _baudrate;

    private const int _read_timeout = 1; // milliseconds
    private const int _write_timeout = 1; // milliseconds

    private SerialPort _port;

    private Thread _thread;
    private const string _threadname = "serial";
    private bool _should_terminate;

    // For phobos::flimnap, a packet consists of a pose_t, framed using COBS.
    // A pose_t is a packed struct of 29 bytes + 1 byte overhead from COBS + 1
    // byte delimiter = 31 bytes total
    private byte[64] _buffer_one;
    private byte[64] _buffer_two;
    private int _buffer_offset;
    private bool _buffer_one_active;

    public SerialThread(string portname, Int32 baudrate) {
        _portname = portname;
        _baudrate = baudrate;

        _port = null;
        _thread = null;
        _should_terminate = false;

        _buffer_one = new byte[64];
        _buffer_two = new byte[64];
        _buffer_offset = 0;
        _buffer_one_active = true;
    }

    public string portname {
        get { return _portname; }
    }

    public Int32 baudrate {
        get { return _baudrate; }
    }

    private byte[] ActiveReadBuffer() {
        if (_buffer_one_active) {
            return _buffer_one;
        }
        return _buffer_two;
    }

    private void OpenPort() {
        _port = new SerialPort(_portname, _baudrate);
        _port.ReadTimeout = _read_timeout;
        _port.WriteTimeout = _write_timeout;
        _port.Open();
    }

    private void ClosePort() {
        if (_port != null) {
            _port.Close();
            _port = null;
        }
    }

    public void Start() {
        OpenPort();
        _thread = new Thread(new ThreadStart(ThreadFunction));
        _thread.Name = _threadname;
        _thread.IsBackground = true;
        _thread.Start();
    }

    public void Stop() {
        _should_terminate = true;
        if (_thread == null) {
            return;
        }

        ClosePort();
        if ((_thread.ThreadState &
             (ThreadState.Stopped | ThreadState.Unstarted)) != 0) {
            _thread = null;
            return;
        }
        _thread.Abort();
        if (_thread.Join(100)) { // milliseconds
            _thread = null;
        }
    }

    private void SwapByteBuffer(byte[] current_buffer, int buffer_offset, int buffer_length, byte[] next_buffer) {
        Buffer.BlockCopy(current_buffer, buffer_offset, next_buffer, 0, buffer_length);
        _active_buffer = next_buffer;
        _inactive_buffer = current_buffer;
        _buffer_offset = buffer_length;
    }

    private void ThreadFunction() {
        _should_terminate = false;

        while (!_should_terminate) {
            if (!_port.IsOpen) {
                OpenPort();
            } else {
                if (_port.BytesToRead > 0) {
                    int bytes_read = _port.Read(_active_buffer, _buffer_offset, _port.BytesToRead);
                    if (bytes_read > 0) {
                        int delimiter_index = Array.IndexOf(_active_buffer, 0, _buffer_offset, bytes_read);
                        if (delimiter_index >= 0) {
                            while (delimiter_index >= 0) {
                                UnstuffPacket(_active_buffer, _buffer_offset, delimiter_index - _buffer_offset);

                                // Continue to process received bytes as long as another packet exists
                                bytes_read -= (delimited_index - _buffer_offset + 1);
                                _buffer_offset = delimiter_index + 1;
                                delimiter_index = Array.IndexOf(_active_buffer, 0, _buffer_offset, bytes_read);
                            }
                            SwapByteBuffer(_active_buffer, _buffer_offset, bytes_read, _inactive_buffer);
                        } else {
                            _buffer_offset += bytes_read;
                        }
                    }

                }
            }
            //Thread.Sleep(10); // milliseconds
            Thread.Yield();
        }
    }

    // Frame unstuff function here.
    // TODO: Move framing (and serialization) code to separate files
    private void UnstuffPacket(byte[] buffer, int buffer_offset, int byte_size) {
        //
    }
}

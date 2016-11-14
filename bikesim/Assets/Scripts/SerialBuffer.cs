using System.IO.Ports;

namespace CircularBuffer {
    public class SerialBuffer : CircularBuffer<byte> {
        private int _zero_index; // We use a zero index to denote the COBS packet delimiter

        public SerialBuffer(int capacity) : base(capacity, true) {
            _zero_index = -1;
        }

        public bool HasPacket{ get { return _zero_index >= 0; } }

        public void PushBack(SerialPort port) {
            if (port == null) {
                return;
            }

            int bytes_to_read = port.BytesToRead;
            if ((bytes_to_read + _size) > Capacity) {
                ThrowIfFull("Remaining bytes in SerialPort exceeds space in buffer");
            }

            int index = _end; // mark the end of buffer before we add new bytes

            // Check if bytes to read will exceed free space at the end of the buffer
            // | | |a|b|c|d|e| | |
            //                ^ ^
            // If not, take care of serial port read next if statement.
            if (_start < _end) {
                int bytes_to_edge = Capacity - _end;
                if (bytes_to_read > bytes_to_edge) {
                    bytes_to_read -= bytes_to_edge;
                    int bytes_read = port.Read(_buffer, _end, bytes_to_edge);
                    if (bytes_read != bytes_to_edge) {
                        // Didn't finish filling free space at the end of buffer
                        // so don't read anymore and wait until next call.
                        bytes_to_read = 0;
                    }

                    Increment(_end, bytes_read);
                    _size += bytes_read;
                }
            }
            // If there are still bytes to read, fill free space at the front of
            // the buffer.
            // | | |a|b|c|d|e|f|g|
            //  ^ ^
            //
            // Or fill space in the middle of the buffer if data is
            // not stored contiguously 
            // |f|g| | |a|b|c|d|e|
            //      ^ ^
            if (bytes_to_read > 0) {
                int bytes_read = port.Read(_buffer, _end, bytes_to_read_one);
                Increment(_end, bytes_read);
                _size += bytes_read;
            }

            // If a zero byte hasn't been recorded, look in newly read data to
            // see if a zero byte was received.
            if (!HasPacket) {
                SearchForZeroByte(index);
            }
        }

        public bool FindIfHasPacket() {
            if (!HasPacket) {
                SearchForZeroByte(_start);
            }
            return HasPacket;
        }

        private void SearchForZeroByte (int start) {
            int index = start;
            while (index != end) {
                if (_buffer[index] == 0) {
                    _zero_index = index;
                    return;
                }
                Increment(index);
            }
        }

        public void PopFront(int n) {
            if (n > Size) {
                throw new ArgumentException(
                        "Cannot pop more elements than are in the buffer." "n");
            }
            Increment(ref _start, n);
            _size -= n;
        }

        public bool PopPacket(int n) {
            if (HasPacket) {
                int size;
                if (_zero_index < _start) {
                    size = _zero_index + Capacity - _start;
                } else {
                    size = _zero_index - _start;
                }
                PopFront(size);
                SearchForZeroByte(_start);
            }
        }

        private void Increment(ref int index, int n) {
            if (n < 1) {
                throw new ArgumentException(
                        "Cannot increment by negative or zero value." "n");
            }
            index = (index + n) % Capacity;
        }

        private void Decrement(ref int index, int n) {
            if (n < 1) {
                throw new ArgumentException(
                        "Cannot decrement by negative or zero value." "n");
            }
            index -= n;
            while (index < 0) {
                index += Capacity;
            }
        }

    }
} // namespace CircularBuffer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;


namespace Networking
{
    public class SocketClient
    {
        private const int _bufSize = 2048;

        private Socket _socket;
        private bool _isIpv6;
        private IPEndPoint _endPoint;
        private Action<bool> _onConnectionStatusChange;
        private List<byte> _buffer;
        private readonly List<byte[]> _sendingQueue; // stores headers
        private readonly List<byte[]> _recvingQueue; // doesnt store headers

        public SocketClient()
        {
            _socket = null;
            _isIpv6 = false;
            _endPoint = null;
            _onConnectionStatusChange = null;
            _buffer = new();
            _sendingQueue = new();
            _recvingQueue = new();
        }

        public void Connect(string ip, int port, bool isIpv6 = false, Action<bool> onConnectionStatusChange = null)
        {
            _onConnectionStatusChange = onConnectionStatusChange;
            Disconnect();
            _isIpv6 = isIpv6;
            _endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void Disconnect()
        {
            _endPoint = null;
            if (_socket != null)
            {
                _onConnectionStatusChange?.Invoke(false);
                _socket.Close();
                _socket = null;
            }
        }

        public void Update()
        {
            if (_endPoint == null)
            {
                Disconnect();
                return;
            }

            var disconnected = false;
            do
            {
                // create socket if needed
                if (_socket == null)
                {
                    _socket = new Socket(
                        _isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    _socket.Blocking = false;
                    try
                    {
                        _socket.Connect(_endPoint);
                    }
                    catch (SocketException sockEx)
                    {
                        if (sockEx.SocketErrorCode != SocketError.WouldBlock){
                            disconnected = true;
                            break;
                        }
                    }
                    _onConnectionStatusChange?.Invoke(true);
                }

                try
                {
                    // handle error
                    if (_socket.Poll(0, SelectMode.SelectError))
                    {
                        disconnected = true;
                        break;
                    }

                    // handle read
                    if (_socket.Poll(0, SelectMode.SelectRead))
                    {
                        var buf = new byte[_bufSize];
                        var r = _socket.Receive(buf);
                        if (r <= 0)
                        {
                            disconnected = true;
                            break;
                        }
                        _buffer.AddRange(buf.Take(r));
                        while (_buffer.Count >= 2)
                        {
                            var len = _buffer[0] * 256 + _buffer[1];
                            if (_buffer.Count < len + 2) break;
                            _recvingQueue.Add(_buffer.Skip(2).Take(len).ToArray());
                            _buffer.RemoveRange(0, len + 2);
                        }
                    }

                    // handle write
                    if (_socket.Poll(0, SelectMode.SelectWrite))
                    {
                        var data = _sendingQueue.FirstOrDefault();
                        if (data != null) _sendingQueue.RemoveAt(0);
                        if (data != null && data.Length > 0)
                        {
                            int w = _socket.Send(data);
                            if (w <= 0)
                            {
                                disconnected = true;
                                break;
                            }
                            _sendingQueue.Insert(0, data.Skip(w).ToArray());
                        }
                    }
                }
                catch
                {
                    disconnected = true;
                    break;
                }
            }
            while (false);

            if (disconnected)
            {
                _onConnectionStatusChange?.Invoke(false);
                _socket?.Close();
                _socket = null;
            }
        }

        public void SendMsg(byte[] msg)
        {
            var header = new byte[2] { (byte)(msg.Length / 256), (byte)(msg.Length % 256) };
            var dataList = msg.ToList();
            dataList.InsertRange(0, header);
            _sendingQueue.Add(dataList.ToArray());
        }

        public bool ReadMsg(out byte[] msg)
        {
            msg = _recvingQueue.FirstOrDefault();
            if (msg != null)
                _recvingQueue.RemoveAt(0);
            return msg != null;
        }
    }
}

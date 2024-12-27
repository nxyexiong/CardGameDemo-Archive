using System;
using System.Collections.Generic;
using UnityEngine;


namespace Networking
{
    public class Client : MonoBehaviour
    {
        private SocketClient _socket = null;
        private int _seq = 0;

        // seq -> internal callback (response)
        private readonly Dictionary<int, Action<string>> _c2sHandlers = new();

        // request type name -> internal callback (request -> response)
        private readonly Dictionary<string, Func<string, string>> _s2cHandlers = new();

        private bool _connected = false;
        private Action<bool> _connectionStatusCallback = null;

        void Start()
        {
            var ip = PlayerPrefs.GetString("ServerIp") ?? string.Empty;
            _socket = new SocketClient();
            _socket.Connect(ip, 8800, false, (connected) =>
            {
                _connected = connected;
                _connectionStatusCallback?.Invoke(_connected);
            });
        }

        void Update()
        {
            _socket.Update();
            while (_socket.ReadMsg(out var msg))
            {
                var data = System.Text.Encoding.UTF8.GetString(msg);
                Debug.Log($"connection on message:\r\n{data}");

                // parse CSData
                CSData s2cData;
                try
                {
                    s2cData = CSData.From(data);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Update, parsing S2CData failed: {ex}");
                    continue;
                }

                // handle request or response
                if (s2cData.Type == CSData.DataType.Request)
                {
                    // parse request
                    Request req;
                    try
                    {
                        req = Request.From(s2cData.Data);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, parsing Request failed: {ex}");
                        continue;
                    }

                    // call request handler
                    var rspData = string.Empty;
                    if (_s2cHandlers.TryGetValue(req.Type, out var handler))
                        rspData = handler.Invoke(req.Data);

                    // build response
                    CSData c2sData;
                    try
                    {
                        var rsp = new Response { Seq = req.Seq, Data = rspData };
                        c2sData = new CSData { Type = CSData.DataType.Response, Data = rsp.RawData() };
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, building C2SData failed: {ex}");
                        continue;
                    }

                    // send data
                    _socket.SendMsg(System.Text.Encoding.UTF8.GetBytes(c2sData.RawData()));
                }
                else if (s2cData.Type == CSData.DataType.Response)
                {
                    // build response
                    Response rsp;
                    try
                    {
                        rsp = Response.From(s2cData.Data);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Update, building Response failed: {ex}");
                        continue;
                    }

                    // notify
                    if (_c2sHandlers.TryGetValue(rsp.Seq, out var callback))
                        callback.Invoke(rsp.Data);
                }
            }
        }

        void OnDestroy()
        {
            _socket.Disconnect();
            _socket = null;
        }

        // must be called in main thread
        // return false if sending is failed, callback gives default if failed
        public bool SendRequest(string typeName, string requestRaw, Action<string> callback)
        {
            var seq = _seq++;

            // build CSData
            var data = new Request { Seq = seq, Type = typeName, Data = requestRaw };
            var csData = new CSData { Type = CSData.DataType.Request, Data = data.RawData() };

            // setup handler
            _c2sHandlers[seq] = new Action<string>((rspRaw) =>
            {
                _c2sHandlers.Remove(seq);
                callback.Invoke(rspRaw);
            });

            // send
            _socket.SendMsg(System.Text.Encoding.UTF8.GetBytes(csData.RawData()));

            return true;
        }

        // must be called in main thread
        public void SetListener(string typeName, Func<string, string> func)
        {
            _s2cHandlers[typeName] = func;
        }

        // must be called in main thread
        public void RemoveListener(string typeName)
        {
            _s2cHandlers.Remove(typeName);
        }

        public void SetConnectionStatusListener(Action<bool> callback)
        {
            _connectionStatusCallback = callback;
            callback.Invoke(_connected);
        }

        public void RemoveConnectionStatusListener()
        {
            _connectionStatusCallback = null;
        }
    }
}

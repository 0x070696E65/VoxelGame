using System;
using System.Collections.Generic;
using System.Text;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Events;

public class WebSocketManager: MonoBehaviour
{
    public UnityAction<WsTransaction> OnConfirmedTransaction;
    private static WebSocket _websocket;
    
    public async void ConnectWebSocket(string wsNode, string recipientAddress, string hash)
    {
        _websocket = new WebSocket(wsNode);
        _websocket.OnOpen += () => { Debug.Log("WebSocket opened. " + wsNode); };
        _websocket.OnError += errMsg => Debug.Log($"WebSocket Error Message: {errMsg}");
        _websocket.OnClose += code => Debug.Log("WS closed with code: " + code);

        _websocket.OnMessage += async (msg) =>
        {
            var data = Encoding.UTF8.GetString(msg);
            Debug.Log(data);
            var rootData = JsonUtility.FromJson<RootData>(data);
            if (rootData.uid != null)
            {
                var body = "{\"uid\":\"" + rootData.uid + "\", \"subscribe\":\"block\"}";
                await _websocket.SendText(body);
                var confirmed = "{\"uid\":\"" + rootData.uid + "\", \"subscribe\":\"confirmedAdded/" +
                                recipientAddress + "\"}";
                await _websocket.SendText(confirmed);
            }
            else
            {
                var root = JsonUtility.FromJson<Root>(data);
                if (root.topic == "block") Debug.Log("new block:");
                else if (root.topic.Contains("confirmed"))
                {
                    if (root.data.meta.hash != hash) return;
                    await _websocket.Close();
                    OnConfirmedTransaction?.Invoke(root.data.transaction);
                }
                else
                {
                    Debug.Log("else e.Data :" + data);
                }
            }
        };
        await _websocket.Connect();
    }
    
    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _websocket?.DispatchMessageQueue();
#endif
    }
    
    private async void OnDestroy()
    {
        if (_websocket == null) return;
        if(_websocket.State != WebSocketState.Closed && _websocket.State != WebSocketState.Closing) await _websocket.Close();
    }
    
    private async void OnApplicationQuit()
    {
        if (_websocket == null) return;
        if(_websocket.State != WebSocketState.Closed && _websocket.State != WebSocketState.Closing) await _websocket.Close();
    }
    
    [Serializable]
    public class Mosaic
    {
        public string id;
        public string amount;
    }

    [Serializable]
    public class WsTransaction
    {
        public string signature;
        public string signerPublicKey;
        public int version;
        public int network;
        public int type;
        public string maxFee;
        public string deadline;
        public string recipientAddress;
        public string secret;
        public string proof;
        public List<Mosaic> mosaics;
    }

    [Serializable]
    public class Meta
    {
        public string hash;
        public string merkleComponentHash;
        public string height;
    }

    [Serializable]
    public class WaTransactionData
    {
        public WsTransaction transaction;
        public Meta meta;
    }

    [Serializable]
    public class Root
    {
        public string topic;
        public WaTransactionData data;
    }

    [Serializable]
    public class RootData
    {
        public string uid;
    }
}
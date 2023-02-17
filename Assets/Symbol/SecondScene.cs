using System;
using System.Globalization;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.UI;

public class SecondScene : MonoBehaviour
{
    // 表示用
    [SerializeField] private Text address;
    [SerializeField] private Text amount;

    // 送信テスト用
    public InputField ToAddress;
    public InputField ToMosaicId;
    public InputField Message;
    public InputField SendAmount;

    // エディタテスト用 webGlビルドの場合は不要
    public InputField PrivateKey;

    private void Start()
    {
        address.text = SymbolManager.address;
        amount.text = SymbolManager.amount.ToString(CultureInfo.InvariantCulture);
    }

    public async void SendTransaction()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        await SymbolManager.TransferTransaction(ToAddress.text, ToMosaicId.text,
            ulong.Parse(SendAmount.text), Message.text, ObserveTransaction);
#else
        await SymbolManager.TransferTransaction(PrivateKey.text, ToAddress.text, ToMosaicId.text,
            ulong.Parse(SendAmount.text), Message.text, ObserveTransaction);
#endif
    }
    
    // 送金トランザクションが承認されたら残高を更新する関数
    private async void ObserveTransaction()
    {
        Debug.Log($"Complete Transaction");
        await SymbolManager.GetAmount();
        amount.text = SymbolManager.amount.ToString(CultureInfo.InvariantCulture);
        WebSocketManager.OnConfirmedTransaction -= ObserveTransaction;
    }
    
    private void Update()
    {
    #if !UNITY_WEBGL || UNITY_EDITOR
        WebSocketManager.websocket?.DispatchMessageQueue();
    #endif
    }

    private async void OnDestroy()
    {
        if (WebSocketManager.websocket == null) return;
        if(WebSocketManager.websocket.State != WebSocketState.Closed && WebSocketManager.websocket.State != WebSocketState.Closing) await WebSocketManager.websocket.Close();
    }

    private async void OnApplicationQuit()
    {
        if (WebSocketManager.websocket == null) return;
        if(WebSocketManager.websocket.State != WebSocketState.Closed && WebSocketManager.websocket.State != WebSocketState.Closing) await WebSocketManager.websocket.Close();
    }
}
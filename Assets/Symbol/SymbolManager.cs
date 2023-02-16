using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using CatSdk.CryptoTypes;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatSdk.Facade;
using CatSdk.Symbol;
using CatSdk.Symbol.Factory;
using CatSdk.Utils;
using Network = CatSdk.Symbol.Network;
using NetworkTimestamp = CatSdk.Symbol.NetworkTimestamp;

public class SymbolManager : MonoBehaviour
{
    public Text TextFrame;
    public InputField inputField;
    
    // 送信テスト用
    public InputField ToAddress;
    public InputField ToMosaicId;
    public InputField Message;
    public InputField SendAmount;
    // エディタテスト用 webGlビルドの場合は不要
    public InputField PrivateKey;
    
    // 以下のデータは他のシーンなどでも流用できる
    public string addless;
    public string xymId = "72C0212E67A08BCE";
    public float amount;
    
    // シングルトン用
    public static SymbolManager Instance { get; private set; }

    public WebSocketManager webSocketManager;

    private string Node = "https://mikun-testnet.tk:3001";
    
    /*
     * シングルトンと言ってこのスクリプトがアタッチされたオブジェクトが生成されたら同一プロジェクトにこれは一つしか存在しないことを名言する
     * さすれば他のシーンからも
     * Debug.Log(SymbolManager.Instance.amount);
     * のように取得することができる
     */
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }
    
    // この関数をボタンにインスペクターから設定する
    public async void GetAmount()
    {
        addless = inputField.text;
        var symbolAccountData = await GetData($"{Node}/accounts/{addless}");
        var symbolAccountDataJson = JsonUtility.FromJson<Root>(symbolAccountData);
        var xym = symbolAccountDataJson?.account.mosaics.FirstOrDefault(mosaic => mosaic.id == xymId);
        if (xym != null) amount = (float) xym.amount / 1000000;
        Debug.Log(amount);
        TextFrame.text = $"{amount}XYM";
    }
    
    // Apiからデータ取得するための汎用的な関数
    async UniTask<string> GetData(string url)
    {
        using var webRequest = UnityWebRequest.Get(url);
        await webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ConnectionError) throw new Exception(webRequest.error);
        return webRequest.downloadHandler.text;
    }
    
    // 送金トランザクションが承認されたら残高を更新する関数
    private void ObserveTransaction(WebSocketManager.WsTransaction transaction)
    {
        Debug.Log($"Complete Transaction"); 
        GetAmount();
        webSocketManager.OnConfirmedTransaction -= ObserveTransaction;
    }

    // 転送トランザクション送信
    public async void TransferTransaction()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var pubKey = SSS.getActivePublicKey();
        var tx = BuildTransferTransaction(ToAddress.text, pubKey, ToMosaicId.text, ulong.Parse(SendAmount.text), Message.text);
        var signedPayload = await SSS.SignTransactionByPayloadAsync(Converter.BytesToHex(tx.Serialize()));
        var payload = "{ \"payload\" : \"" + signedPayload + "\"}";
        var endpoint = Node + "/transactions";
        var result = await Announce(endpoint, payload);
        Debug.Log(result);
# else
        var facade = new SymbolFacade(Network.TestNet);
        var privateKey = new PrivateKey(PrivateKey.text);
        var keyPair = new KeyPair(privateKey);
        var tx = BuildTransferTransaction(ToAddress.text, Converter.BytesToHex(keyPair.PublicKey.bytes), ToMosaicId.text, ulong.Parse(SendAmount.text), Message.text);
        var signature = facade.SignTransaction(keyPair, tx);
        var payload = TransactionsFactory.AttachSignature(tx, signature);
        var endpoint = Node + "/transactions";
        var result = await Announce(endpoint, payload);
        Debug.Log(result);   
# endif
        var hash = facade.HashTransaction(tx);
        webSocketManager.ConnectWebSocket("wss://mikun-testnet.tk:3001/ws", addless, Converter.BytesToHex(hash.bytes));
        webSocketManager.OnConfirmedTransaction += ObserveTransaction;
    }
    
    private static TransferTransactionV1 BuildTransferTransaction(string address, string pubKey, string mosaicId, ulong amount, string message, ulong feeMultiplier = 100)
    {
        var publicKey = new PublicKey(Converter.HexToBytes(pubKey));
        var facade = new SymbolFacade(CatSdk.Symbol.Network.TestNet);
        
        var tx = new TransferTransactionV1
        {
            Network = NetworkType.TESTNET,
            RecipientAddress = new UnresolvedAddress(Converter.StringToAddress(address)),
            Mosaics = new UnresolvedMosaic[]
            {
                new()
                {
                    MosaicId = new UnresolvedMosaicId(ulong.Parse(mosaicId, NumberStyles.HexNumber)),
                    Amount = new Amount(amount)
                },
            },
            SignerPublicKey = publicKey,
            Message = Converter.Utf8ToPlainMessage(message),
            Deadline = new Timestamp(facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(2).Timestamp)
        };
        tx.Fee = new Amount(tx.Size * feeMultiplier);
        return tx;
    }

    public static async UniTask<string> Announce(string endpoint, string payload)
    {
        using var webRequest = UnityWebRequest.Put(endpoint, Encoding.UTF8.GetBytes(payload));
        webRequest.SetRequestHeader("Content-Type", "application/json");
        await webRequest.SendWebRequest();
        if (webRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception(webRequest.error);
        }
        return webRequest.downloadHandler.text;
    }
    
    // 次のシーンで残高が取得されるか確認用
    public void SceneNext()
    {
        SceneManager.LoadScene("Next");
    }
    
    [System.Serializable]
    public class SupplementalPublicKeys
    {
    }
 
    [System.Serializable]
    public class ActivityBucket
    {
        public string startHeight;
        public string totalFeesPaid;
        public int beneficiaryCount;
        public string rawScore;
    }
 
    [System.Serializable]
    public class Mosaic
    {
        public string id;
        public int amount;
    }
    [System.Serializable]
    public class Account
    {
        public int version;
        public string address;
        public string addressHeight;
        public string publicKey;
        public string publicKeyHeight;
        public int accountType;
        public SupplementalPublicKeys supplementalPublicKeys;
        public List<ActivityBucket> activityBuckets;
        public List<Mosaic> mosaics;
        public string importance;
        public string importanceHeight;
    }
 
    [System.Serializable]
    public class Root
    {
        public Account account;
        public string id;
    }
}

public static class SSS
{
    [DllImport("__Internal")]
    public static extern string getActivePublicKey();
    
    [DllImport("__Internal")]
    private static extern string signTransactionByPayload(Action<string> cb, string pyaload);
    
    private static UniTaskCompletionSource<string> utcs;
    
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void funcCB(string payload)
    {
        utcs.TrySetResult(payload);  
    }
    
    public static UniTask<string> SignTransactionByPayloadAsync(string payload)
    {
        utcs = new UniTaskCompletionSource<string>();
        signTransactionByPayload(funcCB, payload);
        return utcs.Task;
    }
}
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirstScene: MonoBehaviour
{
    public Text TextFrame;
    public InputField inputField;
    
    // 最初のシーンでボタンに設置する関数
    public async void GetAmount()
    {
        SymbolManager.address = inputField.text;
        Debug.Log(SymbolManager.address);
        await SymbolManager.GetAmount();
        TextFrame.text = SymbolManager.amount.ToString(CultureInfo.InvariantCulture);
    }
    
    // 次のシーンで残高が取得されるか確認用
    public void SceneNext()
    {
        SceneManager.LoadScene("SecondScene");
    }
}
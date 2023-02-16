var SSS = {
    getActivePublicKey: function()
    {
        if(!window.SSS) {
          console.log("SSS is not instaled");
          return;
        }
        var returnStr = window.SSS.activePublicKey;
        var buffer = _malloc(lengthBytesUTF8(returnStr) + 1);
        stringToUTF8(returnStr, buffer, returnStr.length + 1);
        return buffer;
    },
    signTransactionByPayload: async function (cb, payload)
    {
        if(!window.SSS) {
          console.log("SSS is not instaled");
          return;
        }
        var strPayload = UTF8ToString(payload);
        window.SSS.setTransactionByPayload(strPayload);
        var signedTx = await window.SSS.requestSign();
        var buffer = _malloc(lengthBytesUTF8(signedTx.payload) + 1);
        stringToUTF8(signedTx.payload, buffer, signedTx.payload.length + 1);
        dynCall_vi(cb, buffer);
    }
}
mergeInto(LibraryManager.library, SSS);
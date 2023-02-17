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
        var r = signedTx.payload + "," + signedTx.hash;
        var buffer = _malloc(lengthBytesUTF8(r) + 1);
        stringToUTF8(r, buffer, r.length + 1);
        dynCall_vi(cb, buffer);
    }
}
mergeInto(LibraryManager.library, SSS);
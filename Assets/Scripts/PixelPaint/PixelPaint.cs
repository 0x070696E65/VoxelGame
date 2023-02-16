using System;
using System.Collections;
using System.Collections.Generic;
using HSVPicker;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PixelPaint : MonoBehaviour
{
    public ColorPicker picker;
    public GameObject canvas;
    public GameObject brush;
    public Image brushImage;

    private float offset = 640f;
    private float brushOffset = 20f;
    
    private GameInputs gameInputs;
    private Vector3 canvasBasePos;
    
    // Start is called before the first frame update
    void Start()
    {
        picker.onValueChanged.AddListener(color =>
        {
            brushImage.color = color;
        });
        gameInputs = new GameInputs();
        gameInputs.Enable();
        
        canvasBasePos = canvas.transform.position;
    }

    private void Update()
    {
        var mousePos = Mouse.current.position.ReadValue();
        if (mousePos.x > canvasBasePos.x 
            && mousePos.x < canvasBasePos.x + offset - brushOffset 
            && mousePos.y > canvasBasePos.y 
            && mousePos.y < canvasBasePos.y + offset - brushOffset)
        {
            brush.transform.position = mousePos;
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
        }
        //throw new NotImplementedException();
    }
}

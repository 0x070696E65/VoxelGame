using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HSVPicker;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PixelPaint : MonoBehaviour
{
    public ColorPicker picker;
    public GameObject Boarad;
    public Dictionary<Vector2, Image> BoaradImages = new Dictionary<Vector2, Image>();
    private float imageSize;
    public GameObject brush;
    public Image brushImage;

    private float offset = 640f;
    private float brushOffset = 20f;
    
    private GameInputs gameInputs;
    private Vector3 boaradBasePos;
    private Color brushColor;

    public byte BoardSize = 16;

    // Start is called before the first frame update
    void Start()
    {
        picker.onValueChanged.AddListener(color =>
        {
            brushColor = color;
            brushImage.color = brushColor;
        });
        gameInputs = new GameInputs();
        gameInputs.Paint.GetMousePos.started += OnDraw;
        gameInputs.Enable();
        boaradBasePos = Boarad.transform.position;
        InitCanvas();
    }

    void InitCanvas()
    {
        imageSize = offset / BoardSize;
        for (var x = 0; x < BoardSize; x++) {
            for (var y = 0; y < BoardSize; y++)
            {
                var obj = new GameObject();
                obj.name = $"{x},{y}";
                var reactTransform = obj.AddComponent<RectTransform>();
                reactTransform.pivot = new Vector2(0, 0);
                reactTransform.sizeDelta = new Vector2(imageSize, imageSize);
                obj.transform.SetParent(Boarad.transform);
                var basePoint = new Vector2(x * imageSize, y * imageSize);
                obj.transform.localPosition = basePoint;
                var image = obj.AddComponent<Image>();
                BoaradImages.Add(basePoint, image);
            }
        }
    }

    private void Update()
    {
        var mousePos = Mouse.current.position.ReadValue();
        if (InBoard(mousePos))
        {
            brush.transform.position = mousePos;
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
        }
    }

    private void OnDraw(InputAction.CallbackContext context)
    {
        var mousePos = Mouse.current.position.ReadValue();
        var image = GetImageFromVector2(mousePos);
        if (image == null) return;
        image.color = brushColor;
    }

    private bool InBoard(Vector2 pos)
    {
        return pos.x > boaradBasePos.x
               && pos.x < boaradBasePos.x + offset 
               && pos.y > boaradBasePos.y
               && pos.y < boaradBasePos.y + offset;
    }

    private Vector2 ConvertMousePosToBoard(Vector2 pos)
    {
        var position = Boarad.transform.position;
        return new Vector2(pos.x - position.x, pos.y - position.y);
    }

    private Image GetImageFromVector2(Vector2 pos)
    {
        if (!InBoard(pos)) return null;
        foreach (var boaradImagesKey in BoaradImages.Keys)
        {
            Debug.Log(boaradImagesKey);
        }
        return BoaradImages.Keys.Where(
            boaradImagesKey => boaradImagesKey.x < pos.x && boaradImagesKey.x + imageSize > pos.x && boaradImagesKey.y < pos.y && boaradImagesKey.y + imageSize > pos.y)
            .Select(boaradImagesKey => BoaradImages[boaradImagesKey]).FirstOrDefault();
    }
}

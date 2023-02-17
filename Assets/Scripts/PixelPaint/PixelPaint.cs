using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HSVPicker;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PixelPaint : MonoBehaviour
{
    public ColorPicker picker;
    public GameObject Boarad;
    private float imageSize;
    public GameObject brush;
    public Image brushImage;

    private const float offset = 640f;

    private GameInputs gameInputs;
    private Vector3 boaradBasePos;
    private Color brushColor;
    
    public Texture2D texture;

    Vector2 _cursorPosition;
    public byte BoardSize = 16;
    
    [SerializeField] string filePath = "Assets/export.png";
    [SerializeField] private Button drawMode;
    [SerializeField] private Button eraceMode;
    [SerializeField] private Button allClear;
    [SerializeField] private Button save;
    [SerializeField] private Button setAllTextures;
    private bool isDraw;
    
    [SerializeField] private Button front;
    [SerializeField] private Button back;
    [SerializeField] private Button top;
    [SerializeField] private Button botton;
    [SerializeField] private Button left;
    [SerializeField] private Button right;

    [SerializeField] private List<Image> images;

    // Start is called before the first frame update
    void Start()
    {
        brushColor = Color.black;
        boaradBasePos = Boarad.transform.position;
        picker.onValueChanged.AddListener(color =>
        {
            brushColor = color;
            brushImage.color = brushColor;
        });
        _cursorPosition = new Vector2(Screen.width / 2, Screen.height / 2);
        gameInputs = new GameInputs();
        gameInputs.Enable();
        InitCanvas();
        
        drawMode.onClick.AddListener(() => {
            isDraw = true;
            drawMode.interactable = false;
            eraceMode.interactable = true;
        });
        eraceMode.onClick.AddListener(() => {
            isDraw = false;
            drawMode.interactable = true;
            eraceMode.interactable = false;
        });
        allClear.onClick.AddListener(AllErace);
        save.onClick.AddListener(SaveTecture);
        isDraw = true;
        
        setAllTextures.onClick.AddListener(SetAllTextures);
    }

    void InitCanvas()
    {
        imageSize = offset / BoardSize;
    }

    private void Update()
    {
        var mousePos = Mouse.current.position.ReadValue();
        if (InBoard(mousePos))
        {
            Cursor.visible = false;
            if (Gamepad.current != null)
            {
                var delta = gameInputs.UI.GamepadMouse.ReadValue<Vector2>();
                _cursorPosition += delta * World.Instance.settings.mouseSensitivity;
                _cursorPosition.x = Mathf.Clamp(_cursorPosition.x, 0, Screen.width);
                _cursorPosition.y = Mathf.Clamp(_cursorPosition.y, 0, Screen.height);
                Mouse.current.WarpCursorPosition(_cursorPosition);

                brush.transform.position = _cursorPosition;
            }
            else
            {
                brush.transform.position = mousePos;
            }

            if (gameInputs.Paint.GetMousePos.IsPressed())
            {
                Draw();
            }
        }
        else
        {
            Cursor.visible = true;
        }
    }

    private void Draw()
    {
        var mp = GetMousePositionInLocalSpace(Boarad);
        var v2i = new Vector2Int((int)(mp.x / imageSize), (int)(mp.y / imageSize));
        var pixelData = texture.GetPixelData<Color32>( 0 );
        pixelData[v2i.x + v2i.y * BoardSize] = isDraw ? brushColor : new Color(0, 0, 0, 0);
        texture.Apply();
    }

    private void OnDestroy()
    {
        AllErace();
        Resources.UnloadUnusedAssets();
        foreach (var image in images)
        {
            Destroy(image.sprite);
        }
    }

    private void AllErace()
    {
        var pixelData = texture.GetPixelData<Color32>( 0 );
        for ( var i = 0; i < pixelData.Length; i++ ) {
            pixelData[ i ] = new Color32( 0, 0, 0, 0 );
        }
        texture.Apply();
    }

    private bool InBoard(Vector2 pos)
    {
        return pos.x > boaradBasePos.x
               && pos.x < boaradBasePos.x + offset 
               && pos.y > boaradBasePos.y
               && pos.y < boaradBasePos.y + offset;
    }
    
    private void SaveTecture()
    {
        ConvertTextureToPng(texture);
    }
    
    void ConvertTextureToPng(Texture2D tex)
    {
        var pngData = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);
    }
    
    private static Vector2 GetMousePositionInLocalSpace(GameObject targetObject)
    {
        var mousePosition = Input.mousePosition;
        var position = targetObject.transform.position;
        return new Vector2(mousePosition.x - position.x,
            mousePosition.y - position.y);
    }

    private void SetAllTextures()
    {
        foreach (var image in images)
        {
            image.sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.zero);;
        }
    }
}

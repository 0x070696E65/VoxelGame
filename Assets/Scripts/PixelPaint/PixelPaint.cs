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
    
    Vector2 _cursorPosition;
    public byte BoardSize = 16;
    private const string filePath = "Assets/texture";
    [SerializeField] private Button drawMode;
    [SerializeField] private Button eraceMode;
    [SerializeField] private Button allClear;
    [SerializeField] private Button save;
    private bool isDraw;
    
    [SerializeField] private List<Button> changeButton;

    [SerializeField] private Image mainImage;
    [SerializeField] private List<Image> images;

    [SerializeField] private List<Button> Mirrors;
    [SerializeField] private List<Image> MirrorsImage;
    private readonly List<byte> MirrorsState = new List<byte>{0,0,0,0,0,0};

    private readonly List<Texture2D> textureList = new List<Texture2D>();
    private readonly List<Sprite> spriteList = new List<Sprite>();
    private byte textureNumber = 0;

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
        allClear.onClick.AddListener(()=>AllErace(textureList[textureNumber]));
        save.onClick.AddListener(SaveTecture);
        isDraw = true;

        for (byte i = 0; i < changeButton.Count; i++) {
            var i1 = i;
            changeButton[i].onClick.AddListener(()=>SwitchTexture(i1));
        }
        for (byte i = 0; i < Mirrors.Count; i++) {
            var i1 = i;
            Mirrors[i].onClick.AddListener(()=>Mirror(i1));
        }
        
    }
    
    void InitCanvas()
    {
        Clear();
        imageSize = offset / BoardSize;
        initAllImages(16);
        mainImage.sprite = spriteList[0];
        for (var i = 0; i < 6; i++)
            images[i].sprite = spriteList[0];
        textureNumber = 0;
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
        var pixelData = textureList[textureNumber].GetPixelData<Color32>( 0 );
        pixelData[v2i.x + v2i.y * BoardSize] = isDraw ? brushColor : new Color(0, 0, 0, 0);
        textureList[textureNumber].Apply();
    }
    
    private static void AllErace(Texture2D _texture)
    {
        var pixelData = _texture.GetPixelData<Color32>( 0 );
        for ( var i = 0; i < pixelData.Length; i++ ) {
            pixelData[ i ] = new Color32( 0, 0, 0, 0 );
        }
        _texture.Apply();
    }

    private bool InBoard(Vector2 pos)
    {
        return pos.x > boaradBasePos.x
               && pos.x < boaradBasePos.x + offset 
               && pos.y > boaradBasePos.y
               && pos.y < boaradBasePos.y + offset;
    }

    private bool IsTransparent(Texture2D _texture)
    {
        var pixelData = _texture.GetPixelData<Color32>( 0 );
        for ( var i = 0; i < pixelData.Length; i++ ) {
            if (pixelData[i].b != 0 || pixelData[i].g != 0 || pixelData[i].r != 0 || pixelData[i].a != 0) return false;
        }
        return true;
    }
    
    private void SaveTecture()
    {
        for (var i = 0; i < textureList.Count; i++) {
            if (!IsTransparent(textureList[i])) ConvertTextureToPng(textureList[i], i.ToString());
        }
    }
    
    void ConvertTextureToPng(Texture2D tex, string fileName)
    {
        var pngData = tex.EncodeToPNG();
        File.WriteAllBytes(filePath + fileName + ".png", pngData);
    }
    
    private static Vector2 GetMousePositionInLocalSpace(GameObject targetObject)
    {
        var mousePosition = Input.mousePosition;
        var position = targetObject.transform.position;
        return new Vector2(mousePosition.x - position.x,
            mousePosition.y - position.y);
    }

    private void Mirror(int index)
    {
        MirrorsState[index]++;
        if (MirrorsState[index] > 5) MirrorsState[index] = 0;
        MirrorsImage[index].color = MirrorsState[index] switch
        {
            0 => Color.white,
            1 => Color.blue,
            2 => Color.red,
            3 => Color.green,
            4 => Color.yellow,
            5 => Color.cyan,
            _ => MirrorsImage[index].color
        };
        images[index].sprite = spriteList[MirrorsState[index]];
    }

    private void SwitchTexture(byte index)
    {
        textureNumber = MirrorsState[index];
        mainImage.sprite = spriteList[MirrorsState[index]];
    }

    private void initAllImages(byte size)
    {
        foreach (var image in images)
        {
            var _texture = new Texture2D(size, size) {
                filterMode = FilterMode.Point
            };
            AllErace(_texture);
            textureList.Add(_texture);
            var _sprite = Sprite.Create(_texture,
                new Rect(0, 0, _texture.width, _texture.height),
                Vector2.zero);
            spriteList.Add(_sprite);
            image.sprite = _sprite;
        }
    }
    
    void Clear()
    {
        if (textureList.Count > 0) {
            textureList.Clear();
            foreach (var _texture in textureList)
                Destroy(_texture);
        }

        if (spriteList.Count > 0) {
            spriteList.Clear();
            foreach (var _sprite in spriteList)
                Destroy(_sprite);
        }
    }
    
    private void OnDestroy()
    {
        Clear();
    }
}

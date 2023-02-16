using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Threading;
using UnityEngine.Events;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class World : MonoBehaviour
{
    public int count;
    public Settings settings;
     
    private Camera mainCamera;
    
    [Header("World Generation Values")]
    public BiomeAttributes[] biomes;
    
    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;
    
    public Transform player;
    public Player _player;
    public Vector3 spawnPosition;
    
    public Material material;
    public Material transparentMaterial;
    public Material waterMaterial;
    public BlockType[] blocktypes;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    private List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    public ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;
    
    private bool applyingModifications = false;

    private Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    
    private bool _inUI = false;
    
    public GameObject debugScreen;
    public Text debugText;

    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;
    
    private Thread ChunkUpdateThread;
    public readonly object ChunkUpdateThreadLock = new object();
    public object ChunkListThreadLock = new object();

    public static World Instance { get; private set; }

    public WorldData worldData;

    public UnityAction OnStartCheckViewDistance;

    public string appPath;

    private GameInputs gameInputs;

    private void Awake()
    {
        // If the instance value is not null and not *this*, we've somehow ended up with more than one World component.
        // Since another one has already been assigned, delete this one.
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        // Else set this to the instance.
        else
            Instance = this;

        appPath = Application.persistentDataPath;

        _player = player.GetComponent<Player>();
    }

    private void Start()
    {
        Debug.Log("Generating new world using seed " + VoxelData.seed);

        worldData = new WorldData("NewWorld", 0);
        
        var jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);
        
        //Random.InitState(VoxelData.seed);

        gameInputs = new GameInputs();
        gameInputs.Main.Debug.started += OnDebug;
        gameInputs.Main.Save.started += OnSave;
        gameInputs.Main.Load.started += OnLoad;
        gameInputs.Enable();
        
        LoadWorld();

        mainCamera = Camera.main;
        spawnPosition = new Vector3(VoxelData.WorldCentre, VoxelData.ChunkHeight - 120f, VoxelData.WorldCentre);
        player.position = spawnPosition;
        CheckViewDistance();
        //playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
        
        //StartCoroutine(Tick());
    }

    public void SetNewVoxel(Vector3 pos, byte id)
    {
        GetChunkFromVector3(pos).EditVoxel(pos, id);
        // load
        //var a = GetChunkFromVector3(new Vector3(800f, 46f, 800f));
        //Debug.Log(a.chunkData.map[1, 1, 1].id);
        //Debug.Log(a.chunkObject.name);
        // World.Instance.worldData.GetVoxel()
        
        // save
        //Debug.Log(a.GetVoxelFromGlobalVector3(new Vector3(800f, 47f, 800f)).id);
        //Debug.Log(chunksToUpdate[0].chunkObject.name);
    }
    
    IEnumerator Tick() {
        while (true) {
            foreach (var c in activeChunks) 
                chunks[c.x, c.z].TickUpdate();
            
            yield return new WaitForSeconds(VoxelData.tickLength);
        }
    }
    
    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        
        if (chunksToDraw.Count > 0) 
            chunksToDraw.Dequeue().CreateMesh();
        
        if (!applyingModifications)
            ApplyModifications();

        if (chunksToUpdate.Count > 0)
            UpdateChunks();
    }

    
    void LoadWorld()
    {
        Task.Run(() =>
        {
            //for (var x = VoxelData.WorldSizeInChunks / 2 - settings.loadDistance; x < VoxelData.WorldSizeInChunks / 2 + settings.loadDistance; x++) {
            //for (var z = VoxelData.WorldSizeInChunks / 2 - settings.loadDistance; z < VoxelData.WorldSizeInChunks / 2 + settings.loadDistance; z++){
            for (var x = 0; x < 8; x++)
            {
                for (var z = 0; z < 8; z++)
                {
                    worldData.LoadChunk(new Vector2Int(x, z));
                }
            }
        });
    }
    
    public void AddChunkToUpdate(Chunk chunk)
    {
        AddChunkToUpdate(chunk, false);
    }
    
    public void AddChunkToUpdate(Chunk chunk, bool insert)
    {
        // Lock list to ensure only one thing is using the list at a time.
        lock (ChunkUpdateThreadLock) {
            // Make sure update list doesn't already contain chunk.
            if (!chunksToUpdate.Contains(chunk)) {
                if(insert)
                    chunksToUpdate.Insert(0, chunk);
                else
                    chunksToUpdate.Add(chunk);
            }
        }
    }

    void UpdateChunks()
    {
        lock (ChunkUpdateThreadLock) {
            chunksToUpdate[0].UpdateChunk();
            if(!activeChunks.Contains(chunksToUpdate[0].coord))
                activeChunks.Add(chunksToUpdate[0].coord);
            chunksToUpdate.RemoveAt(0);
        }
    }
    
    void ApplyModifications()
    {
        applyingModifications = true;

        while (modifications.Count > 0)
        {
            var queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                worldData.SetVoxel(v.position, v.id, 1);
            }
        }
        applyingModifications = false;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        var x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        var z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        var x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        var z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];
    }
    
    void CheckViewDistance()
    {
        OnStartCheckViewDistance?.Invoke();
        
        var coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;
        
        var previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
        
        activeChunks.Clear();

        // Loop through all chunks currently within view distance of the player.
        for (var x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++) {
            for (var z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {
                var thisChunkCoord = new ChunkCoord(x, z);
                
                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord)) {
                    
                    // Check if it active, if not, activate it.
                    if (chunks[x, z] == null)
                        chunks[x, z] = new Chunk(thisChunkCoord);
                    chunks[x, z].isActive = true;
                    activeChunks.Add(thisChunkCoord);
                }

                // Check through previously active chunks to see if this chunk is there. If it is, remove it from the list.
                for (var i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }    
        }

        foreach (var c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false;
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        var voxel = worldData.GetVoxel(pos);
        return blocktypes[voxel.id].isSolid;
    }
    
    public VoxelState GetVoxelState(Vector3 pos)
    {
        return worldData.GetVoxel(pos);
    }

    public bool inUI
    {
        get => _inUI;
        set
        {
            _inUI = value;
            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }
    
    public byte GetVoxel(Vector3 pos)
    {
        var yPos = Mathf.FloorToInt(pos.y);
        /* IMMUTABLE PASS */
        
        // If outside world return air.
        if (!IsVoxelInWorld(pos))
            return 0;

        // If bottom block of chunk, return bedrock.
        if (yPos == 0)
            return 1;
        return 0;
    }

    bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 &&
            coord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        return false;
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
            pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        return false;
    }

    private void OnDebug(InputAction.CallbackContext context)
    {
        debugScreen.SetActive(!debugScreen.activeSelf);
    }
    private void OnSave(InputAction.CallbackContext context)
    {
        var saveWorld = new SaveWorld();
        foreach (var (key, value) in worldData.chunks.Where(chunk => chunk.Value.HasVoxel()))
        {
            var vs = new List<SaveVoxel> { };
            foreach (var voxelState in value.map)
            {
                if (voxelState.id is 0 or 1) continue;
                var v = new SaveVoxel(new List<int>()
                {
                    voxelState.id,
                    voxelState.position.x,
                    voxelState.position.y,
                    voxelState.position.z
                });
                if (voxelState.orientation != 1) v.d.Add(voxelState.orientation);
                vs.Add(v);
            }
            var saveChunk = new SaveChunk(new []{key.x, key.y}, vs);
            saveWorld.c.Add(saveChunk);
        }
        var json = JsonUtility.ToJson(saveWorld);
        Debug.Log(json);
        SaveSystem.SaveWorld(worldData.worldName, json);
    }
    
    private void OnLoad(InputAction.CallbackContext context)
    {
        var data = SaveSystem.LoadWorld(worldData.worldName);
        Debug.Log(data);
        var savedWorldData = JsonUtility.FromJson<SaveWorld>(data);
            foreach (var saveChunk in savedWorldData.c)
            {
                var vi = new Vector2Int(saveChunk.i[0], saveChunk.i[1]);
                var chunkDataPair = worldData.chunks.FirstOrDefault(kvp => kvp.Key.Equals(vi));
                foreach (var saveVoxel in saveChunk.v)
                {
                    var direction = saveVoxel.d.Count == 4 ? 1 : saveVoxel.d[4]; 
                    chunkDataPair.Value.ModifyVoxel(new Vector3Int(saveVoxel.d[1], saveVoxel.d[2], saveVoxel.d[3]), saveVoxel.d[0], (byte)direction);    
                }
            }

            var saveWorld = new SaveWorld();
        foreach (var (key, value) in worldData.chunks.Where(chunk => chunk.Value.HasVoxel()))
        {
            var vs = new List<SaveVoxel> { };
            foreach (var voxelState in value.map)
            {
                if (voxelState.id is 0 or 1) continue;
                var v = new SaveVoxel(new List<int>()
                {
                    voxelState.id,
                    voxelState.position.x,
                    voxelState.position.y,
                    voxelState.position.z
                });
                if (voxelState.orientation != 1) v.d.Add(voxelState.orientation);
                vs.Add(v);
            }
            var saveChunk = new SaveChunk(new []{key.x, key.y}, vs);
            saveWorld.c.Add(saveChunk);
        }
        var json = JsonUtility.ToJson(saveWorld);
        Debug.Log(json);
        SaveSystem.SaveWorld(worldData.worldName, json);
    }
}

[Serializable]
public class SaveWorld
{
    public List<SaveChunk> c;
    public SaveWorld()
    {
        c = new List<SaveChunk>();
    }
}

[Serializable]
public class SaveChunk
{
    public int[] i;
    public List<SaveVoxel> v;
    public SaveChunk(int[] _id, List<SaveVoxel> _v)
    {
        i = _id;
        v = _v;
    }
}

[Serializable]
public class SaveVoxel
{
    public List<int> d;

    public SaveVoxel(List<int> _d)
    {
        d = _d;
    }
}


[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;
    public VoxelMeshData meshData;
    public bool transparency;
    public bool renderNeighborFaces;
    public bool isWater;
    public byte opacity;
    public Sprite icon;
    public bool isActive;
    
    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
    
    // Back, Front, Top, Bottom, Left, Right
    public int GetTextureID (int faceIndex) {
        switch (faceIndex) {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }
    
    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}

[Serializable]
public class Settings
{
    [Header("Game Data")] 
    public string version = "0.0.1";

    [Header("Performance")]
    public int loadDistance = 16;
    public int viewDistance = 8;
    public bool enableThreading = true;
    public CloudStyle clouds = CloudStyle.Fast;
    public bool enableAnimatedChunks = true;
    
    [Header("Controls")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2.0f;
}
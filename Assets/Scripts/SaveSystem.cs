using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

public static class SaveSystem
{
    public static void SaveWorld(string worldName, string data)
    {
        // Set our save location and make sure we have a saves folder ready to go.
        var savePath = World.Instance.appPath + "/saves/" + worldName + "/";
        
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        
        Debug.Log("Saving " + worldName);
        var formatter = new BinaryFormatter();
        var stream = new FileStream(savePath + "world.world", FileMode.Create);
        
        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static void SaveChunks(WorldData world)
    {
        var chunks = new List<ChunkData>(world.modifiedChnks);
        world.modifiedChnks.Clear();

        var count = 0;
        foreach (var chunk in chunks)
        {
            SaveChunk(chunk, world.worldName);
            count++;
        }

        Debug.Log(count + " chunks saved.");
    }

    public static string LoadWorld(string worldName, int seed = 0)
    {
        var loadPath = World.Instance.appPath + "/saves/" + worldName + "/";
        if (File.Exists(loadPath + "world.world"))
        {
            Debug.Log(worldName + " found. Loading from save.");

            var formatter = new BinaryFormatter();
            var stream = new FileStream(loadPath + "world.world", FileMode.Open);

            var world = formatter.Deserialize(stream) as string;
            stream.Close();
            return world;
        }
        else
        {
            Debug.Log(worldName + " not found. Creating new world.");

            var world = new WorldData(worldName, seed);
            SaveWorld(world.worldName, "");
            return "";
        }
    }
    
    public static void SaveChunk(ChunkData chunk, string worldName)
    {
        var chunkName = chunk.position.x + "-" + chunk.position.y;
        
        // Set our save location and make sure we have a saves folder ready to go.
        var savePath = World.Instance.appPath + "/saves/" + worldName + "/chunks/";
        
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        
        var formatter = new BinaryFormatter();
        var stream = new FileStream(savePath + chunkName +".chunk", FileMode.Create);
        
        formatter.Serialize(stream, chunk);
        stream.Close();
    }
    
    public static ChunkData LoadChunk(string worldName, Vector2Int position)
    {
        var chunkName = position.x + "-" + position.y;
        
        var loadPath = World.Instance.appPath + "/saves/" + worldName + "/chunks/" + chunkName + ".chunk";
        if (!File.Exists(loadPath)) return null;
        
        var formatter = new BinaryFormatter();
        var stream = new FileStream(loadPath, FileMode.Open);

        var chunkData = formatter.Deserialize(stream) as ChunkData;
        stream.Close();
        return chunkData;
    }
}

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public static class Serialization
{

    public static string SaveLocation(string worldName)
    {
        string saveLocation = Config.Directories.SaveFolder + "/" + worldName + "/";

        if (!Directory.Exists(saveLocation))
        {
            Directory.CreateDirectory(saveLocation);
        }

        return saveLocation;
    }

    public static string FileName(BlockPos chunkLocation)
    {
        string fileName = chunkLocation.x + "," + chunkLocation.y + "," + chunkLocation.z + ".bin";
        return fileName;
    }

    public static void SaveChunk(Chunk chunk)
    {
        Save save = new Save(chunk);

        if (save.changed)
        {
            string saveFile = SaveLocation(chunk.world.worldName);
            saveFile += FileName(chunk.pos);

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(saveFile, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, save);
            stream.Close();
        }
    }

    public static bool Load(Chunk chunk)
    {

        string saveFile = SaveLocation(chunk.world.worldName);
        saveFile += FileName(chunk.pos);

        if (!File.Exists(saveFile))
            return false;
        try
        {

            IFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(saveFile, FileMode.Open);

            Save save = (Save)formatter.Deserialize(stream);

            //Once the blocks in the save are added they're marked as unmodified so
            //as not to trigger a new save on unload unless new blocks are added.
            for (int i =0; i< save.blocks.Length; i++)
            {
                Block placeBlock = save.blocks[i];
                placeBlock.modified = false;
                chunk.SetBlock(save.positions[i], placeBlock, false);
            }

            stream.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }


        return true;
    }
}
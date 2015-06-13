using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

[Serializable]
public class Save
{
    public BlockPos[] positions = new BlockPos[0];
    public Block[] blocks = new Block[0];

    public bool changed = false;

    public Save(Chunk chunk)
    {

        try
        {
            //Because existing saved blocks aren't marked as modified we have to add the
            //blocks already in the save fie if there is one. Then add 
            Dictionary<BlockPos, Block> blocksDictionary = AddSavedBlocks(chunk);

            for (int x = 0; x < Config.Env.ChunkSize; x++)
            {
                for (int y = 0; y < Config.Env.ChunkSize; y++)
                {
                    for (int z = 0; z < Config.Env.ChunkSize; z++)
                    {
                        BlockPos pos = new BlockPos(x, y, z);
                        if (chunk.GetBlock(pos).modified)
                        {
                            //remove any existing blocks in the dictionary as they're
                            //from the existing save and are overwritten
                            blocksDictionary.Remove(pos);
                            blocksDictionary.Add(pos, chunk.GetBlock(pos));
                            changed = true;
                        }
                    }
                }
            }

            blocks = new Block[blocksDictionary.Keys.Count];
            positions = new BlockPos[blocksDictionary.Keys.Count];

            int index = 0;

            foreach (var pair in blocksDictionary)
            {
                blocks[index] = pair.Value;
                positions[index] = pair.Key;
                index++;
            }

        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }

    }

    Dictionary<BlockPos, Block> AddSavedBlocks(Chunk chunk){
        string saveFile = Serialization.SaveLocation(chunk.world.worldName);
        saveFile += Serialization.FileName(chunk.pos);

        Dictionary<BlockPos, Block> blocksDictionary = new Dictionary<BlockPos, Block>();

        if (!File.Exists(saveFile))
            return blocksDictionary;

        IFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(saveFile, FileMode.Open);

        Save save = (Save)formatter.Deserialize(stream);

        for (int i = 0; i< save.blocks.Length; i++)
        {
            blocksDictionary.Add(save.positions[i], save.blocks[i]);
        }

        stream.Close();

        return blocksDictionary;
    }
}
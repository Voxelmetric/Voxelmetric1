using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Threading;

public static class Serialization {

    /// <summary>
    /// Get the directory to save files in for worldName
    /// </summary>
    /// <remarks>Also ensures the directory has been created</remarks>
    /// <param name="worldName"></param>
    /// <returns></returns>
    public static string SaveLocation(string worldName) {
        string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
            ? Environment.GetEnvironmentVariable("HOME")
            : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

        string saveLocation = homePath + "/Saved Games/" + Config.Directories.SaveFolder + "/" + worldName + "/";

        if (!Directory.Exists(saveLocation)) {
            Directory.CreateDirectory(saveLocation);
        }

        return saveLocation;
    }

    private static string FileName(BlockPos chunkLocation) {
        string fileName = chunkLocation.x + "," + chunkLocation.y + "," + chunkLocation.z + ".bin";
        return fileName;
    }

    public static string SaveFileName(World world, BlockPos chunkLocation) {
        string saveFile = SaveLocation(world.worldName);
        saveFile += FileName(chunkLocation);
        return saveFile;
    }

    public static string SaveFileName(Chunk chunk) {
        return SaveFileName(chunk.world, chunk.pos);
    }

    public class Saver {
        private bool? saved;

        public bool IsSaved { get { return saved ?? saved.Value; } }

        public IEnumerator Save(Chunk chunk) {
            // Check for existing file.
            //Save existing = Read(chunk);
            var loader = new Loader();
            var t = new Thread(() => loader.Load(chunk));
            t.Start();
            while(t.IsAlive) yield return null;
            Save existing = loader.save;
            // Merge existing with present information
            Save save = new Save(chunk, existing);
            if(save.changed) {
                // Write
                //var e = Write(save); while(e.MoveNext()) yield return e.Current;
                Write(save);
            } else {
                saved = true;
                //yield break;
            }
        }

        //private IEnumerator Write(Save save) {
        private void Write(Save save) {
            string finalSaveFile = SaveFileName(save.Chunk);
            // Write to a temporary file for safety
            string saveFile = finalSaveFile + ".tmp";
            if(WriteL(saveFile, save)) {
                // Now delete the final file and move the temporary over
                if(File.Exists(finalSaveFile))
                    File.Delete(finalSaveFile);
                File.Move(saveFile, finalSaveFile);

                saved = true;
            } else {
                saved = false;
                //yield break;
            }
        }
    }

    public static bool SaveChunk(Chunk chunk) {
        var saver = new Saver();
        CoroutineUtils.DoCoroutine(saver.Save(chunk));
        return saver.IsSaved;
    }

    private class Loader {
        public Save save;
        public void Load(Chunk chunk) {
            save = Read(chunk);
        }
    }

    public static IEnumerator LoadChunk(Chunk chunk) {
        // Read file
        //Save save = Read(chunk);
        var loader = new Loader();
        var t = new Thread(() => loader.Load(chunk));
        t.Start();
        while(t.IsAlive) yield return null;
        Save save = loader.save;
        if(save == null)
            yield break;
        //Once the blocks in the save are added they're marked as unmodified so
        //as not to trigger a new save on unload unless new blocks are added.
        for (int i = 0; i < save.blocks.Length; i++)
            chunk.blocks.Set(save.positions[i], save.blocks[i], updateChunk: false, setBlockModified: false);
        yield return null;
    }

    public static Save Read(Chunk chunk) {
        string saveFile = SaveFileName(chunk);

        if (!File.Exists(saveFile))
            return null;
        Save save = ReadL(saveFile, chunk.world);
        if (save == null)
            return null;

        // Handle version changes
        World world = chunk.world;
        var blockTypeMap = save.MakeTypeMap();

        bool missingBlocks = false;
        for(int idx=0; idx<save.blocks.Length; ++idx) {
            Block block = save.blocks[idx];
            string name;
            if (blockTypeMap.TryGetValue(block.Type, out name)) {
                ushort newType = world.blockIndex.GetBlockType(name);
                if (newType == 0) {
                    // This block doesn't exist in the world config anymore
                    // Change to air, but make a note to backup later
                    save.blocks[idx] = Block.New(Block.AirType, world);
                    missingBlocks = true;
                } else if (newType != block.Type) {
                    // type code of block now maps to a different name -- most likely
                    // due to a new block type being added to the world config
                    save.blocks[idx] = Block.New(newType, world);
                } else {
                    BlockConfig newConfig = world.blockIndex.GetConfig(newType);
                    if (!block.GetType().Equals(newConfig.blockClass)) {
                        // Cater for the seemingly impossible -- happened to one of my worlds somehow...
                        save.blocks[idx] = Block.New(newType, world);
                    }
                }
            } else if (save.Version > 0) {
                // This shouldn't ever happen!
                Debug.LogError("No name found in save map for block type " + block.Type);
            }
        }

        bool saveNow = false;
        if (save.Version < 1) {
            // Critical update -- save again immediately to upgrade
            // First backup...
            File.Move(saveFile, saveFile + ".bak_" + save.Version);
            saveNow = true;
        } else if (missingBlocks) {
            // Some blocks were missing -- make a backup
            File.Move(saveFile, saveFile + ".bak");
            saveNow = true;
        }
        if (saveNow) {
            // Merge existing with present information
            save = new Save(chunk, save);
            // And write the new version
            WriteL(saveFile, save);
        }

        return save;
    }

    private static bool WriteL(string saveFile, Save save) {
        try {
            var context = new StreamingContext(StreamingContextStates.All, save.Chunk.world);
            IFormatter formatter = new BinaryFormatter(null, context);
            using (FileStream stream = new FileStream(saveFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
                AddSerializationSurrogates(formatter);
                formatter.Serialize(stream, save);
            }
            return true;
        } catch (Exception ex) {
            Debug.LogError("Could not save: " + saveFile + "\n" + ex);
            return false;
        }
    }

    private static Save ReadL(string saveFile, World world) {
        try {
            var context = new StreamingContext(StreamingContextStates.All, world);
            IFormatter formatter = new BinaryFormatter(null, context);
            using (FileStream stream = new FileStream(saveFile, FileMode.Open)) {
                AddSerializationSurrogates(formatter);
                return (Save)formatter.Deserialize(stream);
            }
        } catch (Exception ex) {
            Debug.LogError("Could not load: " + saveFile + "\n" + ex);
            return null;
        }
    }

    private static void AddSerializationSurrogates(IFormatter formatter)
    {
        SurrogateSelector selector = new SurrogateSelector();
        selector.AddSurrogate(typeof(Color), new StreamingContext(StreamingContextStates.All),
            new ColorSerializationSurrogate());
        formatter.SurrogateSelector = selector;
    }
}
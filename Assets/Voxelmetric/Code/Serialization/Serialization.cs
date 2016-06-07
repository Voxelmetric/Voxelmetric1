using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Serialization
{
    public static class Serialization
    {
        private static string SaveLocation(string worldName) {
            string saveLocation = Directories.SaveFolder + "/" + worldName + "/";

            if (!Directory.Exists(saveLocation)) {
                Directory.CreateDirectory(saveLocation);
            }

            return saveLocation;
        }

        private static string FileName(BlockPos chunkLocation) {
            string fileName = chunkLocation.x + "," + chunkLocation.y + "," + chunkLocation.z + ".bin";
            return fileName;
        }

        private static string SaveFileName(Chunk chunk) {
            string saveFile = SaveLocation(chunk.world.worldName);
            saveFile += FileName(chunk.pos);
            return saveFile;
        }

        public static bool SaveChunk(Chunk chunk)
        {
            // Check for existing file.
            Save existing = Read(chunk);
            // Merge existing with present information
            Save save = new Save(chunk, existing);
            // Write
            return Write(save);
        }

        public static bool LoadChunk(Chunk chunk)
        {
            // Read file
            Save save = Read(chunk);
            if (save == null)
                return false;
            //Once the blocks in the save are added they're marked as unmodified so
            //as not to trigger a new save on unload unless new blocks are added.
            for (int i = 0; i < save.blocks.Length; i++)
                chunk.blocks.Set(save.positions[i], save.blocks[i], updateChunk: false, setBlockModified: false);
            return true;
        }

        private sealed class SaveLoadDeserializationBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                // For each assemblyName/typeName that you want to deserialize to
                // a different type, set typeToDeserialize to the desired type.
                string exeAssembly = Assembly.GetExecutingAssembly().FullName;

                // The following line of code returns the type.
                Type typeToDeserialize = AppDomain.CurrentDomain.GetAssemblies()
                                                  .Where(a => a.FullName == exeAssembly)
                                                  .SelectMany(a => a.GetTypes())
                                                  .FirstOrDefault(t => t.Name == typeName);

                return typeToDeserialize;
            }
        }

        private static bool Write(Save save) {
            if (!save.changed)
                return true;

            string saveFile = SaveFileName(save.Chunk);
            try {
                IFormatter formatter = new BinaryFormatter();
                formatter.Binder = new SaveLoadDeserializationBinder();
                using (FileStream stream = new FileStream(saveFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    AddSerializationSurrogates(formatter);
                    formatter.Serialize(stream, save);
                }
            } catch (Exception ex) {
                Debug.LogError("Could not save: " + saveFile + "\n" + ex);
                return false;
            }
            return true;
        }

        private static Save Read(Chunk chunk) {
            string saveFile = SaveFileName(chunk);

            if (!File.Exists(saveFile))
                return null;
            Save save;
            try {
                IFormatter formatter = new BinaryFormatter();
                formatter.Binder = new SaveLoadDeserializationBinder();
                using (FileStream stream = new FileStream(saveFile, FileMode.Open)) {
                    AddSerializationSurrogates(formatter);
                    save = (Save)formatter.Deserialize(stream);
                }
            } catch (Exception ex) {
                Debug.LogError("Could not load: " + saveFile + "\n" + ex);
                return null;
            }
            foreach (Block block in save.blocks) {
                block.world = chunk.world;
            }
            return save;
        }

        private static void AddSerializationSurrogates(IFormatter formatter)
        {
            SurrogateSelector selector = new SurrogateSelector();
            selector.AddSurrogate(typeof(Color), new StreamingContext(StreamingContextStates.All),
                                  new ColorSerializationSurrogate());
            formatter.SurrogateSelector = selector;
        }
    }
}
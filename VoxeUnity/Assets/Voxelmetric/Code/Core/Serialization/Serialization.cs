using System.IO;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core.Serialization
{
    public static class Serialization
    {
        private static string SaveLocation(string worldName)
        {
            string saveLocation = Directories.SaveFolder + "/" + worldName + "/";

            if (!Directory.Exists(saveLocation))
                Directory.CreateDirectory(saveLocation);

            return saveLocation;
        }

        private static string FileName(Vector3Int chunkLocation)
        {
            string fileName = chunkLocation.x + "," + chunkLocation.y + "," + chunkLocation.z + ".bin";
            return fileName;
        }

        private static string SaveFileName(Chunk chunk)
        {
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

            // Once the blocks in the save are added they're marked as unmodified so
            // as not to trigger a new save on unload unless new blocks are added.
            for (int i = 0; i<save.blocks.Length; i++)
            {
                ushort type = save.blocks[i].Type;
                BlockPos pos = save.positions[i];
                chunk.blocks.Set(new Vector3Int(pos.x,pos.y,pos.z), new BlockData(type));
            }

            return true;
        }

        private static bool Write(Save save)
        {
            if (!save.changed)
                return true;

            string saveFile = SaveFileName(save.Chunk);
            return FileHelpers.BinarizeToFile(saveFile, save);
        }

        private static Save Read(Chunk chunk)
        {
            string saveFile = SaveFileName(chunk);
            if (!File.Exists(saveFile))
                return null;

            Save s = new Save(chunk);
            if (!FileHelpers.DebinarizeFromFile(saveFile, s))
                return null;

            return s;
        }
    }
}
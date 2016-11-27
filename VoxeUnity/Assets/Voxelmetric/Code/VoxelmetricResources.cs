using System.Collections.Generic;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Load_Resources.Textures;

namespace Voxelmetric.Code
{
    public class VoxelmetricResources
    {
        public readonly System.Random random = new System.Random();

        // Worlds can use different block and texture indexes or share them so they are mapped here to
        // the folders they're loaded from so that a world can create a new index or if it uses the same
        // folder to fetch and index it can return an existing index and avoid building it again

        /// <summary>
        /// A map of texture indexes with the folder they're built from
        /// </summary>
        public readonly Dictionary<string, TextureProvider> TextureProviders = new Dictionary<string, TextureProvider>();

        public TextureProvider GetTextureProvider(World world)
        {
            // Check for the folder in the dictionary and if it doesn't exist create it
            TextureProvider textureProvider;
            if (TextureProviders.TryGetValue(world.config.textureFolder, out textureProvider))
                return textureProvider;

            textureProvider = TextureProvider.Create(world.config);
            TextureProviders.Add(world.config.textureFolder, textureProvider);
            return textureProvider;
        }

        /// <summary>
        /// A map of block indexes with the folder they're built from
        /// </summary>
        public readonly Dictionary<string, BlockProvider> BlockProviders = new Dictionary<string, BlockProvider>();

        public BlockProvider GetBlockProvider(World world)
        {
            //Check for the folder in the dictionary and if it doesn't exist create it
            BlockProvider blockProvider;
            if (BlockProviders.TryGetValue(world.config.blockFolder, out blockProvider))
                return blockProvider;

            blockProvider = BlockProvider.Create(world.config.blockFolder, world);
            BlockProviders.Add(world.config.blockFolder, blockProvider);
            return blockProvider;
        }

    }
}

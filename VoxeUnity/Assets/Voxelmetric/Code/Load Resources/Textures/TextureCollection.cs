using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public class TextureCollection
    {
        public readonly string m_textureName;
        private readonly TextureConfigType m_textureType;
        private readonly List<Rect> m_uvs;

        public TextureCollection(string name, TextureConfigType type)
        {
            m_textureName = name;
            m_textureType = type;

            if (m_textureType == TextureConfigType.Connected)
            {
                m_uvs = new List<Rect>(48);
                for (int i=0; i<48; i++)
                    m_uvs.Add(new Rect());
            }
            else
                m_uvs = new List<Rect>();
        }

        public void AddTexture(Rect uvs, TextureConfig.Texture texture)
        {
            switch (m_textureType)
            {
                case TextureConfigType.Connected:
                    m_uvs[texture.index] = uvs;
                    break;
                default:
                    if (texture.weight <= 0)
                        texture.weight = 1;
                    // Add the texture multiple times to raise the chance it's selected randomly
                    for (int i = 0; i < texture.weight; i++)
                        m_uvs.Add(uvs);
                    break;
            }
        }

        public Rect GetTexture(Chunk chunk, ref Vector3Int localPos, Direction direction)
        {
            if (m_uvs.Count == 1)
                return m_uvs[0];

            if (m_textureType==TextureConfigType.Connected)
            {
                ChunkBlocks blocks = chunk.Blocks;
                int localPosIndex = Helpers.GetChunkIndex1DFrom3D(localPos.x, localPos.y, localPos.z);
                ushort blockType = blocks.Get(localPosIndex).Type;

                // Side blocks
                bool n_, _e, s_, _w;
                // Corner blocks
                bool nw, ne, se, sw;

                int index1, index2, index3;
                int sizeWithPadding = chunk.SideSize + Env.ChunkPadding2;
                int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

                switch (direction)
                {
                    case Direction.up:
                        index1 = localPosIndex + sizeWithPaddingPow2;   // + (0,1,0)
                        index2 = index1 - sizeWithPadding;              // - (0,0,1)
                        index3 = index1 + sizeWithPadding;              // + (0,0,1)

                        sw = blocks.Get(index2 - 1).Type==blockType;    // -1,1,-1
                        s_ = blocks.Get(index2).Type == blockType;      //  0,1,-1
                        se = blocks.Get(index2 + 1).Type == blockType;  //  1,1,-1
                        _w = blocks.Get(index1 - 1).Type == blockType;  // -1,1, 0
                        _e = blocks.Get(index1 + 1).Type == blockType;  //  1,1, 0
                        nw = blocks.Get(index3 - 1).Type == blockType;  // -1,1, 1
                        n_ = blocks.Get(index3).Type == blockType;      //  0,1, 1
                        ne = blocks.Get(index3 + 1).Type == blockType;  //  1,1, 1
                        break;
                    case Direction.down:
                        index1 = localPosIndex - sizeWithPaddingPow2;   // - (0,1,0)
                        index2 = index1 - sizeWithPadding;              // - (0,0,1)
                        index3 = index1 + sizeWithPadding;              // + (0,0,1)

                        sw = blocks.Get(index2 - 1).Type == blockType; // -1,-1,-1
                        s_ = blocks.Get(index2).Type == blockType;     //  0,-1,-1
                        se = blocks.Get(index2 + 1).Type == blockType; //  1,-1,-1
                        _w = blocks.Get(index1 - 1).Type == blockType; // -1,-1, 0
                        _e = blocks.Get(index1 + 1).Type == blockType; //  1,-1, 0
                        nw = blocks.Get(index3 - 1).Type == blockType; // -1,-1, 1
                        n_ = blocks.Get(index3).Type == blockType;     //  0,-1, 1
                        ne = blocks.Get(index3 + 1).Type == blockType; //  1,-1, 1
                        break;
                    case Direction.north:
                        index1 = localPosIndex + sizeWithPadding;   // + (0,0,1)
                        index2 = index1 - sizeWithPaddingPow2;      // - (0,1,0)
                        index3 = index1 + sizeWithPaddingPow2;      // + (0,1,0)

                        sw = blocks.Get(index2 - 1).Type == blockType; // -1,-1,1
                        se = blocks.Get(index2 + 1).Type == blockType; //  1,-1,1
                        _w = blocks.Get(index1 - 1).Type == blockType; // -1, 0,1
                        _e = blocks.Get(index1 + 1).Type == blockType; //  1, 0,1
                        nw = blocks.Get(index3 - 1).Type == blockType; // -1, 1,1
                        s_ = blocks.Get(index2).Type == blockType;     //  0,-1,1
                        n_ = blocks.Get(index3).Type == blockType;     //  0, 1,1
                        ne = blocks.Get(index3 + 1).Type == blockType; //  1, 1,1
                        break;
                    case Direction.south:
                        index1 = localPosIndex - sizeWithPadding;   // - (0,0,1)
                        index2 = index1 - sizeWithPaddingPow2;      // - (0,1,0)
                        index3 = index1 + sizeWithPaddingPow2;      // + (0,1,0)

                        sw = blocks.Get(index2 - 1).Type == blockType; // -1,-1,-1
                        se = blocks.Get(index2 + 1).Type == blockType; //  1,-1,-1
                        _w = blocks.Get(index1 - 1).Type == blockType; // -1, 0,-1
                        _e = blocks.Get(index1 + 1).Type == blockType; //  1, 0,-1
                        nw = blocks.Get(index3 - 1).Type == blockType; // -1, 1,-1
                        s_ = blocks.Get(index2).Type == blockType;     //  0,-1,-1
                        n_ = blocks.Get(index3).Type == blockType;     //  0, 1,-1
                        ne = blocks.Get(index3 + 1).Type == blockType; //  1, 1,-1
                        break;
                    case Direction.east:
                        index1 = localPosIndex + 1;             // + (1,0,0)
                        index2 = index1 - sizeWithPaddingPow2;  // - (0,1,0)
                        index3 = index1 + sizeWithPaddingPow2;  // + (0,1,0)

                        sw = blocks.Get(index2 - sizeWithPadding).Type == blockType; // 1,-1,-1
                        s_ = blocks.Get(index2).Type == blockType;                   // 1,-1, 0
                        se = blocks.Get(index2 + sizeWithPadding).Type == blockType; // 1,-1, 1
                        _w = blocks.Get(index1 - sizeWithPadding).Type == blockType; // 1, 0,-1
                        _e = blocks.Get(index1 + sizeWithPadding).Type == blockType; // 1, 0, 1
                        nw = blocks.Get(index3 - sizeWithPadding).Type == blockType; // 1, 1,-1
                        n_ = blocks.Get(index3).Type == blockType;                   // 1, 1, 0
                        ne = blocks.Get(index3 + sizeWithPadding).Type == blockType; // 1, 1, 1
                        break;
                    default://case Direction.west:
                        index1 = localPosIndex - 1;             // - (1,0,0)
                        index2 = index1 - sizeWithPaddingPow2;  // - (0,1,0)
                        index3 = index1 + sizeWithPaddingPow2;  // + (0,1,0)

                        sw = blocks.Get(index2 - sizeWithPadding).Type == blockType; // -1,-1,-1
                        s_ = blocks.Get(index2).Type == blockType;                   // -1,-1, 0
                        se = blocks.Get(index2 + sizeWithPadding).Type == blockType; // -1,-1, 1
                        _w = blocks.Get(index1 - sizeWithPadding).Type == blockType; // -1, 0,-1
                        _e = blocks.Get(index1 + sizeWithPadding).Type == blockType; // -1, 0, 1
                        nw = blocks.Get(index3 - sizeWithPadding).Type == blockType; // -1, 1,-1
                        n_ = blocks.Get(index3).Type == blockType;                   // -1, 1, 0
                        ne = blocks.Get(index3 + sizeWithPadding).Type == blockType; // -1, 1, 1
                        break;
                }

                int uvIndex = ConnectedTextures.GetTexture(n_, _e, s_, _w, nw, ne, se, sw);
                return m_uvs[uvIndex];
            }

            if (m_uvs.Count > 1)
            {
                int hash = localPos.GetHashCode();
                if (hash < 0)
                    hash *= -1;

                float randomNumber = (hash % 100) /100f;
                randomNumber *= m_uvs.Count;

                return m_uvs[(int)randomNumber];
            }

            Debug.LogError("There were no textures for " + m_textureName);
            return new Rect();
        }
    }
}

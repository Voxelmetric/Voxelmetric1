using UnityEngine;
using System.Collections;
using System;

public static class Blocks {

    public static Block[] types = new Block[] { new BlockAir(), new Stone(), new Dirt(), new Grass(), new Log(), new Leaves(), new Sand() };
    public static string[] typeNames = new string[] { "air", "stone", "dirt", "grass", "log", "leaves", "sand" };
    public static BlockType[] blockTypes = new BlockType[] { BlockType.air, BlockType.stone, BlockType.dirt, BlockType.grass, BlockType.log, BlockType.leaves, BlockType.sand };

    public static BlockType TypeFromString(string type){
        for(int i=0; i<typeNames.Length; i++){
            if(type.ToLower() == typeNames[i].ToLower()){
                return blockTypes[i];
            }
        }

        return 0;
    }

}

[Serializable]
public struct SBlock
{
    public readonly byte type;
    public byte data1;
    public bool modified;

    public SBlock(BlockType type)
    {
        this.type = (byte)type;
        modified = true;
        data1 = 0;
    }

    public SBlock(byte type)
    {
        this.type = type;
        modified = true;
        data1 = 0;
    }

    public void BuildBlock(Chunk chunk, BlockPos pos, MeshData meshData)
    {
        Block().BuildBlock(chunk, pos, meshData, this);
    }

    public static implicit operator Block(SBlock sBlock)
    {
        return Blocks.types[sBlock];
    }

    public static implicit operator int(SBlock sBlock)
    {
        return sBlock.type;
    }

    public static implicit operator byte(SBlock sBlock)
    {
        return sBlock.type;
    }

    public Block Block()
    {
        return this;
    }

    public override string ToString()
    {
        return Blocks.typeNames[this];
    }
}

public enum BlockType{air, stone, dirt, grass, log, leaves, sand}
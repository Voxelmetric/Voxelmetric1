using UnityEngine;
using System.Collections;
using System;

[Serializable]
public struct BlockPos
{
    public int x, y, z;

    public BlockPos(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 47;
            // Suitable nullity checks etc, of course :)
            hash = hash * 227 + x.GetHashCode();
            hash = hash * 227 + y.GetHashCode();
            hash = hash * 227 + z.GetHashCode();
            return hash;
        }
    }

    public override bool Equals(object obj)
    {
        if (GetHashCode() == obj.GetHashCode())
            return true;
        return false;
    }

    public static BlockPos Vec3(Vector3 vec3)
    {
        return Terrain.GetBlockPos(vec3);
    }

    public static implicit operator BlockPos(Vector3 v)
    {
        return Terrain.GetBlockPos(v);
    }

    public static implicit operator Vector3(BlockPos wp)
    {
        return new Vector3(wp.x, wp.y, wp.z);
    }

    public BlockPos Add(int x, int y, int z)
    {
        return new BlockPos(this.x + x, this.y + y, this.z + z);
    }

    public BlockPos Add(BlockPos pos)
    {
        return new BlockPos(this.x + pos.x, this.y + pos.y, this.z + pos.z);
    }

    public BlockPos Subtract(BlockPos pos)
    {
        return new BlockPos(this.x - pos.x, this.y - pos.y, this.z - pos.z);
    }

    public override string ToString()
    {
        return "(" + x + ", " + y + ", " + z + ")";
    }
}
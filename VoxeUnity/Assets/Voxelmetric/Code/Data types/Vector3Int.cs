using System;
using UnityEngine;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Data_types
{
    [Serializable]
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public readonly int x, y, z;

        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Int Add(int x, int y, int z)
        {
            return new Vector3Int(this.x + x, this.y + y, this.z + z);
        }

        public Vector3Int Add(Vector3Int pos)
        {
            return new Vector3Int(x + pos.x, y + pos.y, z + pos.z);
        }

        public Vector3Int Subtract(Vector3Int pos)
        {
            return new Vector3Int(x - pos.x, y - pos.y, z - pos.z);
        }

        public Vector3Int Negate()
        {
            return new Vector3Int(-x, -y, -z);
        }

        public byte[] ToBytes()
        {
            byte[] BX = BitConverter.GetBytes(x);
            byte[] BY = BitConverter.GetBytes(y);
            byte[] BZ = BitConverter.GetBytes(z);

            return new[] {
                BX[0], BX[1], BX[2], BX[3],
                BY[0], BY[1], BY[2], BY[3],
                BZ[0], BZ[1], BZ[2], BZ[3]};
        }

        public static Vector3Int FromBytes(byte[] bytes, int offset)
        {
            return new Vector3Int(
                BitConverter.ToInt32(bytes, offset),
                BitConverter.ToInt32(bytes, offset + 4),
                BitConverter.ToInt32(bytes, offset + 8));
        }

        //BlockPos and Vector3 can be substituted for one another
        public static implicit operator Vector3Int(Vector3 v)
        {
            Vector3Int vector3Int = new Vector3Int(
                Mathf.RoundToInt(v.x * Env.BlockSizeInv),
                Mathf.RoundToInt(v.y * Env.BlockSizeInv),
                Mathf.RoundToInt(v.z * Env.BlockSizeInv)
                );

            return vector3Int;
        }

        public static implicit operator Vector3(Vector3Int pos)
        {
            return new Vector3(pos.x, pos.y, pos.z) * Env.BlockSize;
        }

        public static implicit operator Vector3Int(Direction d)
        {
            switch (d) {
                case Direction.up:
                    return new Vector3Int(0, 1, 0);
                case Direction.down:
                    return new Vector3Int(0, -1, 0);
                case Direction.north:
                    return new Vector3Int(0, 0, 1);
                case Direction.east:
                    return new Vector3Int(1, 0, 0);
                case Direction.south:
                    return new Vector3Int(0, 0, -1);
                case Direction.west:
                    return new Vector3Int(-1, 0, 0);
                default:
                    return new Vector3Int();
            }
        }

        //These operators let you add and subtract BlockPos from each other
        //or check equality with == and !=
        public static Vector3Int operator -(Vector3Int pos1, Vector3Int pos2)
        {
            return pos1.Subtract(pos2);
        }

        public static Vector3Int operator -(Vector3Int pos) {
            return pos.Negate();
        }

        public static bool operator >(Vector3Int pos1, Vector3Int pos2)
        {
            return (pos1.x > pos2.x || pos1.y > pos2.y || pos1.z > pos2.z);
        }

        public static bool operator <(Vector3Int pos1, Vector3Int pos2)
        {
            return (pos1.x < pos2.x || pos1.y < pos2.y || pos1.z < pos2.z);
        }

        public static bool operator >=(Vector3Int pos1, Vector3Int pos2)
        {
            return (pos1.x >= pos2.x || pos1.y >= pos2.y || pos1.z >= pos2.z);
        }

        public static bool operator <=(Vector3Int pos1, Vector3Int pos2)
        {
            return (pos1.x <= pos2.x || pos1.y <= pos2.y || pos1.z <= pos2.z);
        }

        public static Vector3Int operator +(Vector3Int pos1, Vector3Int pos2)
        {
            return pos1.Add(pos2);
        }

        public static Vector3Int operator *(Vector3Int pos, int i)
        {
            return new Vector3Int(pos.x * i, pos.y * i, pos.z * i);
        }

        public static Vector3Int operator *(Vector3Int pos1, Vector3Int pos2)
        {
            return new Vector3Int(pos1.x * pos2.x, pos1.y * pos2.y, pos1.z * pos2.z);
        }

        #region Struct comparison

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x;
                hashCode = (hashCode*397)^y;
                hashCode = (hashCode*397)^z;
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Vector3Int && Equals((Vector3Int)obj);
        }

        public bool Equals(Vector3Int other)
        {
            return x==other.x && y==other.y && z==other.z;
        }

        public static bool operator ==(Vector3Int pos1, Vector3Int pos2)
        {
            return pos1.x==pos2.x && pos1.y==pos2.y && pos1.z==pos2.z;
        }

        public static bool operator !=(Vector3Int pos1, Vector3Int pos2)
        {
            return !(pos1 == pos2);
        }

        #endregion

        //You can safely use BlockPos as part of a string like this:
        //"block at " + BlockPos + " is broken."
        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ")";
        }

        //
        // Summary:
        //     Shorthand for writing BlockPos(0, 0, 0).
        public static readonly Vector3Int zero = new Vector3Int(0, 0, 0);
        //
        // Summary:
        //     Shorthand for writing BlockPos(1, 1, 1).
        public static readonly Vector3Int one = new Vector3Int(1, 1, 1);
    }
}

using System;
using UnityEngine;

namespace Voxelmetric.Code.Data_types
{
    [Serializable]
    public struct Vector3Int : IEquatable<Vector3Int>
    {
        public static readonly Vector3Int zero = new Vector3Int(0, 0, 0);
        public static readonly Vector3Int one = new Vector3Int(1, 1, 1);
        public static readonly Vector3Int up = new Vector3Int(0, 1, 0);
        public static readonly Vector3Int down = new Vector3Int(0, -1, 0);
        public static readonly Vector3Int north = new Vector3Int(0, 0, 1);
        public static readonly Vector3Int south = new Vector3Int(0, 0, -1);
        public static readonly Vector3Int east = new Vector3Int(1, 0, 0);
        public static readonly Vector3Int west = new Vector3Int(-1, 0, 0);

        public int x, y, z;

        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
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
                Mathf.RoundToInt(v.x),
                Mathf.RoundToInt(v.y),
                Mathf.RoundToInt(v.z)
                );

            return vector3Int;
        }

        public static implicit operator Vector3(Vector3Int pos)
        {
            return new Vector3(pos.x, pos.y, pos.z);
        }

        public static implicit operator Vector3Int(Direction d)
        {
            switch (d) {
                case Direction.up:
                    return up;
                case Direction.down:
                    return down;
                case Direction.north:
                    return north;
                case Direction.south:
                    return south;
                case Direction.east:
                    return east;
                default:// Direction.west:
                    return west;
            }
        }

        public int Distance2(ref Vector3Int pos)
        {
            int xx = x - pos.x;
            int yy = y - pos.y;
            int zz = z - pos.z;
            return xx*xx + yy*yy + zz*zz;
        }

        public static int Distance2(ref Vector3Int pos1, ref Vector3Int pos2)
        {
            int xx = pos1.x - pos2.x;
            int yy = pos1.y - pos2.y;
            int zz = pos1.z - pos2.z;
            return xx*xx + yy*yy + zz*zz;
        }

        public static Vector3Int operator -(Vector3Int pos)
        {
            Vector3Int v;
            v.x = -pos.x;
            v.y = -pos.y;
            v.z = -pos.z;
            return v;
        }

        public static Vector3Int operator -(Vector3Int pos, int i)
        {
            Vector3Int v;
            v.x = pos.x - i;
            v.y = pos.y - i;
            v.z = pos.z - i;
            return v;
        }

        public static Vector3Int operator -(Vector3Int pos1, Vector3Int pos2)
        {
            Vector3Int v;
            v.x = pos1.x - pos2.x;
            v.y = pos1.y - pos2.y;
            v.z = pos1.z - pos2.z;
            return v;
        }

        public Vector3Int Sub(int x, int y, int z)
        {
            Vector3Int v;
            v.x = this.x - x;
            v.y = this.y - y;
            v.z = this.z - z;
            return v;
        }

        public static Vector3Int operator +(Vector3Int pos, int i)
        {
            Vector3Int v;
            v.x = pos.x + i;
            v.y = pos.y + i;
            v.z = pos.z + i;
            return v;
        }

        public static Vector3Int operator +(Vector3Int pos1, Vector3Int pos2)
        {
            Vector3Int v;
            v.x = pos1.x + pos2.x;
            v.y = pos1.y + pos2.y;
            v.z = pos1.z + pos2.z;
            return v;
        }

        public Vector3Int Add(int x, int y, int z)
        {
            Vector3Int v;
            v.x = this.x + x;
            v.y = this.y + y;
            v.z = this.z + z;
            return v;
        }

        public static Vector3Int operator *(Vector3Int pos, int i)
        {
            Vector3Int v;
            v.x = pos.x * i;
            v.y = pos.y * i;
            v.z = pos.z * i;
            return v;
        }

        public static Vector3Int operator *(Vector3Int pos1, Vector3Int pos2)
        {
            Vector3Int v;
            v.x = pos1.x * pos2.x;
            v.y = pos1.y * pos2.y;
            v.z = pos1.z * pos2.z;
            return v;
        }

        public Vector3Int Mul(int x, int y, int z)
        {
            Vector3Int v;
            v.x = this.x * x;
            v.y = this.y * y;
            v.z = this.z * z;
            return v;
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

        public static bool operator ==(Vector3Int a, Vector3Int b)
        {
            return a.x==b.x && a.y==b.y && a.z==b.z;
        }

        public static bool operator !=(Vector3Int a, Vector3Int b)
        {
            return a.x!=b.x || a.y!=b.y || a.z!=b.z;
        }

        #endregion
        
        public override string ToString()
        {
            return "[" + x + ", " + y + ", " + z + "]";
        }
    }
}

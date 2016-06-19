using System;
using System.IO;
using UnityEngine;

namespace Voxelmetric.Code.Common.IO
{
    public static class FileHelpers
    {
        public static bool SaveToFile(string targetFilePath, byte[] data)
        {
            try
            {
                using (FileStream fs = new FileStream(targetFilePath, FileMode.Create))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }

            return true;
        }

        public static bool SaveToFile(string targetFilePath, MemoryStream streamData)
        {
            try
            {
                using (FileStream fs = new FileStream(targetFilePath, FileMode.Create))
                {
                    fs.Write(streamData.GetBuffer(), 0, (int)streamData.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }

            return true;
        }

        public static bool LoadFromFile(string targetFilePath, out byte[] data)
        {
            using (FileStream fs = new FileStream(targetFilePath, FileMode.Open))
            {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
            }

            return true;
        }

        public static bool LoadFromFile(string targetFilePath, out MemoryStream ms)
        {
            using (FileStream fs = new FileStream(targetFilePath, FileMode.Open))
            {
                ms = new MemoryStream((int)fs.Length);
                fs.Read(ms.GetBuffer(), 0, (int)fs.Length);
            }

            return true;
        }

        public static bool WriteAllBytes(string path, byte[] data)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(path, FileMode.Create);
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    bw.Write(data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }

            return true;
        }

        public static bool BinarizeToFile(string targetFilePath, IBinarizable stream)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(targetFilePath, FileMode.Create);
                using (var bw = new BinaryWriter(fs))
                {
                    fs = null;
                    stream.Binarize(bw);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }

            return true;
        }

        public static bool DebinarizeFromFile(string targetFilePath, IBinarizable stream)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(targetFilePath, FileMode.Open);
                using (var br = new BinaryReader(fs))
                {
                    fs = null;
                    stream.Debinarize(br);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (fs!=null)
                    fs.Dispose();
            }

            return true;
        }
    }
}
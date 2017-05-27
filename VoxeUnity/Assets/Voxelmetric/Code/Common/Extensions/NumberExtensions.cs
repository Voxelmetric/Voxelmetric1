using System.Globalization;

namespace Voxelmetric.Code.Common.Extensions
{
    public static class NumberExtensions
    {
        private static readonly string[] suffixes = { "B", "KiB", "MiB", "GiB", "TiB" };

        public static string GetKiloString (this int value)
        {
            int i;
            double dblSByte = 0;
            for (i = 0; value / 1024 > 0; i++, value /= 1024)
                dblSByte = value / 1024.0;
            return string.Format("{0} {1}", dblSByte.ToString("0.00",CultureInfo.InvariantCulture), suffixes [i]);
        }
	
        public static string GetKiloString (this long value)
        {
            int i;
            double dblSByte = 0;
            for (i = 0; (int)(value / 1024) > 0; i++, value /= 1024)
                dblSByte = value / 1024.0;
            return string.Format("{0} {1}", dblSByte.ToString("0.00", CultureInfo.InvariantCulture), suffixes[i]);
        }
    }
}

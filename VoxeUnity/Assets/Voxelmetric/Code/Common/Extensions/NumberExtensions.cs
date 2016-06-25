namespace Assets.Engine.Scripts.Common.Extensions
{
    public static class NumberExtensions
    {
        public static string GetKiloString (this int value)
        {
            int i;
            string[] suffixes = { "B", "KiB", "MiB", "GiB", "TiB" };
            double dblSByte = 0;
            for (i = 0; value / 1024 > 0; i++, value /= 1024)
                dblSByte = value / 1024.0;
            return dblSByte.ToString ("0.00") + suffixes [i];
        }
	
        public static string GetKiloString (this long value)
        {
            int i;
            string[] suffixes = { "B", "KiB", "iMB", "GiB", "TiB" };
            double dblSByte = 0;
            for (i = 0; (int)(value / 1024) > 0; i++, value /= 1024)
                dblSByte = value / 1024.0;
            return dblSByte.ToString ("0.00") + suffixes [i];
        }
    }
}

using UnityEngine;
using Voxelmetric.Code.Common.Collections;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core.Clipmap
{
    public class Clipmap
    {
        private readonly AxisInfo[] m_axes;
        private readonly int m_diffCachedVisibleRange;

        public int VisibleRange { get; private set; }
        public int CachedRange { get; private set; }
        public int RangeYMin { get; private set; }
        public int RangeYMax { get; private set; }

        public Clipmap(int rangeYMin, int rangeYMax, int visibleRange, int cachedRange)
        {
            VisibleRange = visibleRange;
            CachedRange = cachedRange;

            m_diffCachedVisibleRange = CachedRange - VisibleRange;

            if (rangeYMin < -CachedRange)
                rangeYMin = -CachedRange;
            if (rangeYMax > CachedRange)
                rangeYMax = CachedRange;

            RangeYMin = rangeYMin;
            RangeYMax = rangeYMax;

            m_axes = new[]
            {
                new AxisInfo
                {
                    Map = new CircularArray1D<ClipmapItem>(2*CachedRange+1),
                    RangeMin = -CachedRange,
                    RangeMax = CachedRange
                },
                new AxisInfo
                {
                    Map = new CircularArray1D<ClipmapItem>(2*(rangeYMax-rangeYMin)+1),
                    RangeMin = rangeYMin,
                    RangeMax = rangeYMax
                },
                new AxisInfo
                {
                    Map = new CircularArray1D<ClipmapItem>(2*CachedRange+1),
                    RangeMin = -CachedRange,
                    RangeMax = CachedRange
                }
            };
        }
        
        public ClipmapItem this[int x, int y, int z]
        {
            get
            {
                /*int absX = Mathf.Abs(x + m_axes[0].Map.Offset);
                int absY = Mathf.Abs(y + m_axes[1].Map.Offset);
                int absZ = Mathf.Abs(z + m_axes[2].Map.Offset);

                if (absX > absZ)
                    return absX > absY ? m_axes[0].Map[absX] : m_axes[1].Map[absY];

                return absZ > absY ? m_axes[2].Map[absZ] : m_axes[1].Map[absY];*/
                int absX = Mathf.Abs(x + m_axes[0].Map.Offset);
                int absZ = Mathf.Abs(z + m_axes[2].Map.Offset);

                return absX > absZ ? m_axes[0].Map[absX] : m_axes[2].Map[absZ];
            }
        }

        public void Init(int forceLOD, float coefLOD)
        {
            // Generate clipmap fields. It is enough to generate them for one dimension for clipmap is symetrical in all m_axes
            for (int axis = 0; axis<3; axis++)
            {
                var axisInfo = m_axes[axis];
                for (int distance = axisInfo.RangeMin; distance<=axisInfo.RangeMax; distance++)
                {
                    int lod = DetermineLOD(distance, forceLOD, coefLOD);
                    bool isInVisibilityRange = IsWithinVisibilityRange(axisInfo, distance);
                    bool isInCacheRange = IsWithinCachedRange(axisInfo, distance);

                    axisInfo.Map[distance] = new ClipmapItem
                    {
                        LOD = lod,
                        IsWithinVisibleRange = isInVisibilityRange,
                        IsWithinCachedRange = isInCacheRange
                    };
                }
            }
        }

        public void SetOffset(int x, int y, int z)
        {
            m_axes[0].Map.Offset = -x;
            m_axes[1].Map.Offset = -y;
            m_axes[2].Map.Offset = -z;
        }

        public bool IsInsideBounds(int x, int y, int z)
        {
            int xx = x + m_axes[0].Map.Offset;
            int yy = y + m_axes[1].Map.Offset;
            int zz = z + m_axes[2].Map.Offset;
            return IsWithinCachedRange(m_axes[0], xx) && IsWithinCachedRange(m_axes[1], yy) && IsWithinCachedRange(m_axes[2], zz);
        }

        private static int DetermineLOD(int distance, int forceLOD, float coefLOD)
        {
            int lod = 0;

            if (forceLOD >= 0)
            {
                lod = forceLOD;
            }
            else
            {
                if (coefLOD <= 0)
                    return 0;

                // Pick the greater distance and choose a proper LOD
                int dist = Mathf.Abs(distance);
                lod = (int)(dist / (coefLOD * Env.ChunkPower));
            }

            // LOD can't be bigger than chunk size
            if (lod < 0)
                lod = 0;
            if (lod > Env.ChunkPower)
                lod = Env.ChunkPower;

            return lod;
        }

        private bool IsWithinVisibilityRange(AxisInfo axis, int distance)
        {
            int rangeMin = axis.RangeMin + m_diffCachedVisibleRange;
            int rangeMax = axis.RangeMax - m_diffCachedVisibleRange;
            return distance >= rangeMin && distance <= rangeMax;
        }

        private bool IsWithinCachedRange(AxisInfo axis, int distance)
        {
            int rangeMin = axis.RangeMin;
            int rangeMax = axis.RangeMax;
            return distance >= rangeMin && distance <= rangeMax;
        }

        private class AxisInfo
        {
            public CircularArray1D<ClipmapItem> Map; // -N ... 0 ... N
            public int RangeMax;
            public int RangeMin;
        }
    }
}
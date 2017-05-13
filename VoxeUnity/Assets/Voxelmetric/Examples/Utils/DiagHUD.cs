using System;
using System.Collections;
using System.Text;
using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Core;

namespace Client.Scripts.Misc
{
    [ExecuteInEditMode]
    public class DiagHUD : MonoBehaviour
    {
        public bool Show = true;
        public bool ShowInEditor = false;
        public World World;

        private bool m_stop = false;
        private float m_lastCollect = 0;
        private float m_lastCollectNum = 0;
        private float m_delta = 0;
        private float m_lastDeltaTime = 0;
        private long m_allocRate = 0;
        private long m_lastAllocMemory = 0;
        private float m_lastAllocSet = -9999;
        private long m_allocMem = 0;
        private long m_collectAlloc = 0;
        private long m_peakAlloc = 0;

        private readonly StringBuilder m_text = new StringBuilder();

        void Start()
        {
            useGUILayout = false;

            StartCoroutine(OnUpdate());
        }

        void OnDestroy()
        {
            m_stop = true;
        }

        public IEnumerator OnUpdate()
        {
            while (!m_stop)
            {
                int collCount = GC.CollectionCount(0);

                if (Math.Abs(m_lastCollectNum - collCount) > Mathf.Epsilon)
                {
                    m_lastCollectNum = collCount;
                    m_delta = Time.realtimeSinceStartup - m_lastCollect;
                    m_lastCollect = Time.realtimeSinceStartup;
                    m_lastDeltaTime = Time.deltaTime;
                    m_collectAlloc = m_allocMem;
                }

                m_allocMem = GC.GetTotalMemory(false);

                m_peakAlloc = m_allocMem > m_peakAlloc ? m_allocMem : m_peakAlloc;

                if (!(Time.realtimeSinceStartup - m_lastAllocSet > 0.3f))
                    yield return new WaitForSeconds(1.0f);

                long diff = m_allocMem - m_lastAllocMemory;
                m_lastAllocMemory = m_allocMem;
                m_lastAllocSet = Time.realtimeSinceStartup;

                if (diff >= 0)
                {
                    m_allocRate = diff;
                }

                yield return new WaitForSeconds(1.0f);
            }
        }

        // Use this for initialization
        public void OnGUI()
        {
            if (!Show || !Application.isPlaying && !ShowInEditor)
                return;

            m_text.Remove(0, m_text.Length);

            m_text.AppendFormat("Currently allocated: {0}\n", m_allocMem.GetKiloString());
            m_text.AppendFormat("Peak allocated: {0}\n", m_peakAlloc.GetKiloString());
            m_text.AppendFormat("Last collected: {0}\n", m_collectAlloc.GetKiloString());
            m_text.AppendFormat("Allocation rate: {0}\n", m_allocRate.GetKiloString());

            m_text.AppendFormat("Collection freq: {0:0.00}s\n", m_delta);
            m_text.AppendFormat("Last collect delta: {0:0.000}s ({1:0.0} FPS)\n", m_lastDeltaTime, 1f/m_lastDeltaTime);

            if (World != null)
            {
                int chunks = World.chunks.Count;
                m_text.AppendFormat("Chunks: {0}\n", chunks);
            }

            // Tasks
            m_text.AppendFormat("TP tasks: {0}\n", WorkPoolManager.ToString());
            m_text.AppendFormat("IO tasks: {0}\n", IOPoolManager.ToString());
            
            // Individual object pools
            for(int i=0; i<Globals.WorkPool.Size; i++)
                m_text.AppendLine("TP #"+i+" pools: "+Globals.WorkPool.GetPool(i)); // thread pool
            m_text.AppendLine("IO pools: " + Globals.IOPool.Pools); // io pool
            m_text.AppendLine("Main pools: " + Globals.MemPools); // the main thread pool

            m_text.AppendLine(GameObjectProvider.Instance.ToString());

            const int width = 720;
            const int height = 320;
            GUI.Box(new Rect(Screen.width-width, Screen.height-height, width, height), "");
            GUI.Label(new Rect(Screen.width-width+10, Screen.height-height+10, width-10, height-10), m_text.ToString());
        }
    }
}
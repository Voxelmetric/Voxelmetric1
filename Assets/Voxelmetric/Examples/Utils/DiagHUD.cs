using System;
using System.Collections;
using System.Text;
using UnityEngine;
using Assets.Engine.Scripts.Common.Extensions;
using Voxelmetric.Code;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Core;

namespace Assets.Client.Scripts.Misc
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
            if (!Show || (!Application.isPlaying && !ShowInEditor))
                return;

            m_text.Remove(0, m_text.Length);

            m_text.AppendFormat("Currently allocated: {0}\n", m_allocMem.GetKiloString());
            m_text.AppendFormat("Peak allocated: {0}\n", m_peakAlloc.GetKiloString());
            m_text.AppendFormat("Last collected: {0}\n", m_collectAlloc.GetKiloString());
            m_text.AppendFormat("Allocation rate: {0}\n", m_allocRate.GetKiloString());

            m_text.AppendFormat("Collection freq: {0}s\n", m_delta.ToString("0.00"));
            m_text.AppendFormat("Last collect delta: {0}s ({1} FPS)\n",
                m_lastDeltaTime.ToString("0.000"),
                (1F / m_lastDeltaTime).ToString("0.0")
                );

            if (World != null)
            {
                int chunks = World.chunks.chunkCollection.Count;
                m_text.AppendFormat("Chunks {0}\n", chunks);
            }
            m_text.AppendFormat("ThreadPool items {0}\n", Globals.WorkPool.Size);
            m_text.AppendFormat("TaskPool items {0}\n", Globals.IOPool.Size);

            m_text.AppendLine(Globals.MemPools.ToString());
            m_text.AppendLine(GameObjectProvider.Instance.ToString());

            const int width = 400;
            const int height = 210;
            GUI.Box(new Rect(Screen.width-width, Screen.height-height, width, height), "");
            GUI.Label(new Rect(Screen.width-width+10, Screen.height-height+10, width-10, height-10), m_text.ToString());
        }
    }
}
using System.Collections;

namespace Voxelmetric.Code.Load_Resources
{
    public struct LayerConfig
    {
        public string name;

        // This is used to sort the layers, low numbers are applied first
        // does not need to be consecutive so use numbers like 100 so that
        // layer you can add layers in between if you have to
        public int index;
        public string layerType;
        public string structure;
        public Hashtable properties;

        public override string ToString()
        {
            return name;
        }
    }
}

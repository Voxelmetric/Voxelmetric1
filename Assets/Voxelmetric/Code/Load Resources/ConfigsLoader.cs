using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Voxelmetric.Code.Load_Resources
{
    public class ConfigLoader<T>
    {
        Dictionary<string, T> configs = new Dictionary<string, T>();
        string[] configFolders;

        public ConfigLoader(string[] folders)
        {
            configFolders = folders;
        }

        void LoadConfigs()
        {
            foreach (var configFolder in configFolders)
            {
                var configFiles = Resources.LoadAll<TextAsset>(configFolder);
                foreach (var configFile in configFiles)
                {
                    T config = JsonConvert.DeserializeObject<T>(configFile.text);
                    if(!configs.ContainsKey(config.ToString()))
                        configs.Add(config.ToString(), config);
                }
            }
        }

        public T GetConfig(string configName)
        {
            if (configs.Keys.Count == 0)
                LoadConfigs();

            T conf;
            if (configs.TryGetValue(configName, out conf))
                return conf;

            Debug.LogError("Config not found for " + configName + ". Using defaults");
            return conf;
        }

        public T[] AllConfigs()
        {
            if (configs.Keys.Count == 0)
                LoadConfigs();

            T[] configValues = new T[configs.Count];
            configs.Values.CopyTo(configValues, 0);
            return configValues;
        }
    }
}

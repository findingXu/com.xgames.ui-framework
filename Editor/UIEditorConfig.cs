using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace XGames.UIFramework
{
    public static class UIEditorConfig
    {
        public static readonly string[] UIPrefabDirectory = { "Assets/res/ui/prefab/", "Assets/Resources/" };
        public static readonly string[] UICodeDirectory = {"../gamelua/ts/ui/"};
        public static readonly string BindHashTag = "//<PREFAB {0}>{1}//</PREFAB>";
        public static readonly string FuncGetPath = "GetPrefabPath";
        public static readonly string FuncBindComponents = "BindComponents";
        public static readonly string FuncGetComponent = "GetComponent";
        public static readonly string FuncGetSubControl = "BindChildControl";
        public static readonly string ClassUIBaseName = "UIWindow|UIControl";

        private static readonly string CacheFilePath = Path.Combine(Application.dataPath, "Editor/ui_cache.json");
        
        public static Dictionary<string, string> ReadBindingCache()
        {
            EnsureCacheFile();
            
            var jsonTxt = File.ReadAllText(CacheFilePath);
            var bindingCache = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonTxt) ?? new Dictionary<string, string>();
            return bindingCache;
        }

        public static void SaveBindingCache(in Dictionary<string, string> bindingCache)
        {
            EnsureCacheFile();
            var cacheStr = JsonConvert.SerializeObject(bindingCache);
            File.WriteAllText(CacheFilePath, cacheStr);
        }

        private static void EnsureCacheFile()
        {
            if (File.Exists(CacheFilePath)) return;
            
            var fileDirectory = Path.GetDirectoryName(CacheFilePath);
            if(!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }
            File.WriteAllBytes(CacheFilePath, Array.Empty<byte>());
        }
    }
}
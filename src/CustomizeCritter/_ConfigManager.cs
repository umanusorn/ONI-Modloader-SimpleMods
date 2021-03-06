﻿using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;
using Harmony;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using System.Linq;

namespace Config
{
    public class MyContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {

            var props1 = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)//.Where(s => s.IsPublic || Attribute.IsDefined(s, typeof(SerializeField)))
                        .Select(f => base.CreateProperty(f, memberSerialization))
                        .ToList();
            props1.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props1;

            // var props2 = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            //             .Select(p => base.CreateProperty(p, memberSerialization))
            //         .Union(type.GetFields(BindingFlags.Public |/* BindingFlags.NonPublic |*/ BindingFlags.Instance)//.Where(s => s.IsPublic || Attribute.IsDefined(s, typeof(SerializeField)))
            //             .Select(f => base.CreateProperty(f, memberSerialization)))
            //         .ToList();
            // props2.ForEach(p => { p.Writable = true; p.Readable = true; });
            // props2.AddRange(props1);
            // return props2;
        }
    }

    public class JsonManager
    {
        public JsonSerializer Serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ContractResolver = new MyContractResolver()
        });

        public T Deserialize<T>(string path)
        {
            T result;

            using (StreamReader streamReader = new StreamReader(path))
            {
                using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
                {
                    result = this.Serializer.Deserialize<T>(jsonReader);

                    jsonReader.Close();
                }

                streamReader.Close();
            }

            return result;
        }
        public void Serialize<T>(T value, string path)
        {
            using (StreamWriter streamReader = new StreamWriter(path))
            {
                using (JsonTextWriter jsonReader = new JsonTextWriter(streamReader))
                {
                    this.Serializer.Serialize(jsonReader, value);

                    jsonReader.Close();
                }

                streamReader.Close();
            }
        }
    }

    public class JsonFileManager
    {
        private readonly JsonManager _jsonManager;


        public JsonManager GetJsonManager()
        {
            return _jsonManager;
        }

        public JsonFileManager(JsonManager jsonManager)
        {
            this._jsonManager = jsonManager;
        }


        public bool TryLoadConfiguration<T>(string path, out T state)
        {
            try
            {
                state = _jsonManager.Deserialize<T>(path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
                Debug.LogWarning("Can't load configuration!");
                BootDialog.PostBootDialog.ErrorList.Add("Error in config file: " + ex.Message);

                state = (T)Activator.CreateInstance(typeof(T));

                return false;
            }
        }

        public bool TrySaveConfiguration<T>(string path, T state)
        {
            try
            {
                _jsonManager.Serialize<T>(state, path);
                return true;
            }
            catch (Exception ex)
            {
                const string Message = "Can't save configuration!";

                Debug.LogWarning(ex);
                Debug.LogWarning(Message);

                return false;
            }
        }
    }

    public class Manager<T>
    {
        public readonly string StateFilePath;

        public readonly JsonFileManager JsonLoader;

        private T _state;

        public Func<T, bool> updateCallback = null;

        public T State
        {
            get
            {
                if (_state != null)
                {
                    return _state;
                }
                Debug.Log("Loading: " + this.StateFilePath);

                if (!File.Exists(this.StateFilePath))
                {
                    Debug.Log(this.StateFilePath + " not found. Creating a default config file...");
                    EnsureDirectoryExists(new FileInfo(this.StateFilePath).Directory.FullName);

                    JsonLoader.TrySaveConfiguration(this.StateFilePath, (T)Activator.CreateInstance(typeof(T)));
                }
                JsonLoader.TryLoadConfiguration(this.StateFilePath, out _state);
                return _state;
            }

            private set
            {
                _state = value;
            }
        }


        public bool TryReloadConfiguratorState()
        {
            T state;
            if (JsonLoader.TryLoadConfiguration(this.StateFilePath, out state))
            {
                State = state;
                return true;
            }

            return false;
        }

        public bool TrySaveConfigurationState()
        {
            if (_state != null)
                return JsonLoader.TrySaveConfiguration(this.StateFilePath, _state);

            return false;
        }

        /// <summary>
        /// if not isAbsolute then path is the mods name
        /// </summary>
        public Manager(string path, bool isAbsolute, Func<T, bool> updateCallback = null)
        {
            bool errorFlag = false;
            string resultPath = null;

            if (isAbsolute)
            {
                resultPath = path;
                EnsureDirectoryExists(new FileInfo(resultPath).Directory.FullName);
                errorFlag = !Directory.Exists(new FileInfo(resultPath).Directory.FullName);
            }

            if (!isAbsolute || errorFlag)
            {
                resultPath = GetFallBackPath(path);
                EnsureDirectoryExists(new FileInfo(path).Directory.FullName);
            }

            this.StateFilePath = resultPath;
            this.JsonLoader = new JsonFileManager(new JsonManager());

            this.updateCallback = updateCallback;

            UpdateVersion();
        }

        public void UpdateVersion()
        {
            try
            {
                T newObj = Activator.CreateInstance<T>();
                int newVersion = (int)typeof(T).GetField("version").GetValue(newObj);
                int savedVersion = (int)typeof(T).GetField("version").GetValue(State);

                if (savedVersion != 0 && newVersion != 0)
                {
                    if (savedVersion != newVersion)
                    {
                        Debug.Log("Updating version...");
                        bool shouldSave = true;
                        if (updateCallback != null) shouldSave = updateCallback(State);
                        typeof(T).GetField("version").SetValue(State, newVersion);
                        if (shouldSave) this.TrySaveConfigurationState();
                    }
                }
            }
            catch (Exception e)
            {
                if (this.StateFilePath != null)
                    Debug.Log("Config.Manager could not check version of: " + Path.GetFileName(this.StateFilePath) + "\n" + e.Message);
                return;
            }
        }

        /// <summary>
        /// name: file name without extension
        /// returns file-path to save config, located in root mod folder; NOT TESTED
        /// </summary>
        private static string GetKleiDocs(string name)
        {
            //return System.getProperty("user.home") + Path.DirectorySeparatorChar + "Documents" + Path.DirectorySeparatorChar
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar
            + "Klei" + Path.DirectorySeparatorChar
            + "OxygenNotIncluded" + Path.DirectorySeparatorChar
            + "mods" + Path.DirectorySeparatorChar
            + name + ".json";
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public static string GetFallBackPath(string path)
        {
            string name = Path.GetFileNameWithoutExtension(Path.GetFileName(path));
            return "Mods" + Path.DirectorySeparatorChar + name + Path.DirectorySeparatorChar + "Config" + Path.DirectorySeparatorChar + name + ".json";
        }
    }

    public class Helper
    {
        //https://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static string ModsDirectory
        {
            get
            {
                return System.IO.Directory.GetParent(AssemblyDirectory).Parent.FullName;
            }
        }

        /// <summary>
        /// returns absolute file-path
        /// </summary>
        public static string CreatePath(string modName)
        {
            return ModsDirectory + Path.DirectorySeparatorChar + modName + ".json";
        }
    }

}
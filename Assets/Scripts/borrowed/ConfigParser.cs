using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using SysObject = System.Object;

public class ConfigParser
{
    public static ConfigParser instance;

    private static bool? _debugMode = null;
    public static bool debugMode
    {
        get
        {
            _debugMode ??= (bool)instance["Debug"];
            return _debugMode.Value;
        }
    }

    public FileInfo m_configFile { get; private set; }

    public struct ConfigItem
    {
        public SysObject value;
        public string str;
    
        public ConfigItem(SysObject value)
        {
            this.value = value;
            this.str = value.ToString();
        }
    }

    private readonly Dictionary<string, ConfigItem> parameters;

    public ConfigParser(Dictionary<string, ConfigItem> defConfig)
    {
        instance = this;
        
        m_configFile = new FileInfo(Directory.GetParent(Application.dataPath) + "/" + Utils.externalDataPath + "/conf.ini");

        LogRecorder debugWriter = LogRecorderCtrl.instance.writerDict["debug"];
        
        parameters = new Dictionary<string, ConfigItem>();

        string[] content = File.ReadAllLines(m_configFile.ToString());
        foreach (string line in content)
        {
            string[] pair = Regex.Replace(line, @"\s+", " ").Split('=');
            if (pair.Length != 2 || pair[0][0] == '#')
            {
                continue;
            }
            
            if (parameters.Keys.Contains(pair[0]))
            {
                debugWriter.Write(
                    $"ConfigParser: Entry \"{pair[0]}\" is a duplicate, currently stored value for this key is \"{parameters[pair[0]].str}\"");
            }
            else
            {
                string key = pair[0][1..];
                SysObject value;
                
                switch (pair[0][0])
                {
                    case 'b':
                        value = Convert.ChangeType(pair[1], typeof(bool));
                        break;
                    case 'i':
                        value = Convert.ChangeType(pair[1], typeof(int));
                        break;
                    case 'f':
                        value = Convert.ChangeType(pair[1], typeof(float));
                        break;
                    case 's':
                    default:
                        value = Convert.ChangeType(pair[1], typeof(string));
                        break;
                }
                parameters.Add(key, new ConfigItem( value ));
            }
        }

        // Set default value if missing from config file
        foreach (var configItem in defConfig)
        {
            string key = configItem.Key;
            ConfigItem val = configItem.Value;
            string source = "conf.ini";

            if (!parameters.Keys.Contains(key)){
                parameters.Add(configItem.Key, val);
                source = "default";
            }
            
            val = parameters[key];

            debugWriter.Write($"[CONFIG] {key} = {val.str} ({source})");
        }
    }

    public SysObject this[string key]
    {
        get
        {
            if (parameters == null || !parameters.Keys.Contains(key))
            {
                return null;
            }
            return parameters[key].value;
        }
    }

    public void Set<T>(string key, T value)
    {
        LogRecorder debugWriter = LogRecorderCtrl.instance.writerDict["debug"];
        debugWriter.Write($"[CONFIG] {key} = {value} (set.func)");

        ConfigItem configItem = new ConfigItem {value = value, str = value.ToString()};

        parameters[key] = configItem;
    }
}

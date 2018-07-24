﻿/**
 * 创 建 者：treertzhu
 * 创建日期：2018/7/24 16:11:49
**/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadWebsite
{
    class XConfig
    {
        public static string GetString(string key, string def = "")
        {
            return XLocalSave.singleton.GetString(key, def);
        }

        public static void SetString(string key, string val)
        {
            XLocalSave.singleton.SetString(key, val);
        }

        public static bool GetBool(string key, bool def = false)
        {
            var def_str = def.ToString();
            var str = XLocalSave.singleton.GetString(key, def_str);
            if(bool.TryParse(str, out def) == false)
            {
                def = false;
            }
            return def;
        }

        public static void SetBool(string key, bool val)
        {
            XLocalSave.singleton.SetString(key, val.ToString());
        }

        public static int GetInt(string key, int def = 0)
        {
            var def_str = def.ToString();
            var str = XLocalSave.singleton.GetString(key, def_str);
            if (int.TryParse(str, out def) == false)
            {
                def = 0;
            }
            return def;
        }

        public static void SetInt(string key, int val)
        {
            XLocalSave.singleton.SetString(key, val.ToString());
        }
    }

    public class XLocalSave
    {
        public static XLocalSave singleton = new XLocalSave();

        Dictionary<string, string> m_dic = new Dictionary<string, string>();
        string m_config_file = "";
        protected XLocalSave()
        {
            var dir = Application.LocalUserAppDataPath;
            Directory.CreateDirectory(dir);
            m_config_file = Path.Combine(dir, "download_website.key_val.config.txt");

            LoadConfig();
        }

        public string GetString(string key, string def = "")
        {
            if (m_dic.ContainsKey(key))
            {
                return m_dic[key];
            }
            else
            {
                return def;
            }
        }

        public void SetString(string key, string val)
        {
            m_dic[key] = val;
        }

        public void LoadConfig()
        {
            m_dic.Clear();
            if (File.Exists(m_config_file))
            {
                var content = File.ReadAllLines(m_config_file);
                for(var i =1; i < content.Length; i++)
                {
                    var line = content[i];
                    var cols = line.Trim().Split('\t');
                    if(cols.Length == 2)
                    {
                        m_dic[cols[0]] = cols[1];
                    }
                }
            }
        }

        public void SaveConfig()
        {
            List<string> list = new List<string>();
            list.Add("Key\tValue");
            foreach(var item in m_dic)
            {
                list.Add($"{item.Key}\t{item.Value}");
            }
            File.WriteAllLines(m_config_file, list.ToArray());
        }
    }
}

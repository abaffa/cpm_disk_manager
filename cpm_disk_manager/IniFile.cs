using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cpm_disk_manager
{
    public class IniFile
    {
        public string path;
        // string Path = System.Environment.CurrentDirectory+"\\"+"ConfigFile.ini";

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);
        public IniFile(string Path)
        {
            path = Path;
        }

        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp,
                                            255, this.path);
            return temp.ToString();

        }
    }

    /*
    //In mail Class
    public string ReadConfig(string section, string key)
    {
        string retVal = string.Empty;
        string bankname = string.Empty;
        string basePath = System.Environment.CurrentDirectory + "\\" + "Settings";
        IniFile ini = new IniFile(basePath + "\\" + "ConfigFile.ini");
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
            ini.IniWriteValue("DefaultNames", "default1", "2");
            ini.IniWriteValue("DefaultNames", "default2", "1");

        }
        retVal = ini.IniReadValue(section, key);
        return retVal;
    }
    */

}

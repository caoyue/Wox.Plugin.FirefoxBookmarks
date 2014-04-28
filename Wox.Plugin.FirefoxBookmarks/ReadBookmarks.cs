using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Wox.Plugin.FirefoxBookmarks
{
    public class ReadBookmarks
    {
        public List<Bookmark> GetBookmarks(string path)
        {
            var bookmarks = new List<Bookmark>();

            var dbPath = GetDbPath(path);
            SQLiteConnection conn = new SQLiteConnection(dbPath);
            conn.Open();
            var sql = @"SELECT moz_places.url, moz_bookmarks.title,parent.title as parent  
                           FROM moz_bookmarks    
                           JOIN moz_places ON moz_places.id = moz_bookmarks.fk 
                           JOIN moz_bookmarks as parent ON parent.id = moz_bookmarks.parent   
                           WHERE moz_bookmarks.type = 1";
            SQLiteCommand cmdQ = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = cmdQ.ExecuteReader();

            while (reader.Read())
            {
                var url = reader.IsDBNull(reader.GetOrdinal("url")) ? "" : reader.GetString(reader.GetOrdinal("url"));
                var title = reader.IsDBNull(reader.GetOrdinal("title")) ? url : reader.GetString(reader.GetOrdinal("title"));
                var folder = reader.IsDBNull(reader.GetOrdinal("parent")) ? url : reader.GetString(reader.GetOrdinal("parent"));
                if (url.StartsWith("http"))
                {
                    bookmarks.Add(new Bookmark
                    {
                        Title = title,
                        Url = url,
                        Folder = folder
                    });
                }
            }

            conn.Close();
            return bookmarks;
        }

        private string GetDbPath(string currentPath)
        {
            var profilePath = GetConfigPath(currentPath);
            if (string.IsNullOrEmpty(profilePath))
            {
                profilePath = GetFirefoxProfilePath();
            }
            return string.Format(@"Data Source ={0}", Path.Combine(profilePath,@"places.sqlite"));
        }

        private string GetConfigPath(string currentPath)
        {
            var xs = new XmlSerializer(typeof(PluginConfig));
            var config = xs.Deserialize(File.OpenRead(currentPath + @"\PluginConfig.xml")) as PluginConfig;
            return config.ProfilePath;
        }

        private string GetFirefoxProfilePath()
        {
            var firefoxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox");
            var profileIni = Path.Combine(firefoxPath, @"profiles.ini");

            if (File.Exists(profileIni))
            {
                var rdr = new StreamReader(profileIni);
                var resp = rdr.ReadToEnd();
                var lines = resp.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();

                var index = lines.IndexOf("Default=1");
                if (index > 3)
                {
                    var relative = lines[index - 2].Split('=')[1];
                    var profiePath = lines[index - 1].Split('=')[1];
                    return relative == "0" ? profiePath : Path.Combine(firefoxPath, profiePath);
                }
            }
            return "";
        }
    }

    [Serializable]
    public class PluginConfig
    {
        public string ProfilePath { get; set; }
    }
}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Wox.Plugin.FirefoxBookmarks
{
    public class Main : IPlugin
    {
        private static List<Bookmark> allBookmarks = new List<Bookmark>();
        private static string browser;
        private static string currentPath;

        public void Init(PluginInitContext context)
        {
            browser = GetDefaultBrowserPath();
            currentPath = context.CurrentPluginMetadata.PluginDirecotry;
            allBookmarks = new ReadBookmarks().GetBookmarks(currentPath);
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            var bookmarks = new List<Bookmark>();

            if (query.ActionParameters.Any()) {
                var keyword = query.ActionParameters[0].ToLower();
                bookmarks = allBookmarks.Where(b => b.Title.ToLower().Contains(keyword)
                                           || b.Url.ToLower().Contains(keyword)
                                           || b.Folder.ToLower().Contains(keyword)).ToList();

            }
            else {
                bookmarks = allBookmarks;
            }

            if (bookmarks != null && bookmarks.Any()) {
                results.AddRange(bookmarks.OrderBy(b => b.Folder).Select(b => new Result {
                    Title = b.Folder + " - " + b.Title,
                    SubTitle = b.Url,
                    IcoPath = System.IO.Path.Combine(currentPath, @"ico\bookmark.png"),
                    Action = (a) => {
                        Process.Start(browser, b.Url);
                        return true;
                    }
                }));
            }
            return results;
        }

        private static string GetDefaultBrowserPath()
        {
            var key = @"HTTP\shell\open\command";
            using (RegistryKey registrykey = Registry.ClassesRoot.OpenSubKey(key, false)) {
                if (registrykey != null) return ((string)registrykey.GetValue(null, null)).Split('"')[1];
            }
            return null;
        }
    }
}

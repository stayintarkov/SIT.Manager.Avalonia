using System;
using System.Collections.Generic;

namespace SIT.Manager.Avalonia.Models
{
    public class GiteaRelease
    {
        public List<Asset> assets { get; set; }
        public Author author { get; set; }
        public string body { get; set; }
        public DateTime created_at { get; set; }
        public bool draft { get; set; }
        public string html_url { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public bool prerelease { get; set; }
        public DateTime published_at { get; set; }
        public string tag_name { get; set; }
        public string tarball_url { get; set; }
        public string target_commitish { get; set; }
        public string url { get; set; }
        public string zipball_url { get; set; }

        public class Asset
        {
            public string browser_download_url { get; set; }
            public DateTime created_at { get; set; }
            public int download_count { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public int size { get; set; }
            public string uuid { get; set; }
        }

        public class Author
        {
            public bool active { get; set; }
            public string avatar_url { get; set; }
            public DateTime created { get; set; }
            public string description { get; set; }
            public string email { get; set; }
            public int followers_count { get; set; }
            public int following_count { get; set; }
            public string full_name { get; set; }
            public int id { get; set; }
            public bool is_admin { get; set; }
            public string language { get; set; }
            public DateTime last_login { get; set; }
            public string location { get; set; }
            public string login { get; set; }
            public string login_name { get; set; }
            public bool prohibit_login { get; set; }
            public bool restricted { get; set; }
            public int starred_repos_count { get; set; }
            public string visibility { get; set; }
            public string website { get; set; }
        }
    }
}

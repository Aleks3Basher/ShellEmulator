using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace ShellEmulator.model
{
    internal class Vfs
    {
        public string Name { get; private set; }
        public string Sha256Hex { get; private set; }
        public VfsNode Root { get; private set; } = new VfsNode("/", true);

        public Vfs(string name, byte[] rawData)
        {
            Name = name;
            Sha256Hex = ComputeSha256(rawData);
            ParseCsv(Encoding.UTF8.GetString(rawData));
        }

        private static string ComputeSha256(byte[] data)
        {
            var hash = SHA256.HashData(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private void ParseCsv(string csvText)
        {
            var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int start = 0;
            if (lines.Length > 0)
            {
                var first = lines[0].Trim().ToLowerInvariant();
                if (first.StartsWith("path,") || first.Contains("type") && first.Contains("content"))
                {
                    start = 1;
                }
            }

            for (int i = start; i < lines.Length; i++)
            {
                var line = lines[i];
                var parts = SplitCsvLine(line);
                if (parts.Length < 2) continue;
                var path = parts[0].Trim().Trim('"');
                var type = parts[1].Trim().Trim('"').ToLowerInvariant();
                var content = parts.Length >= 3 ? parts[2].Trim().Trim('"') : string.Empty;

                if (string.IsNullOrEmpty(path)) continue;
                path = path.TrimStart('/');
                if (type == "dir")
                {
                    EnsureDirectory(path);
                }
                else
                {
                    byte[] bytes = new byte[0];
                    if (!string.IsNullOrEmpty(content))
                    {
                        try { bytes = Convert.FromBase64String(content); }
                        catch { bytes = Encoding.UTF8.GetBytes(content); }
                    }
                    AddFile(path, bytes);
                }
            }
        }

        private static string[] SplitCsvLine(string line)
        {
            var matches = Regex.Matches(line, "(?<=^|,)(\"(?<q>(?:[^\"]|\"\")*)\"|[^,]*)");
            var list = new List<string>();
            foreach (Match m in matches)
            {
                var v = m.Value;
                if (v.StartsWith(",")) v = v.Substring(1);
                v = v.Trim();
                if (v.StartsWith("\"") && v.EndsWith("\""))
                {
                    v = v.Substring(1, v.Length - 2).Replace("\"\"", "\"");
                }
                list.Add(v);
            }
            if (list.Count == 0) return new string[0];
            return list.ToArray();
        }

        private void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var cur = Root;
            foreach (var p in parts)
            {
                var child = cur.Children.FirstOrDefault(x => x.Name == p && x.IsDirectory);
                if (child == null)
                {
                    child = new VfsNode(p, true);
                    cur.Children.Add(child);
                }
                cur = child;
            }
        }

        private void AddFile(string path, byte[] content)
        {
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;
            var cur = Root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var p = parts[i];
                var child = cur.Children.FirstOrDefault(x => x.Name == p && x.IsDirectory);
                if (child == null)
                {
                    child = new VfsNode(p, true);
                    cur.Children.Add(child);
                }
                cur = child;
            }
            var filename = parts.Last();
            var existing = cur.Children.FirstOrDefault(x => x.Name == filename);
            if (existing != null)
            {
                existing.IsDirectory = false;
                existing.Content = content;
            }
            else
            {
                var fileNode = new VfsNode(filename, false) { Content = content };
                cur.Children.Add(fileNode);
            }
        }

        public VfsNode? Resolve(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/") return Root;
            var norm = path.TrimStart('/');
            var parts = norm.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var cur = Root;
            foreach (var p in parts)
            {
                var child = cur.Children.FirstOrDefault(x => x.Name == p);
                if (child == null) return null;
                cur = child;
            }
            return cur;
        }
    }
}

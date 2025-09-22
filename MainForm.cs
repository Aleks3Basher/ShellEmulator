using ShellEmulator.model;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ShellEmulator
{
    public partial class MainForm : Form
    {
        private TextBox outputBox;
        private TextBox inputBox;
        private Button sendButton;

        private readonly string? vfsPath;
        private readonly string? scriptPath;

        private Vfs? currentVfs = null;
        private VfsNode currentNode = null!;
        private string currentVfsPath = "/";

        public MainForm(string? vfsPath, string? scriptPath)
        {
            this.vfsPath = vfsPath;
            this.scriptPath = scriptPath;
            InitializeComponent();
            InitUI();
        }

        private void InitUI()
        {
            var user = Environment.UserName;
            string host;
            try { host = Dns.GetHostName(); }
            catch { host = "host"; }
            this.Text = "Эмулятор - [" + user + "@" + host + "]";

            this.Width = 800;
            this.Height = 600;

            outputBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Top,
                Height = this.ClientSize.Height - 40,
                Width = this.ClientSize.Width - 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            inputBox = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                Width = this.ClientSize.Width - 100,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            sendButton = new Button
            {
                Text = "Enter",
                Width = 80,
                Height = 24,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            sendButton.Left = this.ClientSize.Width - sendButton.Width - 8;
            sendButton.Top = this.ClientSize.Height - sendButton.Height - 8;
            sendButton.Click += OnButtonClick;

            this.Resize += (s, e) => { OnResizeControl(); };
            inputBox.KeyDown += OnInputKeyDown;

            this.Controls.Add(outputBox);
            this.Controls.Add(sendButton);
            this.Controls.Add(inputBox);

            PrintWelcome();
            PrintConfig();

            if (!string.IsNullOrEmpty(vfsPath))
            {
                TryLoadVfs(vfsPath);
            }

            RenderPromt();

            inputBox.Focus();
            OnResizeControl();

            if (!string.IsNullOrEmpty(scriptPath))
            {
                RunStartupScript(scriptPath);
            }
        }

        private void TryLoadVfs(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    AppendOutput("Ошибка: VFS файл не найден: " + path);
                    return;
                }
                var raw = File.ReadAllBytes(path);
                var name = Path.GetFileName(path) ?? path;
                currentVfs = new Vfs(name, raw);
                currentNode = currentVfs.Root;
                currentVfsPath = "/";
                AppendOutput("VFS загружен: " + name);
                AppendOutput("SHA-256: " + currentVfs.Sha256Hex);
            }
            catch (Exception ex)
            {
                AppendOutput("Ошибка загрузки VFS: " + ex.Message);
            }
        }

        private void OnResizeControl()
        {
            outputBox.Height = this.ClientSize.Height - inputBox.Height - 16;
            inputBox.Top = this.ClientSize.Height - inputBox.Height - 8;
            sendButton.Left = this.ClientSize.Width - sendButton.Width - 8;
            sendButton.Top = this.ClientSize.Height - sendButton.Height - 8;
        }

        private void PrintWelcome()
        {
            AppendOutput("Эмулятор запущен");
        }

        private void PrintConfig()
        {
            AppendOutput("Конфигурация:");
            AppendOutput("  VFS Path: " + (vfsPath ?? "<не задан>"));
            AppendOutput("  Script Path: " + (scriptPath ?? "<не задан>"));
        }

        private void AppendOutput(string str)
        {
            if (outputBox.Text.Length == 0) outputBox.Text = str;
            else outputBox.AppendText(Environment.NewLine + str);

            outputBox.SelectionStart = outputBox.Text.Length;
            outputBox.ScrollToCaret();
        }

        private void RenderPromt()
        {
            var user = Environment.UserName;
            string host;
            try { host = Dns.GetHostName(); }
            catch { host = "host"; }
            AppendOutput(user + "@" + host + ":$ " + (currentVfs != null ? "[vfs:" + currentVfs.Name + currentVfsPath + "]" : ""));
        }

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ProcessInput(inputBox.Text);
                inputBox.Clear();
            }
        }

        private void OnButtonClick(object? sender, EventArgs e)
        {
            ProcessInput(inputBox.Text);
            inputBox.Clear();
        }

        private void ProcessInput(string line)
        {
            if (line == null) return;
            line = line.Trim();

            if (line.Length == 0)
            {
                RenderPromt();
                return;
            }
            AppendOutput("> " + line);

            var matches = Regex.Matches(line, @"[\""].+?[\""]|[^ ]+");
            var parts = matches
                .Select(m => m.Value.Trim('"'))
                .ToArray();
            if (parts.Length == 0)
            {
                RenderPromt();
                return;
            }

            var cmd = parts[0];
            var args = parts.Skip(1).ToArray();
            try
            {
                switch (cmd)
                {
                    case "exit":
                        AppendOutput("Выход...");
                        this.Close();
                        return;
                    case "ls":
                        CmdLs(args);
                        break;
                    case "cd":
                        CmdCd(args);
                        break;
                    case "vfs-info":
                        CmdVfsInfo(args);
                        break;
                    case "vfs-load":
                        CmdVfsLoad(args);
                        break;
                    case "vfs-cat":
                        CmdVfsCat(args);
                        break;
                    default:
                        AppendOutput("Ошибка: неизвестная команда '" + cmd + "'");
                        break;
                }
            }
            catch (Exception ex)
            {
                AppendOutput("Ошибка выполнения: " + ex.Message);
            }
            finally
            {
                RenderPromt();
            }
        }

        private void CmdVfsLoad(string[] args)
        {
            if (args.Length == 0)
            {
                AppendOutput("Использование: vfs-load <путь к csv>");
                return;
            }
            TryLoadVfs(args[0]);
        }

        private void CmdVfsInfo(string[] args)
        {
            if (currentVfs == null)
            {
                AppendOutput("VFS не загружен");
                return;
            }
            AppendOutput("VFS: " + currentVfs.Name);
            AppendOutput("SHA-256: " + currentVfs.Sha256Hex);
        }

        private void CmdLs(string[] args)
        {
            if (currentVfs == null)
            {
                CmdStub("ls", args);
                return;
            }

            var targetPath = args.Length > 0 ? args[0] : currentVfsPath;
            var node = ResolveVfsPath(targetPath);
            if (node == null)
            {
                AppendOutput("ls: путь не найден: " + targetPath);
                return;
            }
            if (!node.IsDirectory)
            {
                AppendOutput(targetPath + "\t<file>");
                return;
            }
            foreach (var child in node.Children.OrderBy(c => c.IsDirectory ? 0 : 1).ThenBy(c => c.Name))
            {
                AppendOutput((child.IsDirectory ? "d" : "-") + "\t" + child.Name);
            }
        }

        private VfsNode? ResolveVfsPath(string path)
        {
            if (path == ".") return currentNode;
            if (path == "/" || string.IsNullOrEmpty(path)) return currentVfs?.Root;
            if (path.StartsWith("/")) return currentVfs?.Resolve(path);
            var rel = currentVfsPath.TrimEnd('/');
            if (!rel.EndsWith("/")) rel += "/";
            var full = (rel + path).Replace("//", "/");
            return currentVfs?.Resolve(full);
        }

        private void CmdCd(string[] args)
        {
            if (currentVfs == null)
            {
                CmdStub("cd", args);
                return;
            }
            if (args.Length == 0)
            {
                currentNode = currentVfs.Root;
                currentVfsPath = "/";
                return;
            }
            var target = args[0];
            VfsNode? node;
            if (target.StartsWith("/")) node = currentVfs.Resolve(target);
            else node = ResolveVfsPath(target);

            if (node == null)
            {
                AppendOutput("cd: путь не найден: " + target);
                return;
            }
            if (!node.IsDirectory)
            {
                AppendOutput("cd: не директория: " + target);
                return;
            }
            var stack = new Stack<string>();
            var cur = node;
            while (cur != null && cur != currentVfs.Root)
            {
                stack.Push(cur.Name);
                cur = FindParent(currentVfs.Root, cur);
            }
            var sb = new StringBuilder("/");
            while (stack.Count > 0)
            {
                sb.Append(stack.Pop());
                if (stack.Count > 0) sb.Append('/');
                else sb.Append('/');
            }
            currentVfsPath = sb.ToString();
            currentNode = node;
        }

        private VfsNode? FindParent(VfsNode root, VfsNode target)
        {
            foreach (var child in root.Children)
            {
                if (child == target) return root;
                if (child.IsDirectory)
                {
                    var p = FindParent(child, target);
                    if (p != null) return p;
                }
            }
            return null;
        }

        private void CmdVfsCat(string[] args)
        {
            if (currentVfs == null)
            {
                AppendOutput("VFS не загружен");
                return;
            }
            if (args.Length == 0)
            {
                AppendOutput("Использование: vfs-cat <путь>");
                return;
            }
            var node = ResolveVfsPath(args[0]);
            if (node == null)
            {
                AppendOutput("vfs-cat: путь не найден: " + args[0]);
                return;
            }
            if (node.IsDirectory)
            {
                AppendOutput("vfs-cat: путь — директория: " + args[0]);
                return;
            }
            var bytes = node.Content ?? new byte[0];
            if (bytes.Length == 0)
            {
                AppendOutput("(пустой файл)");
                return;
            }
            try
            {
                var text = Encoding.UTF8.GetString(bytes);
                if (Regex.IsMatch(text, "[\x00-\x08\x0B\x0C\x0E-\x1F]"))
                {
                    AppendOutput(Convert.ToBase64String(bytes));
                }
                else
                {
                    AppendOutput(text);
                }
            }
            catch
            {
                AppendOutput(Convert.ToBase64String(bytes));
            }
        }

        private void CmdStub(string name, string[] args)
        {
            var argLine = args.Length == 0 ? "" : string.Join(" ", args);
            AppendOutput("[" + name + "] args: " + argLine);
        }

        private void RunStartupScript(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    AppendOutput("Ошибка: файл скрипта не найден: " + path);
                    return;
                }

                var lines = File.ReadAllLines(path);
                AppendOutput("Выполнение стартового скрипта: " + path);

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();

                    if (line.StartsWith('#') || line.StartsWith("//") || line.Length == 0)
                        continue;

                    ProcessInput(line);
                }
            }
            catch (Exception ex)
            {
                AppendOutput("Ошибка выполнения скрипта: " + ex.Message);
            }
        }
    }
}

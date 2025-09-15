
using System.Net;

namespace ShellEmulator
{
    public partial class MainForm : Form
    {

        private TextBox outputBox;
        private TextBox inputBox;
        private Button sendButton;

        public MainForm()
        {
            InitializeComponent();
            InitUI();
        }

        private void InitUI()
        {
            var user = Environment.UserName;
            string host;
            try { host = Dns.GetHostName(); }
            catch { host = "host"; }
            this.Text = "Эмулятор - [" + user + "]@" + host + "]";

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

            this.Resize += (s, e) =>
            {
                OnResizeControl();
            };

            inputBox.KeyDown += OnInputKeyDown;

            this.Controls.Add(outputBox);
            this.Controls.Add(sendButton);
            this.Controls.Add(inputBox);

            PrintWelcome();
            RenderPromt();
            inputBox.Focus();
            OnResizeControl();
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
            AppendOutput("Эмулятор");
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
            AppendOutput(user + "@" + host + ":$ ");
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ProcessInput(inputBox.Text);
                inputBox.Clear();
            }
        }

        private void OnButtonClick(object sender, EventArgs e)
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

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        CmdStub("ls", args);
                        break;
                    case "cd":
                        CmdStub("cd", args);
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

        private void CmdStub(string name, string[] args)
        {
            var argLine = args.Length == 0 ? "" : string.Join(" ", args);
            AppendOutput("[" + name + "] args: " + argLine);
        }
    }
}

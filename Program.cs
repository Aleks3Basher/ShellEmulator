namespace ShellEmulator
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string? vfsPath = null;
            string? scriptPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--vfs" && i + 1 < args.Length)
                {
                    vfsPath = args[i + 1];
                    i++;
                }
                else if (args[i] == "--script" && i + 1 < args.Length)
                {
                    scriptPath = args[i + 1];
                    i++;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(vfsPath, scriptPath));
        }
    }
}

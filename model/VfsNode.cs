
namespace ShellEmulator.model
{
    internal class VfsNode
    {
        public string Name { get; set; }
        public bool IsDirectory { get; set; }
        public byte[] Content { get; set; } = new byte[0];
        public List<VfsNode> Children { get; set; } = new List<VfsNode>();

        public VfsNode(string name, bool isDirectory)
        {
            Name = name;
            IsDirectory = isDirectory;
        }
    }
}

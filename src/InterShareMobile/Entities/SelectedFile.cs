using System;
using System.IO;

namespace InterShareMobile.Entities
{
    public class SelectedFile
    {
        private Func<Stream> _getStreamCallback;

        public string Name { get; set; }
        public string Path { get; set; }

        // public SelectedFile(Func<Stream> getStreamCallback, string name = "", string path = "")
        // {
        //     _getStreamCallback = getStreamCallback;
        //     Name = name;
        //     Path = path;
        // }
    }
}
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriveExtractor
{
    public class Node
    {

        public enum NodeType
        {
            FOLDER,
            FILE
        }

        public enum NodeStatus
        {
            QUEUED,
            COPYING,
            ERROR,
            COMPLETED,
            CANCELLED,
            SKIPPED
        }

        private FileSystemInfo _info;
        private string _name;
        private NodeStatus _status;
        private NodeType _nodeType;
        private long size;
        private string _friendySize;
        private string _pathSrc;
        private string _pathDst;
        private int _listViewItemIndex;
        private bool _isCopy;

        public FileSystemInfo Info { get => _info; set => _info = value; }
        public string Name { get => _name; set => _name = value; }
        public NodeStatus Status { get => _status; set => _status = value; }
        public NodeType Type { get => _nodeType; set => _nodeType = value; }
        public long Size { get => size; set => size = value; }
        public string FriendySize { get => _friendySize; set => _friendySize = value; }
        public string PathSrc { get => _pathSrc; set => _pathSrc = value; }
        public string PathDst { get => _pathDst; set => _pathDst = value; }
        public int ListViewItemIndex { get => _listViewItemIndex; set => _listViewItemIndex = value; }
        public bool IsCopy { get => _isCopy; set => _isCopy = value; }

        public Node(string absPath)
        {

            if (Directory.Exists(absPath))
            {
                this.Info = new DirectoryInfo(absPath);
                this.Type = NodeType.FOLDER;
                this.Size = GetDirectorySize(absPath);
            } else if (File.Exists(absPath))
            {
                this.Info = new FileInfo(absPath);
                this.Type = NodeType.FILE;
                this.Size = (this.Info as FileInfo).Length;

            } else
            {
                throw new FileNotFoundException();
            }

            this.Name = Info.Name;
            this.PathSrc = Info.FullName;
            this.FriendySize = FileSizeFormatter.FormatSize(this.Size);
            this.Status = NodeStatus.QUEUED;
            this.IsCopy = true;
        }

        public bool IsValid()
        {

            //todo later

            //if (Type == NodeType.FOLDER)
            //{
            //    return false;
            //}

            return true;

        }

        public void CopyOperation()
        {
            try
            {
                if (Type == NodeType.FOLDER)
                {
                    string dirName = Path.GetDirectoryName(PathDst);
                    if (!Directory.Exists(dirName))
                        Directory.CreateDirectory(dirName);

                    this.Status = NodeStatus.COPYING;
                    FileSystem.CopyDirectory(PathSrc, PathDst, UIOption.AllDialogs);
                }
                else if (Type == NodeType.FILE)
                {
                    this.Status = NodeStatus.COPYING;
                    FileSystem.CopyFile(PathSrc, PathDst, UIOption.AllDialogs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                this.Status = NodeStatus.ERROR;
            }
            this.Status = NodeStatus.COMPLETED;
        }

        private static long GetDirectorySize(string p)
        {
            // Get array of all file names.
            string[] a = Directory.GetFiles(p, "*.*");

            // Calculate total bytes of all files in a loop.
            long b = 0;
            foreach (string name in a)
            {
                // Use FileInfo to get length of each file.
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            // Return total size
            return b;
        }
    }

    public static class FileSizeFormatter
    {
        // Load all suffixes in an array  
        static readonly string[] suffixes =
        { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public static string FormatSize(long bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }
    }


}

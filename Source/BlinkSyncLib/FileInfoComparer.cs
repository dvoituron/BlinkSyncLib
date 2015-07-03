using System;
using System.Collections.Generic;
using System.IO;

namespace BlinkSyncLib
{
    public class FileInfoComparer : IComparer<FileInfo>
    {
        public int Compare(FileInfo x, FileInfo y)
        {
            // sort x and y ascending by name
            return x.Name.CompareTo(y.Name);
        }
    }
}

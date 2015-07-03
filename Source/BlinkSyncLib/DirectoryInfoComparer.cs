using System;
using System.Collections.Generic;
using System.IO;

namespace BlinkSyncLib
{

    public class DirectoryInfoComparer : IComparer<DirectoryInfo>
    {
        public int Compare(DirectoryInfo x, DirectoryInfo y)
        {
            // sort x and y ascending by name
            return x.Name.CompareTo(y.Name);
        }
    }
}

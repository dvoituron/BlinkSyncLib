using System;

namespace BlinkSyncLib
{
    public class SyncResults
    {
        public int FilesCopied { get; set; }
        public int FilesUpToDate { get; set; }
        public int FilesDeleted { get; set; }
        public int FilesIgnored { get; set; }
        public int DirectoriesCreated { get; set; }
        public int DirectoriesDeleted { get; set; }
        public int DirectoriesIgnored { get; set; }
    }
}

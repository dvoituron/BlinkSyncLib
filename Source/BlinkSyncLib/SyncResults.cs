using System;

namespace BlinkSyncLib
{
    public class SyncResults
    {
        /// <summary>
        /// Get or set the number of files copied.
        /// </summary>
        public int FilesCopied { get; set; }

        /// <summary>
        /// Get or set the number of files already up to date.
        /// </summary>
        public int FilesUpToDate { get; set; }

        /// <summary>
        /// Get or set the number of files deleted.
        /// </summary>
        public int FilesDeleted { get; set; }

        /// <summary>
        /// Get or set the number of files not synchronized.
        /// </summary>
        public int FilesIgnored { get; set; }

        /// <summary>
        /// Get or set the number of new folders created.
        /// </summary>
        public int DirectoriesCreated { get; set; }

        /// <summary>
        /// Get or set the number of folders removed.
        /// </summary>
        public int DirectoriesDeleted { get; set; }

        /// <summary>
        /// Get or set the number of folder not synchronized and ignored.
        /// </summary>
        public int DirectoriesIgnored { get; set; }
    }
}

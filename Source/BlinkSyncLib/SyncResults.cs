using System;

namespace BlinkSyncLib
{
    public class SyncResults
    {
        /// <summary>
        /// Get the number of files copied.
        /// </summary>
        public int FilesCopied { get; internal set; }

        /// <summary>
        /// Get the number of files already up to date.
        /// </summary>
        public int FilesUpToDate { get; internal set; }

        /// <summary>
        /// Get the number of files deleted.
        /// </summary>
        public int FilesDeleted { get; internal set; }

        /// <summary>
        /// Get the number of files not synchronized.
        /// </summary>
        public int FilesIgnored { get; internal set; }

        /// <summary>
        /// Get the number of new folders created.
        /// </summary>
        public int DirectoriesCreated { get; internal set; }

        /// <summary>
        /// Get the number of folders removed.
        /// </summary>
        public int DirectoriesDeleted { get; internal set; }

        /// <summary>
        /// Get the number of folder not synchronized and ignored.
        /// </summary>
        public int DirectoriesIgnored { get; internal set; }
    }
}

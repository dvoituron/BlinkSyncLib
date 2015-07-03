using System;
using System.Text.RegularExpressions;

namespace BlinkSyncLib
{
    public class InputParams
    {
        /// <summary>
        /// Are we in quiet mode
        /// </summary>
        public bool IsQuiet { get; set; }

        /// <summary>
        /// Should exclude hidden files/directories in source
        /// </summary>
        public bool ExcludeHidden { get; set; }

        /// <summary>
        /// Should delete files/directories from dest than are not present in source
        /// </summary>
        public bool DeleteFromDest { get; set; }

        /// <summary>
        /// List of filespecs to exclude
        /// </summary>
        public Regex[] ExcludeFiles { get; set; }

        /// <summary>
        /// List of directory specs to exclude
        /// </summary>
        public Regex[] ExcludeDirs { get; set; }

        /// <summary>
        /// List of filespecs to include 
        /// </summary>
        public Regex[] IncludeFiles { get; set; }

        /// <summary>
        /// List of directory specs to include
        /// </summary>
        public Regex[] IncludeDirs { get; set; }

        /// <summary>
        /// List of filespecs NOT to delete from dest
        /// </summary>
        public Regex[] DeleteExcludeFiles { get; set; }

        /// <summary>
        /// List of directory specs NOT to delete from dest
        /// </summary>
        public Regex[] DeleteExcludeDirs { get; set; }

        public bool AreSourceFilesFiltered
        {
            get
            {
                return ExcludeHidden || (IncludeFiles != null) || (ExcludeFiles != null) ||
                    (IncludeDirs != null) || (ExcludeDirs != null);
            }
        }
    }
}

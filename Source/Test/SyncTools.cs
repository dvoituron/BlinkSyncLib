using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BlinkSyncLib;

namespace BlinkSyncTests
{
    public static class SyncTools
    {
        /// <summary>
        /// Converts array of filespec strings to array of equivalent regexes
        /// </summary>
        public static Regex[] FileSpecsToRegex(string[] fileSpecs)
        {
            List<Regex> regexList = new List<Regex>();
            foreach (string fileSpec in fileSpecs)
            {
                regexList.Add(FileSpecToRegex(fileSpec));
            }
            return regexList.ToArray();
        }

        /// <summary>
        /// Parses list of comma-separated filespecs from specified argument and returns list of regex equivalents as out parameter
        /// </summary>
        /// <param name="args"></param>
        /// <param name="iArg"></param>
        /// <param name="matches"></param>
        private static bool ParseFilespecs(string[] args, int iArg, out Regex[] matches)
        {
            matches = null;
            if (iArg >= args.Length)
                return false;

            List<Regex> regexList = new List<Regex>();
            string filespecStr = args[iArg];
            int pos = 0, end;
            while (pos < filespecStr.Length)
            {
                if (filespecStr[pos] == '"')
                {
                    if (pos + 1 >= filespecStr.Length)
                        return false;
                    end = filespecStr.IndexOf('"', pos + 1);
                    if (end == -1)
                        return false;
                }
                else
                {
                    end = filespecStr.IndexOf(',', pos + 1);
                    if (end == -1)
                        end = filespecStr.Length;
                }
                string filespec = filespecStr.Substring(pos, end - pos);
                regexList.Add(FileSpecToRegex(filespec));
                pos = end;
                if ((pos < filespecStr.Length) && (filespecStr[pos] != ','))
                    return false;
                pos++; // skip the next comma
            }

            matches = regexList.ToArray();
            return true;
        }

        /// <summary>
        /// Converts specified filespec string to equivalent regex
        /// </summary>
        /// <param name="fileSpec"></param>
        private static Regex FileSpecToRegex(string fileSpec)
        {
            string pattern = fileSpec.Trim();
            pattern = pattern.Replace(".", @"\.");
            pattern = pattern.Replace("*", @".*");
            pattern = pattern.Replace("?", @".?");
            return new Regex("^" + pattern + "$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Determines if two byte arrays compare exactly
        /// </summary>
        public static bool CompareByteArrays(byte[] x, byte[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                    return false;
            }

            return true;
        }

        public static bool CompareTo(SyncResults first, SyncResults other)
        {
            if (first.FilesCopied != other.FilesCopied)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files copied, got {1}", first.FilesCopied, other.FilesCopied);
                return false;
            }
            if (first.FilesUpToDate != other.FilesUpToDate)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files up to date, got {1}", first.FilesUpToDate, other.FilesUpToDate);
                return false;
            }
            if (first.FilesDeleted != other.FilesDeleted)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files deleted, got {1}", first.FilesDeleted, other.FilesDeleted);
                return false;
            }
            if (first.FilesIgnored != other.FilesIgnored)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files ignored, got {1}", first.FilesIgnored, other.FilesIgnored);
                return false;
            }
            if (first.DirectoriesCreated != other.DirectoriesCreated)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} directories created, got {1}", first.DirectoriesCreated, other.DirectoriesCreated);
                return false;
            }
            if (first.DirectoriesDeleted != other.DirectoriesDeleted)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} directories deleted, got {1}", first.DirectoriesDeleted, other.DirectoriesDeleted);
                return false;
            }
            if (first.DirectoriesIgnored != other.DirectoriesIgnored)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} directories ignored, got {1}", first.DirectoriesIgnored, other.DirectoriesIgnored);
                return false;
            }
            return true;
        }

        public static void Set(this SyncResults syncResult,
                               int theFilesCopied, int theFilesUpToDate, int theFilesDeleted, int theFilesIgnored,
                               int theDirectoriesCreated, int theDirectoriesDeleted, int theDirectoriesIgnored)
        {
            syncResult.FilesCopied = theFilesCopied;
            syncResult.FilesUpToDate = theFilesUpToDate;
            syncResult.FilesDeleted = theFilesDeleted;
            syncResult.FilesIgnored = theFilesIgnored;
            syncResult.DirectoriesCreated = theDirectoriesCreated;
            syncResult.DirectoriesDeleted = theDirectoriesDeleted;
            syncResult.DirectoriesIgnored = theDirectoriesIgnored;
        }

    }

    public class FileInfoComparer : IComparer<FileInfo>
    {
        public int Compare(FileInfo x, FileInfo y)
        {
            // sort x and y ascending by name
            return x.Name.CompareTo(y.Name);
        }
    }

    public class DirectoryInfoComparer : IComparer<DirectoryInfo>
    {
        public int Compare(DirectoryInfo x, DirectoryInfo y)
        {
            // sort x and y ascending by name
            return x.Name.CompareTo(y.Name);
        }
    }
}

//
// BLINKSYNC.CS 
//
// Copyright (c) 2008 BlinkSync development team (Jeremy Stone et al)
// 
// This file is part of BlinkSync.
//
// BlinkSync is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// BlinkSync is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with BlinkSync.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BlinkSync
{

    public class App
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("BlinkSync v1.02 (C) 2008 BlinkSync contributors. http://blinksync.sourceforge.net");
            Console.WriteLine("Distributed under GNU General Public License. http://www.gnu.org/licenses");
            // make sure we have min required arguments
            if (args.Length < 2)
            {
                PrintUsage();
                Environment.Exit(2);
            }

            InputParams inputParams = new InputParams();

            // parse flags
            int i = 0;
            for (i = 0; i < args.Length - 2; i++)
            {
                if (string.Compare(args[i], "-q", true) == 0)
                {
                    inputParams.isQuiet = true;
                }
                else if (string.Compare(args[i], "-d", true) == 0)
                {
                    inputParams.deleteFromDest = true;
                }
                else if (string.Compare(args[i], "-xh", true) == 0)
                {
                    inputParams.excludeHidden = true;
                }
                else if (string.Compare(args[i], "-xf", true) == 0)
                {
                    // get exclude file regex list
                    if (!ParseFilespecs(args, i + 1, out inputParams.excludeFiles))
                    {
                        PrintInvalidCommandLine();
                        Environment.Exit(2);
                    }
                    i++;
                }
                else if (string.Compare(args[i], "-xd", true) == 0)
                {
                    // get exclude dir regex list
                    if (!ParseFilespecs(args, i + 1, out inputParams.excludeDirs))
                    {
                        PrintInvalidCommandLine();
                        Environment.Exit(2);
                    }
                    i++;
                }
                else if (string.Compare(args[i], "-if", true) == 0)
                {
                    // get include file regex list
                    if (!ParseFilespecs(args, i + 1, out inputParams.includeFiles))
                    {
                        PrintInvalidCommandLine();
                        Environment.Exit(2);
                    }
                    i++;
                }
                else if (string.Compare(args[i], "-id", true) == 0)
                {
                    // get include dir regex list
                    if (!ParseFilespecs(args, i + 1, out inputParams.includeDirs))
                    {
                        PrintInvalidCommandLine();
                        Environment.Exit(2);
                    }
                    i++;
                }
                else if (string.Compare(args[i], "-ndf", true) == 0)
                {
                    // get no-delete file regex list
                    if (!ParseFilespecs(args, i + 1, out inputParams.deleteExcludeFiles))
                    {
                        PrintInvalidCommandLine();
                        Environment.Exit(2);
                    }
                    i++;
                }
                else if (string.Compare(args[i], "-ndd", true) == 0)
                {
                    // get no-delete dir regex list
                    if (!ParseFilespecs(args, i + 1, out inputParams.deleteExcludeDirs))
                    {
                        PrintInvalidCommandLine();
                        Environment.Exit(2);
                    }
                    i++;
                }
                else
                {
                    Console.Error.WriteLine("Invalid command line parameter: {0}", args[i]);
                    PrintUsage();
                    Environment.Exit(2);
                }
            }

            if (((inputParams.includeFiles != null) && (inputParams.excludeFiles != null)) ||
                ((inputParams.includeDirs != null) && (inputParams.excludeDirs != null)))
            {
                PrintUsage();
                Environment.Exit(2);
            }

            string srcDir = args[i++];
            string destDir = args[i];

            string fullSrcDir = Path.GetFullPath(srcDir);
            string fullDestDir = Path.GetFullPath(destDir);
            if (destDir.StartsWith(fullSrcDir) || srcDir.StartsWith(fullDestDir))
            {
                Console.Error.WriteLine("Error: source directory {0} and destination directory {1} cannot contain each other", fullSrcDir, fullDestDir);
                Environment.Exit(2);
            }

            if (((inputParams.deleteExcludeFiles != null) || (inputParams.deleteExcludeDirs != null)) &&
                (!inputParams.deleteFromDest))
            {
                Console.Error.WriteLine("Error: exclude-from-deletion options (-ndf and -ndd) require deletion (-d) enabled.");
                PrintUsage();
                Environment.Exit(2);
            }

            // ensure source directory exists
            if (!Directory.Exists(srcDir))
            {
                Console.Error.WriteLine("Error: source directory {0} not found", srcDir);
                Environment.Exit(2);
            }

            Results results = new Results();

            // perform sync
            bool success = Sync(srcDir, destDir, inputParams, ref results);

            if (success)
            {
                Console.WriteLine("Sync completed. {0} file(s) copied, {1} file(s) up to date, {2} file(s) deleted, {3} directories deleted",
                    results.filesCopied, results.filesUpToDate, results.filesDeleted, results.directoriesDeleted);
            }
            else
            {
                Console.Error.WriteLine("Sync failed.");
            }
        }

        /// <summary>
        /// Performs one-way synchronization from source directory tree to destination directory tree
        /// </summary>
        public static bool Sync(string srcDir, string destDir, InputParams inputParams, ref Results results)
        {
            // recursively process directories
            return ProcessDirectory(srcDir, destDir, inputParams, ref results);
        }

        /// <summary>
        /// Recursively performs one-way synchronization from a single source to destination directory
        /// </summary>
        private static bool ProcessDirectory(string srcDir, string destDir, InputParams inputParams, ref Results results)
        {
            DirectoryInfo diSrc = new DirectoryInfo(srcDir);
            DirectoryInfo diDest = new DirectoryInfo(destDir);

            // create destination directory if it doesn't exist
            if (!diDest.Exists)
            {
                try
                {
                    if (!inputParams.isQuiet)
                    {
                        Console.WriteLine("Creating directory: {0}", diDest.FullName);
                    }
                    // create the destination directory
                    diDest.Create();
                    results.directoriesCreated++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error: failed to create directory {0}. {1}", destDir, ex.Message);
                    return false;
                }
            }

            // get list of selected files from source directory
            FileInfo[] fiSrc = GetFiles(diSrc, inputParams, ref results);
            // get list of files in destination directory
            FileInfo[] fiDest = GetFiles(diDest, null, ref results);

            // put the source files and destination files into hash tables                     
            Hashtable hashSrc = new Hashtable(fiSrc.Length);
            foreach (FileInfo srcFile in fiSrc)
            {
                hashSrc.Add(srcFile.Name, srcFile);
            }
            Hashtable hashDest = new Hashtable(fiDest.Length);
            foreach (FileInfo destFile in fiDest)
            {
                hashDest.Add(destFile.Name, destFile);
            }

            // make sure all the selected source files exist in destination
            foreach (FileInfo srcFile in fiSrc)
            {
                bool isUpToDate = false;

                // look up in hash table to see if file exists in destination
                FileInfo destFile = (FileInfo)hashDest[srcFile.Name];
                // if file exists and length, write time and attributes match, it's up to date
                if ((destFile != null) && (srcFile.Length == destFile.Length) &&
                    (srcFile.LastWriteTime == destFile.LastWriteTime) &&
                    (srcFile.Attributes == destFile.Attributes))
                {
                    isUpToDate = true;
                    results.filesUpToDate++;
                }

                // if the file doesn't exist or is different, copy the source file to destination
                if (!isUpToDate)
                {
                    string destPath = Path.Combine(destDir, srcFile.Name);
                    // make sure destination is not read-only
                    if (destFile != null && destFile.IsReadOnly)
                    {
                        destFile.IsReadOnly = false;
                    }

                    try
                    {
                        if (!inputParams.isQuiet)
                        {
                            Console.WriteLine("Copying: {0} -> {1}", srcFile.FullName, Path.GetFullPath(destPath));
                        }
                        // copy the file
                        srcFile.CopyTo(destPath, true);
                        // set attributes appropriately
                        File.SetAttributes(destPath, srcFile.Attributes);
                        results.filesCopied++;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error: failed to copy file from {0} to {1}. {2}",
                            srcFile.FullName, destPath, ex.Message);
                        return false;
                    }
                }
            }

            // delete extra files in destination directory if specified
            if (inputParams.deleteFromDest)
            {
                foreach (FileInfo destFile in fiDest)
                {
                    FileInfo srcFile = (FileInfo)hashSrc[destFile.Name];
                    if (srcFile == null)
                    {
                        // if this file is specified in exclude-from-deletion list, don't delete it
                        if (ShouldExclude(inputParams.deleteExcludeFiles, null, destFile.Name))
                            continue;

                        try
                        {
                            if (!inputParams.isQuiet)
                            {
                                Console.WriteLine("Deleting: {0} ", destFile.FullName);
                            }
                            destFile.IsReadOnly = false;
                            // delete the file
                            destFile.Delete();
                            results.filesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Error: failed to delete file from {0}. {1}",
                                destFile.FullName, ex.Message);
                            return false;
                        }
                    }
                }
            }

            // Get list of selected subdirectories in source directory
            DirectoryInfo[] diSrcSubdirs = GetDirectories(diSrc, inputParams, ref results);
            // Get list of subdirectories in destination directory
            DirectoryInfo[] diDestSubdirs = GetDirectories(diDest, null, ref results);

            // add selected source subdirectories to hash table, and recursively process them
            Hashtable hashSrcSubdirs = new Hashtable(diSrcSubdirs.Length);
            foreach (DirectoryInfo diSrcSubdir in diSrcSubdirs)
            {
                hashSrcSubdirs.Add(diSrcSubdir.Name, diSrcSubdir);
                // recurse into this directory
                if (!ProcessDirectory(diSrcSubdir.FullName, Path.Combine(destDir, diSrcSubdir.Name), inputParams, ref results))
                    return false;
            }

            // delete extra directories in destination if specified
            if (inputParams.deleteFromDest)
            {
                foreach (DirectoryInfo diDestSubdir in diDestSubdirs)
                {
                    // does this destination subdirectory exist in the source subdirs?
                    if (!hashSrcSubdirs.ContainsKey(diDestSubdir.Name))
                    {
                        // if this directory is specified in exclude-from-deletion list, don't delete it
                        if (ShouldExclude(inputParams.deleteExcludeDirs, null, diDestSubdir.Name))
                            continue;

                        try
                        {
                            if (!inputParams.isQuiet)
                            {
                                Console.WriteLine("Deleting directory: {0} ", diDestSubdir.FullName);
                            }
                            // delete directory
                            DeleteDirectory(diDestSubdir.FullName);
                            results.directoriesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Error: failed to delete directory {0}. {1}",
                                diDestSubdir.FullName, ex.Message);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Robustly deletes a directory including all subdirectories and contents
        /// </summary>
        public static void DeleteDirectory(string directory)
        {
            // make sure all files are not read-only
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (FileInfo fileInfo in files)
            {
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }
            }

            // make sure all subdirectories are not read-only
            DirectoryInfo[] directories = directoryInfo.GetDirectories("*.*", SearchOption.AllDirectories);
            foreach (DirectoryInfo subdir in directories)
            {
                if ((subdir.Attributes & FileAttributes.ReadOnly) > 0)
                {
                    subdir.Attributes &= ~FileAttributes.ReadOnly;
                }
            }

            // make sure top level directory is not read-only
            if ((directoryInfo.Attributes & FileAttributes.ReadOnly) > 0)
            {
                directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
            }
            directoryInfo.Delete(true);
        }

        /// <summary>
        /// Gets list of files in specified directory, optionally filtered by specified input parameters
        /// </summary>
        public static FileInfo[] GetFiles(DirectoryInfo directoryInfo, InputParams inputParams, ref Results results)
        {
            // get all files
            List<FileInfo> fileList = new List<FileInfo>(directoryInfo.GetFiles());

            // do we need to do any filtering?
            bool needFilter = (inputParams != null) && (inputParams.areSourceFilesFiltered);

            if (needFilter)
            {
                for (int i = 0; i < fileList.Count; i++)
                {
                    FileInfo fileInfo = fileList[i];

                    // filter out any files based on hiddenness and exclude/include filespecs
                    if ((inputParams.excludeHidden && ((fileInfo.Attributes & FileAttributes.Hidden) > 0)) ||
                         ShouldExclude(inputParams.excludeFiles, inputParams.includeFiles, fileInfo.Name))
                    {
                        fileList.RemoveAt(i);
                        results.filesIgnored++;
                        i--;
                    }
                }
            }

            return fileList.ToArray();
        }

        /// <summary>
        /// Gets list of subdirectories of specified directory, optionally filtered by specified input parameters
        /// </summary>
        public static DirectoryInfo[] GetDirectories(DirectoryInfo directoryInfo, InputParams inputParams, ref Results results)
        {
            // get all directories
            List<DirectoryInfo> directoryList = new List<DirectoryInfo>(directoryInfo.GetDirectories());

            // do we need to do any filtering?
            bool needFilter = (inputParams != null) && (inputParams.areSourceFilesFiltered);
            if (needFilter)
            {
                for (int i = 0; i < directoryList.Count; i++)
                {
                    DirectoryInfo subdirInfo = directoryList[i];

                    // filter out directories based on hiddenness and exclude/include filespecs
                    if ((inputParams.excludeHidden && ((subdirInfo.Attributes & FileAttributes.Hidden) > 0)) ||
                         ShouldExclude(inputParams.excludeDirs, inputParams.includeDirs, subdirInfo.Name))
                    {
                        directoryList.RemoveAt(i);
                        results.directoriesIgnored++;
                        i--;
                    }
                }
            }

            return directoryList.ToArray();
        }

        /// <summary>
        /// Parses list of comma-separated filespecs from specified argument and returns list of regex equivalents as out parameter
        /// </summary>
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
        /// For a given include and exclude list of regex's and a name to match, determines if the
        /// named item should be excluded
        /// </summary>
        private static bool ShouldExclude(Regex[] excludeList, Regex[] includeList, string name)
        {
            if (excludeList != null)
            {
                // check against regex's in our exclude list
                foreach (Regex regex in excludeList)
                {
                    if (regex.Match(name).Success)
                    {
                        // if the name matches an entry in the exclude list, we SHOULD exclude it
                        return true;
                    }
                }
                // no matches in exclude list, we should NOT exclude it
                return false;
            }
            else if (includeList != null)
            {
                foreach (Regex regex in includeList)
                {
                    if (regex.Match(name).Success)
                    {
                        // if the name matches an entry in the include list, we should NOT exclude it
                        return false;
                    }
                }
                // no matches in include list, we SHOULD exclude it
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts specified filespec string to equivalent regex
        /// </summary>
        public static Regex FileSpecToRegex(string fileSpec)
        {
            string pattern = fileSpec.Trim();
            pattern = pattern.Replace(".", @"\.");
            pattern = pattern.Replace("*", @".*");
            pattern = pattern.Replace("?", @".?");
            return new Regex("^" + pattern + "$", RegexOptions.IgnoreCase);
        }

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
        /// Prints usage      
        /// </summary>
        private static void PrintUsage()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("blinksync [options] <source directory tree> <destination directory tree>");
            Console.WriteLine("Where options are:");
            Console.WriteLine("\t-q\tQuiet");
            Console.WriteLine("\t-d\tDelete files and directories in destination which do not appear in source");
            Console.WriteLine("\t-xf <filespec,filespec...>\tExclude files from source that match any of the filespecs");
            Console.WriteLine("\t-xd <filespec,filespec...>\tExclude directories from source that match any of the filespecs");
            Console.WriteLine("\t-xh\tExclude hidden files and directory from source");
            Console.WriteLine("\t-if <filespec,filespec...>\tOnly include files from source that match one of the filespecs");
            Console.WriteLine("\t-id <filespec,filespec...>\tOnly include directories from source that match one of the filespecs");
            Console.WriteLine("\t-ndf <filespec,filespec...>\tExclude files from deletion that match any of the filespecs");
            Console.WriteLine("\t-ndd <filespec,filespec...>\tExclude directories from deletion that match any of the filespecs");
            Console.WriteLine("");
            Console.WriteLine("Include/exclude files options (-if and -xf) may not be combined.");
            Console.WriteLine("Include/exclude directories options (-id and -xd) may not be combined.");
            Console.WriteLine("Exclude-from-deletion options (-ndf and -ndd) require deletion (-d) enabled.");
        }

        private static void PrintInvalidCommandLine()
        {
            Console.Error.WriteLine("Invalid command line syntax.");
            PrintUsage();
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

    public class InputParams
    {
        public bool isQuiet;                    // are we in quiet mode
        public bool excludeHidden;              // should exclude hidden files/directories in source
        public bool deleteFromDest;             // should delete files/directories from dest than are not present in source
        public Regex[] excludeFiles;            // list of filespecs to exclude
        public Regex[] excludeDirs;             // list of directory specs to exclude
        public Regex[] includeFiles;            // list of filespecs to include 
        public Regex[] includeDirs;             // list of directory specs to include
        public Regex[] deleteExcludeFiles;      // list of filespecs NOT to delete from dest
        public Regex[] deleteExcludeDirs;       // list of directory specs NOT to delete from dest

        public bool areSourceFilesFiltered
        {
            get
            {
                return excludeHidden || (includeFiles != null) || (excludeFiles != null) ||
                    (includeDirs != null) || (excludeDirs != null);
            }
        }
    }

    public class Results
    {
        public int filesCopied;
        public int filesUpToDate;
        public int filesDeleted;
        public int filesIgnored;
        public int directoriesCreated;
        public int directoriesDeleted;
        public int directoriesIgnored;

        public void Set(int theFilesCopied, int theFilesUpToDate, int theFilesDeleted, int theFilesIgnored,
            int theDirectoriesCreated, int theDirectoriesDeleted, int theDirectoriesIgnored)
        {
            filesCopied = theFilesCopied;
            filesUpToDate = theFilesUpToDate;
            filesDeleted = theFilesDeleted;
            filesIgnored = theFilesIgnored;
            directoriesCreated = theDirectoriesCreated;
            directoriesDeleted = theDirectoriesDeleted;
            directoriesIgnored = theDirectoriesIgnored;
        }

        public bool CompareTo(Results other)
        {
            if (filesCopied != other.filesCopied)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files copied, got {1}",
                    filesCopied, other.filesCopied);
                return false;
            }
            if (filesUpToDate != other.filesUpToDate)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files up to date, got {1}",
                    filesUpToDate, other.filesUpToDate);
                return false;
            }
            if (filesDeleted != other.filesDeleted)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files deleted, got {1}",
                    filesDeleted, other.filesDeleted);
                return false;
            }
            if (filesIgnored != other.filesIgnored)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} files ignored, got {1}",
                    filesIgnored, other.filesIgnored);
                return false;
            }
            if (directoriesCreated != other.directoriesCreated)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} directories created, got {1}",
                    directoriesCreated, other.directoriesCreated);
                return false;
            }
            if (directoriesDeleted != other.directoriesDeleted)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} directories deleted, got {1}",
                    directoriesDeleted, other.directoriesDeleted);
                return false;
            }
            if (directoriesIgnored != other.directoriesIgnored)
            {
                Console.Error.WriteLine("Result mismatch: expected {0} directories ignored, got {1}",
                    directoriesIgnored, other.directoriesIgnored);
                return false;
            }
            return true;
        }
    }
}
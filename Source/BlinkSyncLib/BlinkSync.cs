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

namespace BlinkSyncLib
{

    public class Sync
    {
        /// <summary>
        /// Set this property to log the synchronization progress by this class to the given delegate. 
        /// For example, to log to the console, set this property to Console.Write.
        /// </summary>
        public virtual Action<string> Log { get; set; }

        /// <summary>
        /// Performs one-way synchronization from source directory tree to destination directory tree
        /// </summary>
        public SyncResults Start(string srcDir, string destDir, InputParams inputParams)
        {
            SyncResults results = new SyncResults();

            if (Validate(srcDir, destDir, inputParams))
            {                
                // recursively process directories
                ProcessDirectory(srcDir, destDir, inputParams, ref results);
                
            }

            return results;
        }

        /// <summary>
        /// Robustly deletes a directory including all subdirectories and contents
        /// </summary>
        public void DeleteDirectory(string directory)
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
        public FileInfo[] GetFiles(DirectoryInfo directoryInfo, InputParams inputParams, ref SyncResults results)
        {
            // get all files
            List<FileInfo> fileList = new List<FileInfo>(directoryInfo.GetFiles());

            // do we need to do any filtering?
            bool needFilter = (inputParams != null) && (inputParams.AreSourceFilesFiltered);

            if (needFilter)
            {
                for (int i = 0; i < fileList.Count; i++)
                {
                    FileInfo fileInfo = fileList[i];

                    // filter out any files based on hiddenness and exclude/include filespecs
                    if ((inputParams.ExcludeHidden && ((fileInfo.Attributes & FileAttributes.Hidden) > 0)) ||
                         ShouldExclude(inputParams.ExcludeFiles, inputParams.IncludeFiles, fileInfo.Name))
                    {
                        fileList.RemoveAt(i);
                        results.FilesIgnored++;
                        i--;
                    }
                }
            }

            return fileList.ToArray();
        }

        /// <summary>
        /// Gets list of subdirectories of specified directory, optionally filtered by specified input parameters
        /// </summary>
        public DirectoryInfo[] GetDirectories(DirectoryInfo directoryInfo, InputParams inputParams, ref SyncResults results)
        {
            // get all directories
            List<DirectoryInfo> directoryList = new List<DirectoryInfo>(directoryInfo.GetDirectories());

            // do we need to do any filtering?
            bool needFilter = (inputParams != null) && (inputParams.AreSourceFilesFiltered);
            if (needFilter)
            {
                for (int i = 0; i < directoryList.Count; i++)
                {
                    DirectoryInfo subdirInfo = directoryList[i];

                    // filter out directories based on hiddenness and exclude/include filespecs
                    if ((inputParams.ExcludeHidden && ((subdirInfo.Attributes & FileAttributes.Hidden) > 0)) ||
                         ShouldExclude(inputParams.ExcludeDirs, inputParams.IncludeDirs, subdirInfo.Name))
                    {
                        directoryList.RemoveAt(i);
                        results.DirectoriesIgnored++;
                        i--;
                    }
                }
            }

            return directoryList.ToArray();
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        private bool Validate(string srcDir, string destDir, InputParams parameters)
        {
            if (((parameters.IncludeFiles != null) && (parameters.ExcludeFiles != null)) ||
                ((parameters.IncludeDirs != null) && (parameters.ExcludeDirs != null)))
            {
                PrintUsage();
                return false;
            }

            string fullSrcDir = Path.GetFullPath(srcDir);
            string fullDestDir = Path.GetFullPath(destDir);
            if (destDir.StartsWith(fullSrcDir) || srcDir.StartsWith(fullDestDir))
            {
                Trace("Error: source directory {0} and destination directory {1} cannot contain each other", fullSrcDir, fullDestDir);
                return false;
            }

            if (((parameters.DeleteExcludeFiles != null) || (parameters.DeleteExcludeDirs != null)) &&
                (!parameters.DeleteFromDest))
            {
                Trace("Error: exclude-from-deletion options (-ndf and -ndd) require deletion (-d) enabled.");
                return false;
            }

            // ensure source directory exists
            if (!Directory.Exists(srcDir))
            {
                this.Trace("Error: source directory {0} not found", srcDir);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Recursively performs one-way synchronization from a single source to destination directory
        /// </summary>
        private bool ProcessDirectory(string srcDir, string destDir, InputParams inputParams, ref SyncResults results)
        {
            DirectoryInfo diSrc = new DirectoryInfo(srcDir);
            DirectoryInfo diDest = new DirectoryInfo(destDir);

            // create destination directory if it doesn't exist
            if (!diDest.Exists)
            {
                try
                {
                    if (!inputParams.IsQuiet)
                    {
                        Trace("Creating directory: {0}", diDest.FullName);
                    }
                    // create the destination directory
                    diDest.Create();
                    results.DirectoriesCreated++;
                }
                catch (Exception ex)
                {
                    Trace("Error: failed to create directory {0}. {1}", destDir, ex.Message);
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
                    results.FilesUpToDate++;
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
                        if (!inputParams.IsQuiet)
                        {
                            Trace("Copying: {0} -> {1}", srcFile.FullName, Path.GetFullPath(destPath));
                        }
                        // copy the file
                        srcFile.CopyTo(destPath, true);
                        // set attributes appropriately
                        File.SetAttributes(destPath, srcFile.Attributes);
                        results.FilesCopied++;
                    }
                    catch (Exception ex)
                    {
                        Trace("Error: failed to copy file from {0} to {1}. {2}", srcFile.FullName, destPath, ex.Message);
                        return false;
                    }
                }
            }

            // delete extra files in destination directory if specified
            if (inputParams.DeleteFromDest)
            {
                foreach (FileInfo destFile in fiDest)
                {
                    FileInfo srcFile = (FileInfo)hashSrc[destFile.Name];
                    if (srcFile == null)
                    {
                        // if this file is specified in exclude-from-deletion list, don't delete it
                        if (ShouldExclude(inputParams.DeleteExcludeFiles, null, destFile.Name))
                            continue;

                        try
                        {
                            if (!inputParams.IsQuiet)
                            {
                                Trace("Deleting: {0} ", destFile.FullName);
                            }
                            destFile.IsReadOnly = false;
                            // delete the file
                            destFile.Delete();
                            results.FilesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Trace("Error: failed to delete file from {0}. {1}", destFile.FullName, ex.Message);
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
            if (inputParams.DeleteFromDest)
            {
                foreach (DirectoryInfo diDestSubdir in diDestSubdirs)
                {
                    // does this destination subdirectory exist in the source subdirs?
                    if (!hashSrcSubdirs.ContainsKey(diDestSubdir.Name))
                    {
                        // if this directory is specified in exclude-from-deletion list, don't delete it
                        if (ShouldExclude(inputParams.DeleteExcludeDirs, null, diDestSubdir.Name))
                            continue;

                        try
                        {
                            if (!inputParams.IsQuiet)
                            {
                                Trace("Deleting directory: {0} ", diDestSubdir.FullName);
                            }
                            // delete directory
                            DeleteDirectory(diDestSubdir.FullName);
                            results.DirectoriesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            Trace("Error: failed to delete directory {0}. {1}", diDestSubdir.FullName, ex.Message);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Parses list of comma-separated filespecs from specified argument and returns list of regex equivalents as out parameter
        /// </summary>
        private bool ParseFilespecs(string[] args, int iArg, out Regex[] matches)
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
        private bool ShouldExclude(Regex[] excludeList, Regex[] includeList, string name)
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
        private Regex FileSpecToRegex(string fileSpec)
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
        private Regex[] FileSpecsToRegex(string[] fileSpecs)
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
        private void PrintUsage()
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

        /// <summary>
        /// Trace message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void Trace(string message, params object[] args)
        {
            if (this.Log != null)
                this.Log.Invoke(String.Format(message, args));

        }
    }

}
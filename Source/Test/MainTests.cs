using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BlinkSyncLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlinkSyncTests
{
    [TestClass]
    public class MainTest
    {
        public const string baseDirSrc = @"C:\TestSrc";
        public const string baseDirDest = @"C:\TestDest";

        private InputParams inputParams;
        private SyncResults expectedResults;

        [TestInitialize]
        public void Initialization()
        {
            inputParams = new InputParams();
            expectedResults = new SyncResults();
            inputParams.IsQuiet = true;
        }

        [TestMethod]
        public void Copying4Files_DestinationFolderDoNotExist()
        {
            inputParams.DeleteFromDest = true;

            // test copying 4 files, dest dirs do not exist yet
            expectedResults.Set(4, 0, 0, 0, 2, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void Copying4Files_DestinationFolderExist()
        {
            inputParams.DeleteFromDest = true;

            // test copying 4 files where dest dirs exist
            expectedResults.Set(4, 0, 0, 0, 0, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"foo" }, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void Copying4Files_2FilesAlreadyExistAndAreUpToDate()
        {
            inputParams.DeleteFromDest = true;

            // test copying 4 files where 2 files already exist and are up to date
            expectedResults.Set(2, 2, 0, 0, 0, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"foo" }, new string[] { @"*c|foo.txt", @"*c|foo\foo1.txt" },
               inputParams, expectedResults);
        }

        [TestMethod]
        public void Copying4Files_2FilesAlreadyExistButAreDifferent()
        {
            inputParams.DeleteFromDest = true;

            // test copying 4 files where 2 files already exist but are different
            expectedResults.Set(4, 0, 0, 0, 0, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"foo" }, new string[] { @"foo.txt", @"foo\foo1.txt" },
               inputParams, expectedResults);
        }

        //***
        [TestMethod]
        public void Copying4Files_2AdditionalFilesExistInDestination()
        {
            inputParams.DeleteFromDest = true;

            // test copying 4 files where 2 additional files exist in destination
            expectedResults.Set(4, 0, 2, 0, 0, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"foo" }, new string[] { @"foo\foo3.txt", @"foo\foo4.txt" },
               inputParams, expectedResults);
        }

        //***
        [TestMethod]
        public void Copying4Files_AdditionalDirectoryExistsInDestination()
        {
            inputParams.DeleteFromDest = true;

            // test copying 4 files where additional directory exists in destination
            expectedResults.Set(4, 0, 0, 0, 1, 1, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"barbar" }, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void Copying4Files_2AdditionalFileExistInDestination()
        {
            inputParams.DeleteFromDest = false;

            // test copying 4 files where 2 additional files exist in destination
            expectedResults.Set(4, 0, 0, 0, 0, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"foo" }, new string[] { @"foo\foo3.txt", @"foo\foo4.txt" },
               inputParams, expectedResults);
        }

        [TestMethod]
        public void Copying4Files_AdditionalDirectoryInDestination()
        {
            // test copying 4 files where additional directory exists in destination
            expectedResults.Set(4, 0, 0, 0, 1, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"barbar" }, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void Copying4Files_2AreHidden()
        {
            inputParams.DeleteFromDest = true;

            // test copying 4 files, 2 are hidden
            expectedResults.Set(4, 0, 0, 0, 2, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"*h|bar.txt", @"foo\foo1.txt", @"*h|foo\foo2.txt" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void Copying4Files_2AreHidden_WithExcludeHidden()
        {
            // test copying 4 files, 2 are hidden
            expectedResults.Set(2, 0, 0, 2, 2, 0, 0);
            inputParams.ExcludeHidden = true;
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"*h|bar.txt", @"foo\foo1.txt", @"*h|foo\foo2.txt" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFiles_ExcludeFileFilter()
        {
            inputParams.ExcludeHidden = false;

            // test copying files with an exclude file filter -- foo.jpg should not get copied
            inputParams.ExcludeFiles = SyncTools.FileSpecsToRegex(new string[] { "*.jpg", "*.wmv" });
            expectedResults.Set(3, 0, 0, 1, 2, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.jpg", @"bar.txt", @"foo\foo1.png", @"foo\txt.foo" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFiles_ExcludeFileFilterNotMatchAnyFiles()
        {
            // test copying files with an exclude file filter that will not match any files
            inputParams.ExcludeFiles = SyncTools.FileSpecsToRegex(new string[] { "*.abc", "foo" });
            expectedResults.Set(4, 0, 0, 0, 2, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.jpg", @"bar.txt", @"foo\foo1.png", @"foo\txt.foo" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFiles_ExcludeFileFilterCase()
        {
            // test copying files with another exclude file filter case
            inputParams.ExcludeFiles = SyncTools.FileSpecsToRegex(new string[] { "*foo*" });
            expectedResults.Set(1, 0, 0, 3, 2, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.jpg", @"bar.txt", @"foo\foo1.png", @"foo\txt.foo" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFile_DirectoryExcludeFilter()
        {
            inputParams.ExcludeFiles = null;

            // test copying files with a directory exclude filter
            inputParams.ExcludeDirs = SyncTools.FileSpecsToRegex(new string[] { "foo*" });
            expectedResults.Set(3, 0, 0, 0, 2, 0, 1);
            TestOneCase(new string[] { @"foo", @"bar" }, new string[] { @"foo.jpg", @"bar.txt", @"foo\foo1.png", @"bar\txt.foo" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFiles_FileIncludeFilter()
        {
            inputParams.ExcludeDirs = null;

            // test copying files with a file include filter
            inputParams.IncludeFiles = SyncTools.FileSpecsToRegex(new string[] { "*.jpg", "txt*" });
            expectedResults.Set(2, 0, 0, 2, 2, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.jpg", @"bar.txt", @"foo\foo1.png", @"foo\txt.foo" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFiles_DirectoryIncludeFilter()
        {
            inputParams.IncludeFiles = null;
            inputParams.IncludeDirs = SyncTools.FileSpecsToRegex(new string[] { "foo*" });

            // test copying files with a directory include filter

            expectedResults.Set(3, 0, 0, 0, 2, 0, 2);
            TestOneCase(new string[] { @"foo", @"bar", @"bar2" }, new string[] { @"foo.jpg", @"bar.txt", @"foo\foo1.png", @"bar\txt.foo", @"bar2\thing.jig" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFiles_DirectoryIncludeFilterNotMatchAnySubdirectories()
        {
            inputParams.IncludeDirs = SyncTools.FileSpecsToRegex(new string[] { "marvin" });

            // test copying files with a directory include filter that doesn't match any subdirectories            
            expectedResults.Set(2, 0, 0, 0, 1, 0, 3);
            TestOneCase(new string[] { @"foo", @"bar", @"bar2" }, new string[] { @"foo.jpg", @"bar.txt", @"foo\foo1.png", @"bar\txt.foo", @"bar2\thing.jig" },
                null, null,
               inputParams, expectedResults);
        }

        [TestMethod]
        public void CopyingFiles_ExcludeFromDeletionFileFilter()
        {
            inputParams.IncludeDirs = null;
            inputParams.DeleteFromDest = true;
            inputParams.DeleteExcludeFiles = SyncTools.FileSpecsToRegex(new string[] { "*.jpg" });

            // test copying files with an exclude-from-deletion file filter            
            expectedResults.Set(4, 0, 1, 0, 0, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"foo" }, new string[] { @"foo\foo3.jpg", @"foo\foo4.txt" },
               inputParams, expectedResults);
        }

        //***
        [TestMethod]
        public void CopyingFiles_ExcludeFromDeletionDirectoryFilter()
        {
            inputParams.DeleteExcludeFiles = null;
            inputParams.DeleteFromDest = true;
            inputParams.DeleteExcludeDirs = SyncTools.FileSpecsToRegex(new string[] { "f*" });

            // test copying files with an exclude-from-deletion directory filter            
            expectedResults.Set(4, 0, 2, 0, 0, 0, 0);
            TestOneCase(new string[] { @"foo" }, new string[] { @"foo.txt", @"bar.txt", @"foo\foo1.txt", @"foo\foo2.txt" },
                new string[] { @"foo", "foo1" }, new string[] { @"foo\foo3.jpg", @"foo\foo4.txt" },
               inputParams, expectedResults);
        }

        /// <summary>
        /// Runs a test case
        /// </summary>
        private static void TestOneCase(string[] srcDirectories, string[] srcFiles, string[] destDirectories, string[] destFiles, InputParams inputParams, SyncResults expectedResults)
        {
            
            // delete base directories in case they were hanging around from a previous failed test
            DeleteTestDirectory(baseDirSrc);
            DeleteTestDirectory(baseDirDest);

            // create source directories and files specified by test
            CreateTestDirectories(baseDirSrc, srcDirectories);
            CreateTestFiles(baseDirSrc, null, srcFiles);
            // create destination directories and files specified by test
            if (destDirectories != null)
            {
                CreateTestDirectories(baseDirDest, destDirectories);
            }
            if (destFiles != null)
            {
                CreateTestFiles(baseDirDest, baseDirSrc, destFiles);
            }

            // perform the directory sync
            SyncResults results = new SyncResults();
            results = new Sync(baseDirSrc, baseDirDest).Start(inputParams);

            // Assert we have expected results
            Assert.IsTrue(SyncTools.CompareTo(expectedResults, results));

            // If we are deleting extra files from destination, verify we have exactly the same files as filtered source files
            if (inputParams.DeleteFromDest &&
                (!(inputParams.DeleteExcludeFiles != null) && !(inputParams.DeleteExcludeDirs != null)))
            {
                // calc hash of filtered files & directories in source tree
                byte[] hashSrc = CalcHash(baseDirSrc, inputParams);
                // calc hash of all files & directories in destination tree
                byte[] hashDest = CalcHash(baseDirDest, null);
                // hashes must match
                bool hashesMatch = SyncTools.CompareByteArrays(hashSrc, hashDest);
                Assert.IsTrue(hashesMatch);
            }

            DeleteTestDirectory(baseDirSrc);
            DeleteTestDirectory(baseDirDest);
        }

        /// <summary>
        /// Creates directories in specified array of directory names
        /// </summary>
        private static void CreateTestDirectories(string baseDir, string[] directories)
        {
            foreach (string directory in directories)
            {
                string directoryPath = Path.Combine(baseDir, directory);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
        }

        /// <summary>
        /// Deletes a test case directory, ignoring failures
        /// </summary>
        private static void DeleteTestDirectory(string dir)
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Creates files from specified array of files names
        /// </summary>
        private static void CreateTestFiles(string baseDir, string srcDir, string[] files)
        {
            foreach (string file in files)
            {
                bool isHidden;  // create this file hidden
                bool isCopy;    // this is a destination file which should be an exact copy of the source
                // get file name and metadata
                string fileName = StripMetadataFromFilename(file, out isHidden, out isCopy);
                if (!isCopy)
                {
                    // create a new test file
                    string filePath = Path.Combine(baseDir, fileName);
                    FileStream fileStream = File.Create(filePath);

                    // seed a random number generator with a hash of the file name so we get exactly repeatable pseudorandom behavior
                    byte[] hashBytes = MD5.Create().ComputeHash(ASCIIEncoding.UTF8.GetBytes(filePath));
                    Random random = new Random(BitConverter.ToInt32(hashBytes, 0));

                    // pick a (deterministically seeded) random length
                    int length = random.Next(1, 16384);
                    byte[] fileBytes = new byte[length];
                    // fill the file with (deterministically seeded) random data
                    for (int i = 0; i < length; i++)
                    {
                        fileBytes[i] = (byte)random.Next(0, 255);
                    }
                    fileStream.Write(fileBytes, 0, fileBytes.Length);
                    fileStream.Close();

                    // set file as hidden if specified
                    if (isHidden)
                    {
                        File.SetAttributes(filePath, FileAttributes.Hidden);
                    }
                }
                else
                {
                    // copy file from source to dest
                    string srcPath = Path.Combine(srcDir, fileName);
                    string destPath = Path.Combine(baseDir, fileName);
                    File.Copy(srcPath, destPath);
                }
            }
        }

        /// <summary>
        /// Calculate a hash of the specified directory tree, filtered by inputParams if non-null
        /// </summary>
        private static byte[] CalcHash(string directory, InputParams inputParams)
        {
            MemoryStream memoryStream = new MemoryStream();

            // build a stream of the directory contents we want to hash
            BuildCRCStream(directory, inputParams, ref memoryStream);

            // create the hash
            MD5 md5 = MD5.Create();
            return md5.ComputeHash(memoryStream.GetBuffer());
        }

        /// <summary>
        /// Fill a stream with relevant contents of directory tree for hashing.  Directory tree is filtered by
        /// inputParams if non-null.
        /// </summary>
        private static void BuildCRCStream(string directory, InputParams inputParams, ref MemoryStream memoryStream)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            SyncResults results = new SyncResults();
            // get filtered list of files in this directory 
            FileInfo[] files = new Sync().GetFiles(directoryInfo, inputParams, ref results);
            // sort by name for deterministic order
            Array.Sort(files, new FileInfoComparer());
            // write information about each file to stream
            foreach (FileInfo fileInfo in files)
            {
                byte[] bytes = ASCIIEncoding.UTF8.GetBytes(fileInfo.Name);
                memoryStream.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(fileInfo.Length);
                memoryStream.Write(bytes, 0, bytes.Length);
                bytes = BitConverter.GetBytes(fileInfo.LastWriteTime.ToBinary());
                memoryStream.Write(bytes, 0, bytes.Length);
                bytes = ASCIIEncoding.UTF8.GetBytes(fileInfo.Attributes.ToString());
                memoryStream.Write(bytes, 0, bytes.Length);
            }

            // get filtered list of subdirectories
            DirectoryInfo[] subdirs = new Sync().GetDirectories(directoryInfo, inputParams, ref results);
            // sort by name for deterministic order
            Array.Sort(subdirs, new DirectoryInfoComparer());

            foreach (DirectoryInfo subdir in subdirs)
            {
                // write information about each subdirectory to stream
                byte[] bytes = ASCIIEncoding.UTF8.GetBytes(subdir.Name);
                memoryStream.Write(bytes, 0, bytes.Length);
                // recurse
                BuildCRCStream(Path.Combine(directory, subdir.Name), inputParams, ref memoryStream);
            }
        }

        /// <summary>
        /// Removes metadata from test case filename and returns the metadata
        /// </summary>
        private static string StripMetadataFromFilename(string fileName, out bool isHidden, out bool isCopy)
        {
            isHidden = false;
            isCopy = false;
            if (fileName[0] != '*')
            {
                return fileName;
            }

            int pos = 1;
            char c;
            while ((c = fileName[pos]) != '|')
            {
                if (c == 'h')
                {
                    isHidden = true;
                }
                else if (c == 'c')
                {
                    isCopy = true;
                }
                else
                {
                    throw new Exception(String.Format("Unknown test file metadata '{0}'", c));
                }
                pos++;
            }

            return fileName.Remove(0, pos + 1);
        }

        
    }

}

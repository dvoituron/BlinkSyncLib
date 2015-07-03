# BlinkSyncLib file and directory synchronization library

##Introduction

BlinkSyncLib is a free file and directory synchronization library. It's based on source code of BlinkSync command line ([http://blinksync.sourceforge.net](http://blinksync.sourceforge.net)).

## How to use BlinkSyncLib

To copy all files from the SourceFolder to the TargetFolder.

    Sync sync = new Sync(@"C:\SourceFolder", @"C:\TargetFolder");
    sync.Start();

The more interesting form is when you include the "delete from destination" flag. This means to make the destination look EXACTLY like the source by deleting any files in the destination tree that don't appear in the source tree. This solves the problem of leaving copies of removed files "lying around" forever in the destination if you just use a standard file copy.

    Sync sync = new Sync(@"C:\SourceFolder", @"C:\TargetFolder");
    sync.Configuration.DeleteFromDest = true;
    sync.Start();

There are many other options for more control if you need it.

- **DeleteFromDest**: Delete files and directories in destination which do not appear in source
- **ExcludeFiles**: Exclude files from source that match any of the filespecs
- **ExcludeDirs**: Exclude directories from source that match any of the filespecs
- **ExcludeHidden**: Exclude hidden files and directory from source
- **IncludeFiles**: Only include files from source that match one of the filespecs
- **IncludeDirs**: Only include directories from source that match one of the filespecs
- **DeleteExcludeFiles**: Exclude files from deletion that match any of the filespecs
- **DeleteExcludeDirs**: Exclude directories from deletion that match any of the filespecs


Include/exclude files options (IncludeFiles and ExcludeFiles) may not be combined.
Include/exclude directories options (IncludeDirs and ExcludeDirs) may not be combined.
Exclude-from-deletion options (DeleteExcludeFiles and DeleteExcludeDirs) require deletion (DeleteFromDest) enabled.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zip;

namespace GitVersionReleaseManager
{
    internal class Zip
    {
        public void AddFilesInDirectory(DirectoryInfo dir, ZipFile zip, List<string> excludeFiles, Stack<string> pathParts)
        {
            foreach (var f in dir.EnumerateFiles())
            {
                // Do not include zip files in the top level directory

                if (!pathParts.Any() && f.Extension == ".zip")
                {
                    continue;
                }
                var fileDir = Path.Combine(pathParts.Reverse().ToArray());
                var filePath = Path.Combine(fileDir, f.Name);
                Boolean exludeThisFile = false;
                foreach (var fileToExclude in excludeFiles)
                {
                    if (string.Equals(filePath, fileToExclude, StringComparison.OrdinalIgnoreCase))
                    {
                        exludeThisFile = true;
                        break;
                    }
                }
                if (!exludeThisFile)
                {
                    zip.AddFile(f.FullName, fileDir);
                }
            }
        }

        public void AddAllInDirectory(DirectoryInfo dir, ZipFile zip, List<string> excludeFiles, Stack<string> pathParts = null)
        {
            if (pathParts == null)
                pathParts = new Stack<string>();

            AddFilesInDirectory(dir, zip, excludeFiles ,pathParts);

            foreach (var subdir in dir.EnumerateDirectories())
            {
                pathParts.Push(subdir.Name);
                AddAllInDirectory(subdir, zip,excludeFiles, pathParts);
                pathParts.Pop();
            }
        }

        public void CreateRelease(string releaseFolder, int version, IOptions options)
        {
            using (var zip = new ZipFile())
            {
                var baseDirectory = new DirectoryInfo(releaseFolder);

                var folderName = baseDirectory.Name;
                string zipFileName = $"{folderName}-{version}.zip";

                AddAllInDirectory(baseDirectory, zip, options.ExcludeFiles);
                var versionInfo = new VersionInfo(options.ProjectPath);
                foreach (var additionalVersionFolderPath in options.VersionFilePaths)
                {
                    zip.AddFile(versionInfo.VersionFilePath, additionalVersionFolderPath);
                }
                zip.Save(Path.Combine(releaseFolder, zipFileName));
                Console.WriteLine($"Release file created {zipFileName}");
            }
        }
    }
}
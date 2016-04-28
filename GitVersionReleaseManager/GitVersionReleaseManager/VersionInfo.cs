using System;
using System.Collections;
using System.Globalization;
using System.IO;
using IniParser.Model;

namespace GitVersionReleaseManager
{
    internal class VersionInfo
    {
        private readonly string _selectedFolder;
        private const string VersionFileName = "version.txt";

        internal string VersionFilePath => Path.GetFullPath(Path.Combine(_selectedFolder, VersionFileName));

        public VersionInfo(string selectedFolder)
        {
            _selectedFolder = selectedFolder;
        }

        public VersionData GetVersionData()
        {
            var dataReader = new IniSerializer<VersionData>();
            if (!File.Exists(VersionFilePath))
            {
                throw new FileNotFoundException(string.Format("Version file does not exist: ", VersionFilePath));
            }
            return dataReader.ReadFromFile(VersionFilePath);
        }



        public void IncrementVersion(VersionData data)
        {
            data.Version += 1;
        }

        public void InitalizeFile()
        {
            if (File.Exists(VersionFilePath))
            {
                throw new IOException($"The version file ({VersionFilePath}) already exists, and can not be created.");
            }
            var versionData = new VersionData
            {
                Version = 0,
                BuildDate = DateTime.Now
            };
            var ini = new IniSerializer<VersionData>();
            ini.WriteToFile(versionData, VersionFilePath);
        }



        internal VersionData UpdateWithReleaseInfo(string sha1)
        {
            VersionData versionData = GetVersionData();
            versionData.ProjectName = new DirectoryInfo(_selectedFolder).Name;
            IncrementVersion(versionData);
            versionData.BuildDate = DateTime.Now;
            versionData.SHA1 = sha1;
            var ini = new IniSerializer<VersionData>();
            ini.WriteToFile(versionData, VersionFilePath);
            return versionData;
        }
    }
}
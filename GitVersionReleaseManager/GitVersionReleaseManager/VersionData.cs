using System;

namespace GitVersionReleaseManager
{
    public class VersionData
    {
        [Section("info")]
        public int Version { get; set; }

        [Section("source control")]
        public string SHA1 { get; set; }

        [Section("build")]
        public DateTime BuildDate { get; set; }


        [Section("build")]
        public string ProjectName { get; set; }
    }
}
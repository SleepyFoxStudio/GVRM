using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace GitVersionReleaseManager
{
    interface IOptions
    {
        bool Create { get; }
        List<string> ExcludeFiles { get; }
        string ProjectPath { get; }
        IEnumerable<string> VersionFilePaths { get; }
    }

    class Options : IOptions
    {
        private readonly Lazy<List<string>> _lazyExcludeFilesList;
        
        public Options()
        {
            _lazyExcludeFilesList = new Lazy<List<string>>(() => ExcludeFiles.ToList());
        }

        [Value(0, Required = true, HelpText = "the folder containing the files for this release.", MetaName = "folder")]
        public string ProjectPath { get; set; }


        [Option('c', "create", HelpText = "Create new version file in selected folder")]
        public bool Create { get; set; }


        [Option('f', "versionFilePaths", HelpText = "put version file in these additional folders within zip, path releative to zip root.")]
        public IEnumerable<string> VersionFilePaths { get; set; }

        [Option('e', "exclude", HelpText = "exclude the following files from the release")]
        public IEnumerable<string> ExcludeFiles { get; set; }

        List<string> IOptions.ExcludeFiles
        {
            get { return _lazyExcludeFilesList.Value; }
        }


        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Create a new release from scratch", new Options { ProjectPath = "c:/foo/bar project", Create = true });
                yield return new Example("Update version of release", new Options { ProjectPath = "c:/foo/bar project" });
                yield return new Example("Update version of release, but exclude sample.txt", new Options { ProjectPath = "c:/foo/bar project", ExcludeFiles = new[] {"sample.txt"}});
                yield return new Example("Update version of release, and put version file in root and in /foo in zip file", new Options { ProjectPath = "c:/foo/bar project" });
            }
        }
    }


}

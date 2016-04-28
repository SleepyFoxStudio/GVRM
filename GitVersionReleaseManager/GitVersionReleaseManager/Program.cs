using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using LibGit2Sharp;
using Ionic.Zip;

namespace GitVersionReleaseManager
{
    class Program
    {

        static int Main(string[] args)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            }

            var result = CommandLine.Parser.Default.ParseArguments<Options>(args);

            if (result is NotParsed<Options>)
                return 1;

            var parsedResult = result as Parsed<Options>;
            if (parsedResult != null)
            {
                IOptions options = parsedResult.Value;
                return RunReleaseManager(options);
            }

            LogHelper.Log("Error in option parsing library");
            return 99;
        }

        private static int RunReleaseManager(IOptions appOptions)
        {
            if (!Directory.Exists(appOptions.ProjectPath))
            {
                throw new Exception("Folder specified for release does not exist.");
            }
            Console.WriteLine("Please enter Password for Repo");
            var consoleUtils = new ConsoleUtils();
            string gitPass = consoleUtils.ReadPasswordInput();
            string repoPath = GetRepoPath(appOptions.ProjectPath);

            var git = new GitHelper(gitPass, repoPath);
            var repoSynched = git.CheckRepoSynched();
            if (!repoSynched)
            {
                throw new Exception("Sorry your repo is not up to date, Release aborted.");
            }
            else
            {
                var versionInfo = new VersionInfo(appOptions.ProjectPath);
                if (appOptions.Create)
                {
                    versionInfo.InitalizeFile();
                }

                try
                {
                    var releaseVersionData = versionInfo.UpdateWithReleaseInfo(git.GetTipSha());
                    var zip = new Zip();
                    zip.CreateRelease(appOptions.ProjectPath, releaseVersionData.Version, appOptions);
                    git.StageAndCommitAll(releaseVersionData, appOptions);
                    git.Push(releaseVersionData);
                }
                catch (FileNotFoundException exception)
                {
                    Console.WriteLine("The Version file has not been created, run with the -c option"+ exception);
                }
            }
            Console.ReadKey();

            return 0;
        }

        private static string GetRepoPath(string projectPath)
        {
            DirectoryInfo di = new DirectoryInfo(projectPath);
            while (di.FullName != di.Root.FullName)
            {
                Console.WriteLine($"Looking for repo root in {di.FullName}");
                if (Repository.IsValid(di.FullName))
                {
                    Console.WriteLine($"Found repo root {di.FullName}");
                    return di.FullName;
                }
                di = di.Parent;
            }
            throw new Exception("Could not find repo Root");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine("Fatal error occurred, see exception below:");
            Console.WriteLine("Press any key to exit.");
            Console.Error.WriteLine(e.ExceptionObject);
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    internal static class LogHelper
    {
        public static void Log(object error)
        {
            Console.Error.WriteLine(error);
        }

        public static void Log(Error error)
        {
            Console.Error.WriteLine();
        }
    }
}

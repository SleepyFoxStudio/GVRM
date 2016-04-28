using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace GitVersionReleaseManager
{
    internal class GitHelper
    {
        private readonly string _repoPath;
        private readonly string _repoUser;
        private readonly string _repoEmail;
        private readonly string _repoPass;
        private readonly string _masterBranchName;
        internal GitHelper(string pass, string repoPath)
        {
            _repoPath = repoPath;
            _masterBranchName = @"master";
            _repoPass = pass;
            using (var repo = new Repository(_repoPath))
            {
                _repoUser = repo.Config.Get<string>("user.name").Value;
                _repoEmail = repo.Config.Get<string>("user.email").Value;
            }
        }

        public bool CheckRepoSynched()
        {
            var repoSynched = true;
            using (var repo = new Repository(_repoPath))
            {
                if (repo.Head.FriendlyName != _masterBranchName)
                {
                    Console.WriteLine("BARF!  you are not on master branch");
                    repoSynched = false;
                }

                GitFetch();

                if ((repo.Head.TrackingDetails.AheadBy ?? 0) > 0)
                {
                    Console.WriteLine($"{repo.Head.FriendlyName}: Can`t continue because you are ahead by: {repo.Head.TrackingDetails.BehindBy}");
                    repoSynched = false;
                }
                if ((repo.Head.TrackingDetails.BehindBy ?? 0) > 0)
                {
                    Console.WriteLine($"{repo.Head.FriendlyName}: Can`t continue because you are behind by: {repo.Head.TrackingDetails.BehindBy}");
                    repoSynched = false;
                }

                foreach (var e in repo.Index)
                {
                    if (e.StageLevel != 0)
                    {
                        Console.WriteLine("UNSTAGED FILES: {0} {1} {2}       {3}", Convert.ToString((int)e.Mode, 8), e.Id, (int)e.StageLevel, e.Path);
                        repoSynched = false;
                    }
                }

                foreach (var item in repo.RetrieveStatus())
                {
                    if (item.State != FileStatus.Ignored)
                    {
                        repoSynched = false;
                    }
                    Console.WriteLine("UNTRACKED FILES: {0} ({1})", item.FilePath, item.State);
                }

            }
            return repoSynched;
        }

        public void GitFetch()
        {

            var creds = new UsernamePasswordCredentials()
            {
                Username = _repoUser,
                Password = _repoPass
            };
            CredentialsHandler credHandler = (url, user, cred) => creds;
            var fetchOpts = new FetchOptions { CredentialsProvider = credHandler };
            using (var repo = new Repository(_repoPath))
            {
                var username = repo.Config.ToList();
                repo.Network.Fetch(repo.Network.Remotes["origin"], fetchOpts);
            }
        }

        public string GetTipSha()
        {
            using (var repo = new Repository(_repoPath))
            {
                return (repo.Head.Tip.Sha);
            }
        }


        public void StageAndCommitAll(VersionData versionData, IOptions appOptions)
        {
            using (var repo = new Repository(_repoPath))
            {
                VersionInfo versionInfo = new VersionInfo(appOptions.ProjectPath);
                // Stage the files
                Console.WriteLine($"Stage {versionInfo.VersionFilePath}");
                repo.Stage(versionInfo.VersionFilePath);

                // Create the committer's signature and commit
                var author = new Signature(_repoUser, _repoEmail, DateTime.Now);
                var committer = author;

                // Commit to the repository
                repo.Commit($"Created a new release {GetCommitName(versionData)}", author, committer);
                Console.WriteLine("Commited files to GIT");
            }
        }

        public void Push(VersionData versionData)
        {
            using (var repo = new Repository(_repoPath))
            {
                var options = new PushOptions();
                var remote = repo.Network.Remotes["origin"];
                options.CredentialsProvider = new CredentialsHandler(
                    (url, usernameFromUrl, types) =>
                        new UsernamePasswordCredentials()
                        {
                            Username = _repoUser,
                            Password = _repoPass
                        });
                Tag t = repo.ApplyTag(GetCommitName(versionData));

                repo.Network.Push(remote, new[] { @"refs/heads/master", t.ToString() }, options);
            }
        }

        internal static string GetCommitName(VersionData versionData)
        {
            return $"GVRM-{GetShortReleaseName(versionData)}-v{versionData.Version}";
        }

        private static string GetShortReleaseName(VersionData versionData)
        {
            var rgx = new Regex("[^a-zA-Z0-9-_]");
            return rgx.Replace(versionData.ProjectName.Replace(" ", "_"), "");
        }

    }

}
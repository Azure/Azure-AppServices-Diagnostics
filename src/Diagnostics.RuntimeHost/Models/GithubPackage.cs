using System.Collections.Generic;
using Octokit;

namespace Diagnostics.RuntimeHost.Models
{
    public class GithubPackage : Package
    {
        public override string Id { get; set; }
        public override string CommittedByAlias { get; set; }

        private IEnumerable<CommitContent> _commits;

        public GithubPackage(string id, string fileExtension, string content)
        {
            Id = id;
            _commits = new List<CommitContent>
            {
                new CommitContent($"{id.ToLower()}/{id.ToLower()}.{fileExtension}", content)
            };
        }

        public GithubPackage(string id, string fileExtension, string content, EncodingType encoding)
        {
            Id = id;
            _commits = new List<CommitContent>
            {
                new CommitContent($"{id.ToLower()}/{id.ToLower()}.{fileExtension}", content, encoding)
            };
        }

        public GithubPackage(string id, string fileName, string fileExtension, string content)
        {
            Id = id;
            _commits = new List<CommitContent>
            {
                new CommitContent($"{id.ToLower()}/{fileName}.{fileExtension}", content)
            };
        }

        public GithubPackage(string id, string fileName, string fileExtension, string content, EncodingType encoding)
        {
            Id = id;
            _commits = new List<CommitContent>
            {
                new CommitContent($"{id.ToLower()}/{fileName}.{fileExtension}", content, encoding)
            };
        }

        public override IEnumerable<CommitContent> GetCommitContents()
        {
            return _commits;
        }
    }
}

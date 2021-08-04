using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Services.DevOpsClient
{
    class PRBody
    {
        public string sourceRefName { get; set; }
        public string targetRefName { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public IList<reviewer> reviewers { get; set; }
    }

    class reviewer
    {
        public string id { get; set; }

        public reviewer(string id)
        {
            this.id = id;
        }
    }

    class PushBody
    {
        public IList<RefUpdates> refUpdates { get; set; }
        public IList<Commits> commits { get; set; }
    }

    class Commits
    {
        public string comment { get; set; }
        public IList<Changes> changes { get; set; }
    }

    class Changes
    {
        public string changeType { get; set; }
        public Item item { get; set; }
        public NewContent newContent { get; set; }
    }

    class NewContent
    {
        public string content { get; set; }
        public string contentType { get; set; }
    }

    class Item
    {
        public string path { get; set; }
    }

    class RefUpdates
    {
        public string name { get; set; }
        public string oldObjectId { get; set; }
    }

    class DevOpsRequestBodyAssistant : IRequestBodyAssistant
    {
        private string contentSerializer(string filePath)
        {
            return Convert.ToBase64String(File.ReadAllBytes(filePath));
        }

        /*private void checkChangeType*/
        public string generatePRRequestBody(string source, string target, string title, string description, IList<string> reviewers)
        {
            PRBody pr = new PRBody();

            pr.sourceRefName = source;
            pr.targetRefName = target;
            pr.title = title;
            pr.description = description;
            pr.reviewers = new List<reviewer>();
            foreach (string r in reviewers)
            {
                pr.reviewers.Add(new reviewer(r));
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(pr, options);
        }

        public string generatePushRequestBody(string name, string oldObjectId, string path, string content, string changeType, string comment)
        {
            RefUpdates ru = new RefUpdates();
            ru.name = name;
            ru.oldObjectId = oldObjectId;

            Item itm = new Item();
            itm.path = path;

            NewContent nc = new NewContent();
            nc.content = contentSerializer(content);
            nc.contentType = "base64encoded";

            Changes chgs = new Changes();
            chgs.changeType = changeType;
            chgs.item = itm;
            chgs.newContent = nc;

            Commits cmts = new Commits();
            cmts.comment = comment;
            cmts.changes = new List<Changes>() { chgs };

            PushBody push = new PushBody();
            push.refUpdates = new List<RefUpdates>() { ru };
            push.commits = new List<Commits>() { cmts };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(push, options);
        }
    }
}

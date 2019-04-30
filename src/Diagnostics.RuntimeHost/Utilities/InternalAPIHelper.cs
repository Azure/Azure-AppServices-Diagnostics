using Diagnostics.Logger;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.RuntimeHost.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    internal class InternalAPIHelper
    {
        public Dictionary<string, string> GetResourceParams(IResourceFilter gResourceFilter)
        {
            try
            {
                var resourceParams = new Dictionary<string, string>();
                resourceParams.Add("ResourceType", gResourceFilter.ResourceType.ToString());
                if (gResourceFilter.ResourceType.ToString() == "App")
                {
                    var appFilter = JsonConvert.DeserializeObject<AppFilter>(JsonConvert.SerializeObject(gResourceFilter));
                    AppType appType = appFilter.AppType;
                    var appTypesList = Enum.GetValues(typeof(AppType)).Cast<AppType>().Where(p => appType.HasFlag(p)).Select(x => Enum.GetName(typeof(AppType), x));
                    resourceParams.Add("AppType", String.Join(",", appTypesList));
                    PlatformType platformType = appFilter.PlatformType;
                    var platformTypesList = Enum.GetValues(typeof(PlatformType)).Cast<PlatformType>().Where(p => platformType.HasFlag(p)).Select(x => Enum.GetName(typeof(PlatformType), x));
                    resourceParams.Add("PlatformType", String.Join(",", platformTypesList));
                }
                return resourceParams;
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public IEnumerable<CommitContent> GetAllFilesInFolder(string folderpath)
        {
            string parentpath = Path.GetFileName(folderpath);
            List<CommitContent> files = new List<CommitContent>();
            foreach (string file in Directory.EnumerateFiles(folderpath, string.Empty))
            {
                 files.Add(new CommitContent(parentpath + "/" + Path.GetFileName(file), File.ReadAllText(file), Octokit.EncodingType.Utf8));
            }
            return files;
        }
    }
}

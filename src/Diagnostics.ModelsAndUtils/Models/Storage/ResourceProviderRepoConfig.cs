namespace Diagnostics.ModelsAndUtils.Models.Storage
{
    /// <summary>
    /// This class contains the fields part of Resource Provider Devops Repo Configuration.
    /// </summary>
    public class ResourceProviderRepoConfig
    {
        /// <summary>
        /// Name of the DevOps organization.
        /// </summary>
        public string Organization;

        /// <summary>
        /// Name of the DevOps project.
        /// </summary>
        public string Project;

        /// <summary>
        /// Name of the DevOps repository.
        /// </summary>
        public string Repository;

        /// <summary>
        /// Folder path if resource provider belongs to a common repo. Default value is "/"
        /// </summary>
        public string FolderPath;

        /// <summary>
        /// Resource Provider type, Example "Microsoft.Web/hostingEnvironments"
        /// </summary>
        public string ResourceProvider;

        /// <summary>
        /// Flag to indicate if Resource Provider wants to auto merge code to default branch without Pull Request review.
        /// </summary>
        public bool AutoMerge;
    }
}

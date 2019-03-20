using Diagnostics.ModelsAndUtils.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class AscInsight
    {
        /// <summary>
        /// Adds an ASC Insight that will be rendered as a regular insight. 
        /// It will flow automatically to Azure Support Center if your detector is enabled for a support topic.
        /// </summary>
        /// <param name="res">Response object for extension method</param>
        /// <param name="title">Title of insight. This should be a constant string, without any part of it changing depending on the resource.</param>
        /// <param name="status">Status of the insight. All insight status types are available, but None and Success with be changes to Info in Azure Support Center.</param>
        /// <param name="description">The description of the insight. This should not contain anything that is instructing the reader to do anything. 
        /// It should only expand on any relevant information about the insight.</param>
        /// <param name="recommendedAction">Recommended action that is specifically addressing what steps thesupport engineer should take.
        /// If you only want to have the support engineer to send the customer ready content, then that is all you would have to include here.</param>
        /// <param name="customerReadyContent">This is a response that is meant for the support engineer to paste directly into an email to the customer. 
        /// It is meant to completely solve the problem, and if that is not the case you may not want to use this field, by passing a null value. 
        /// Other good information to have is additional resources for the customer so that they can solve the problem in the future.</param>
        /// <param name="context">Operation context which is passed into detector run method.</param>
        /// <param name="ascOnly">Only show the insight in Azure Support Center.</param>
        /// <param name="extraNameValuePairs">Additional name value pairs that you want to display in Applens/App Service Diagnostics. These will not be added in Azure Support Center. 
        /// For markdown, wrap your text in markdown tags. HTML or plain text also allowed</param>
        /// <param name="isExpanded">Whether you want to Applens/App Service Diagnostics to expand the insight initially.</param>
        /// <returns>Azure Support Center Insight Object</returns>
        /// <example> 
        /// This sample shows how to use the <see cref="AddAscInsight"/> method.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///     var descriptionMarkdown = 
        ///         @"###A scale operation failed because there was already a scale operation underway. Here is the error:
        ///                    
        ///           ```
        ///           Operation failed because a current scale operation is ongoing.
        ///           ```";
        ///     
        ///     var recommendedActionPlainText = "Copy and paste the Customer Ready Content and send to the customer";
        ///     
        ///     var customerReadyActionMarkdown = 
        ///         @$"Your scale operation for site ***{cxt.Resource.Name}*** failed because there was a current scale operation underway. 
        ///                    
        ///            Please wait for the current operation to finish and then try again to scale your app service plan. ";
        /// 
        ///     res.AddAscInsight(
        ///         "Failed Scale Operation Detected",
        ///         InsightStatus.Critical,
        ///         new Text(descriptionMarkdown, true), 
        ///         new Text(recommendedActionPlainText),
        ///         new Text(customerReadyActionMarkdown, true),
        ///         cxt);
        /// }
        /// </code>
        /// </example>
        public static AzureSupportCenterInsight AddAscInsight<TResource>(this Response res, string title, 
            InsightStatus status, Text description, Text recommendedAction, Text customerReadyContent, 
            OperationContext<TResource> context, Dictionary<string,string> extraNameValuePairs = null, 
            bool ascOnly = false, bool isExpanded = false)
            where TResource : IResource
        {
            var insight = AzureSupportCenterInsightUtilites.CreateInsight(title, status, description, recommendedAction, customerReadyContent, res.Metadata, context);
            res.AscInsights.Add(insight);

            // If you designate an insight as ASC only, it will not show in applens
            // If this is an external call, it will not be added as an insight that renders in the UI
            if (ascOnly || !context.IsInternalCall)
            {
                return insight;
            }

            var nameValuePairs = new Dictionary<string, string>();

            if (description != null)
            {
                nameValuePairs.Add("Description", GetContentStringForApplens(description));
            }

            // Recommended Action is only for internal
            if (context.IsInternalCall && recommendedAction != null)
            {
                nameValuePairs.Add("Recommended Action", GetContentStringForApplens(recommendedAction));
            }

            // For consistency with ASC, we will convert Customer Ready Content to HTML on the server side, 
            // so that we ensure that it renders the same in both ASC and applens
            // For external requests, we will put this content as 'Recommended Action'
            if (customerReadyContent != null)
            {
                nameValuePairs.Add(context.IsInternalCall ? "Customer Ready Content" : "Recommended Action", CommonMark.CommonMarkConverter.Convert(customerReadyContent.Value));
            }

            if (extraNameValuePairs != null)
            {
                foreach (var key in extraNameValuePairs.Keys)
                {
                    nameValuePairs.Add(key, extraNameValuePairs[key]);
                }
            }

            res.AddInsight(new Insight(status, title, nameValuePairs, isExpanded));

            return insight;
        }

        private static string GetContentStringForApplens(Text content)
        {
            return content.IsMarkdown ? $"<markdown>{content.Value}</markdown>" : content.Value;
        }
    }
}

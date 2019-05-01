using System;
using System.Collections.Generic;
using System.Text;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using System.Data;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public static class ResponseChangeAnalysisExtension
    {
        /// <summary>
        /// Adds a list of changesets to response.
        /// </summary>
        /// <param name="response">Response object.</param>
        /// <param name="changeSets">List of changesets.</param>
        /// <example>
        /// This sample shows how to use <see cref="AddChangeSets(Response, List{ChangeSetResponseModel})"/> method to add list of changesets to response.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///      DateTime start = DateTime.Now.AddDays(-10);
        ///      var changesets = await dp.ChangeAnalysis.GetChangeSetsForResource(cxt.Resource.ResourceUri, start, DateTime.Now);
        ///      res.AddChangeSets(changesets);
        /// }
        /// </code>
        /// </example>
        public static DiagnosticData AddChangeSets(this Response response, List<ChangeSetResponseModel> changeSets)
        {
            if (changeSets == null || changeSets.Count <= 0)
            {
                return null;
            }

            DataTable results = new DataTable();
            results.TableName = "ChangeSets";
            results.Columns.Add(new DataColumn("ChangeSetId"));
            results.Columns.Add(new DataColumn("ResourceId"));
            results.Columns.Add(new DataColumn("Source"));
            results.Columns.Add(new DataColumn("TimeStamp"));
            results.Columns.Add(new DataColumn("TimeWindow"));
            results.Columns.Add(new DataColumn("InitiatedBy"));
            results.Columns.Add(new DataColumn("LastScanTime"));
            results.Columns.Add(new DataColumn("Inputs", typeof(List<ResourceChangesResponseModel>)));
            changeSets.ForEach(changeSet =>
            {
                results.Rows.Add(new object[]
                {
                    changeSet.ChangeSetId,
                    changeSet.ResourceId,
                    changeSet.Source,
                    changeSet.TimeStamp,
                    changeSet.TimeWindow,
                    changeSet.InitiatedBy
                });
            });

            var latestRow = results.Rows[0];
            latestRow[6] = changeSets[0].LastScanInformation != null ? changeSets[0].LastScanInformation.TimeStamp : string.Empty;
            latestRow[7] = changeSets[0].ResourceChanges ?? changeSets[0].ResourceChanges;
            var diagData = new DiagnosticData()
            {
                Table = results,
                RenderingProperties = new Rendering(RenderingType.ChangeSets)
                {
                    Title = string.Empty
                }
            };

            response.Dataset.Add(diagData);
            return diagData;
        }

        /// <summary>
        /// Adds Change Analysis Onboarding view.
        /// </summary>
        /// <param name="response">Response object.</param>
        /// <param name="onboardingText">Onboarding text given by author.</param>
        /// <example>
        /// This sample shows how to use <see cref="AddChangeSets(Response, List{ChangeSetResponseModel})"/> method to add list of changesets to response.
        /// <code>
        /// public async static Task<![CDATA[<Response>]]> Run(DataProviders dp, OperationContext cxt, Response res)
        /// {
        ///      res.AddOnboardingView("Enable change analysis");
        /// }
        /// </code>
        /// </example>
        /// <returns>Data with rendering type.</returns>
        public static DiagnosticData AddOnboardingView(this Response response, string onboardingText = null)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("OnboardingText", typeof(string)));
            table.Rows.Add(new object[] { onboardingText });

            var diagData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.ChangeAnalysisOnboarding)
            };

            response.Dataset.Add(diagData);
            return diagData;
        }
    }
}

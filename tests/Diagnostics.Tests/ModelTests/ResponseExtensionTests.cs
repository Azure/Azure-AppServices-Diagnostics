using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Diagnostics.ModelsAndUtils.Attributes;
using Diagnostics.ModelsAndUtils.Models;
using Diagnostics.ModelsAndUtils.Models.ChangeAnalysis;
using Diagnostics.ModelsAndUtils.Models.ResponseExtensions;
using Xunit;

namespace Diagnostics.Tests.ModelTests
{
    public class ResponseExtensionTests
    {
        [Fact]
        public void TestAddInsightsExtension()
        {
            Insight insight = new Insight(InsightStatus.Critical, "some test message");
            Response res = new Response();

            var returnedInsight = res.AddInsight(insight);

            Assert.NotEmpty(res.Dataset);

            var insightFromDataSet = res.Dataset.FirstOrDefault();

            Assert.Equal(returnedInsight, insightFromDataSet);

            Assert.Equal(RenderingType.Insights, insightFromDataSet.RenderingProperties.Type);
        }

        [Theory]
        [InlineData(true, InsightStatus.Critical)]
        [InlineData(true, InsightStatus.Warning)]
        [InlineData(true, InsightStatus.None)]
        [InlineData(false, InsightStatus.Critical)]
        public void TestCorrectDetectorStatus(bool addInsight, InsightStatus status)
        {
            Response res = new Response();

            if (addInsight)
            {
                Insight insight = new Insight(status, "some test message");
                var returnedInsight = res.AddInsight(insight);
            }

            res.UpdateDetectorStatusFromInsights();

            Assert.NotNull(res.Status);
            Assert.Equal(addInsight ? status : InsightStatus.None, res.Status.StatusId);
        }

        [Fact]
        public void TestAddEmailExtension()
        {
            string emailContent = "<b>Test</b>";
            Response res = new Response();

            res.AddEmail(emailContent);

            Assert.NotEmpty(res.Dataset);
            Assert.Equal<RenderingType>(RenderingType.Email, res.Dataset.FirstOrDefault().RenderingProperties.Type);

            foreach (DataRow row in res.Dataset.FirstOrDefault().Table.Rows)
            {
                Assert.Single(row.ItemArray);
            }
        }

        [Fact]
        public void TestAddDataSummaryExtension()
        {
            DataSummary ds1 = new DataSummary("Title1", "40");
            DataSummary ds2 = new DataSummary("Title2", "60");
            DataSummary ds3 = new DataSummary("Title3", "80");

            Response res = new Response();

            res.AddDataSummary(new List<DataSummary>() { ds1, ds2, ds3 });

            Assert.NotEmpty(res.Dataset);
            Assert.Equal<RenderingType>(RenderingType.DataSummary, res.Dataset.FirstOrDefault().RenderingProperties.Type);

            foreach (DataRow row in res.Dataset.FirstOrDefault().Table.Rows)
            {
                Assert.Equal(3, row.ItemArray.Count());
            }
        }

        [Fact]
        public void TestGraphOptions()
        {
            var diagnosticData = new DiagnosticData()
            {
                Table = null,
                RenderingProperties = new TimeSeriesPerInstanceRendering()
                {
                    GraphOptions = new
                    {
                        forceY = new int[] { 0, 100 },
                        yAxis = new
                        {
                            axisLabel = "This is a label"
                        }
                    }
                }
            };

            Assert.Equal(((TimeSeriesPerInstanceRendering)diagnosticData.RenderingProperties).GraphOptions.yAxis.axisLabel, "This is a label");
        }

        [Fact]
        public void TestAddDropdownToResponse()
        {
            Response apiResponse = new Response();
            string label = "test";
            List<Tuple<string, bool, Response>> data = new List<Tuple<string, bool, Response>>();

            var firstData = new Response();
            firstData.AddMarkdownView(@"some markdown content");
            data.Add(new Tuple<string, bool, Response>("firstKey", true, firstData));

            Dropdown dropdown = new Dropdown(label, data);

            apiResponse.AddDropdownView(dropdown);

            Assert.NotEmpty(apiResponse.Dataset);
            Assert.Equal<RenderingType>(RenderingType.DropDown, apiResponse.Dataset.FirstOrDefault().RenderingProperties.Type);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        public void AscInsightExtentionTest(bool ascOnly, bool isInternal)
        {
            OperationContext<App> operationContext = new OperationContext<App>(new App("sub", "rg", "site"), string.Empty, string.Empty, isInternal, string.Empty);
            Response apiResponse = new Response()
            {
                Metadata = new Definition()
                {
                    Id = "detectorid",
                    Name = "detector Name"
                },
            };

            var description = new Text("description", true);
            var recommendedAction = new Text("recommended Action");
            var customerReadyContent = new Text("*Customer Ready Content*", true);

            var customerReadyContentHtml = CommonMark.CommonMarkConverter.Convert(customerReadyContent.Value);

            apiResponse.AddAscInsight("Title", InsightStatus.Critical, description, recommendedAction, customerReadyContent, operationContext, ascOnly: ascOnly);

            var ascInsightAdded = apiResponse.AscInsights.FirstOrDefault();
            var nativeInsightAdded = apiResponse.Insights.FirstOrDefault();

            Assert.NotNull(ascInsightAdded);
            Assert.Equal(ascInsightAdded.Description, description);
            Assert.Equal(ascInsightAdded.RecommendedAction.Text, recommendedAction);
            Assert.Equal(ascInsightAdded.CustomerReadyContent.ArticleContent, customerReadyContentHtml);

            if (ascOnly || !isInternal)
            {
                Assert.Null(nativeInsightAdded);
            }
            else if (!ascOnly && isInternal)
            {
                Assert.NotNull(nativeInsightAdded);
                Assert.Equal(nativeInsightAdded.Body["Description"], $"<markdown>{description.Value}</markdown>");
                Assert.Equal(nativeInsightAdded.Body["Recommended Action"], recommendedAction.Value);
                Assert.Equal(nativeInsightAdded.Body["Customer Ready Content"], customerReadyContentHtml);
            }
            else if (!ascOnly && !isInternal)
            {
                Assert.NotNull(nativeInsightAdded);
                Assert.Equal(nativeInsightAdded.Body["Description"], $"<markdown>{description.Value}</markdown>");
                Assert.Equal(nativeInsightAdded.Body["Recommended Action"], customerReadyContentHtml);
                Assert.False(nativeInsightAdded.Body.ContainsKey("CustomerReadyContent"));
            }
        }

        [Fact]
        public void TestFormExtension()
        {
            Response res = new Response();
            Form myform = new Form(1);
            Textbox input1 = new Textbox(1, "Enter first input", true);
            Textbox input2 = new Textbox(1, "Enter second input", true);
            // Adding inputs with same ID throws exception
            Assert.Throws<Exception>(() => myform.AddFormInputs(new List<FormInputBase>() { input1, input2 }));
            input2 = new Textbox(2, "Enter second input");
            myform.AddFormInput(input2);
            Assert.NotEmpty(myform.FormInputs);
            Assert.Equal(2, myform.FormInputs.Count);
            res.AddForm(myform);
            Assert.NotEmpty(res.Dataset);
            Assert.Equal<RenderingType>(RenderingType.Form, res.Dataset.FirstOrDefault().RenderingProperties.Type);
        }

        [Fact]
        public void AddChangeSetsViewTest()
        {
            Response apiResponse = new Response();
            ChangeSetResponseModel sampleChangeSet = new ChangeSetResponseModel();
            sampleChangeSet.ChangeSetId = "XYZ";
            sampleChangeSet.ResourceId = "/sites/test-site";
            apiResponse.AddChangeSets(new List<ChangeSetResponseModel> { sampleChangeSet });
            Assert.NotEmpty(apiResponse.Dataset);
            Assert.Equal<RenderingType>(RenderingType.ChangeSets, apiResponse.Dataset.FirstOrDefault().RenderingProperties.Type);
        }

        [Fact]
        public void AddOnBoardingViewTest()
        {
            Response apiResponse = new Response();
            apiResponse.AddOnboardingView("Please enable change analysis");
            Assert.NotEmpty(apiResponse.Dataset);
            Assert.Equal<RenderingType>(RenderingType.ChangeAnalysisOnboarding, apiResponse.Dataset.FirstOrDefault().RenderingProperties.Type);
        }
    }
}

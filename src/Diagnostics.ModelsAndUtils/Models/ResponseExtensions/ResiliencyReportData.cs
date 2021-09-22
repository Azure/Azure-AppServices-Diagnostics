using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public class ResiliencyReportData : IResiliencyReportData
    {
        private string _customerName = string.Empty;
        [JsonConverter(typeof(ResiliencyResource))]
        ResiliencyResource[] _resiliencyResourceList;

        /// <summary>
        /// Constructor. Creates an instance of ResiliencyReportData
        /// </summary>
        /// <param name="customername">Customer's name used for the report's cover. This will normally be either customer's Company or simply Customer's name obtained from the subscription.</param>
        /// <param name="resiliencyResourceList">Array containing the list of resources being evaluated of type ResiliencyResource.</param>        
        public ResiliencyReportData(string customerName, ResiliencyResource[] resiliencyResourceList)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentNullException(nameof(customerName), $"{nameof(customerName)} cannot be null or empty");
            }
            else
            {
                this.CustomerName = customerName;
            }
            if (resiliencyResourceList == null || resiliencyResourceList.Length == 0)
            {
                throw new ArgumentNullException(nameof(resiliencyResourceList), $"{nameof(resiliencyResourceList)} cannot be null or empty");
            }
            else
            {
                this._resiliencyResourceList = resiliencyResourceList;
            }
        }
        /// <summary>
        /// Customer's name used for the report's cover. This will normally be either customer's Company or simply Customer's name obtained from the subscription.
        /// </summary>
        public string CustomerName
        {
            get
            {
                return this.CustomerName;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(CustomerName), $"{nameof(CustomerName)} cannot be null or empty");
                }
                else
                {
                    this.CustomerName = value;
                }
            }
        }

        /// <summary>
        /// Array containing a list of all the resources to be included in the report along with the individual features checked and their scores.
        /// </summary>
        public ResiliencyResource[] GetResiliencyResourceList()
        {
            return (ResiliencyResource[])this._resiliencyResourceList.Clone();
        }

        public void SetResiliencyResourceList(ResiliencyResource[] value)
        {
            if (value == null || value.Length == 0)
            {
                throw new ArgumentNullException(nameof(value), $"{nameof(value)} cannot be null or empty");
            }
            else
            {
                this._resiliencyResourceList = value;
            }

        }


    }

    /// <summary>
    /// ResiliencyResources supported can be Web App or ASE
    /// </summary>
    public class ResiliencyResource : IResiliencyResource
    {        
        double _overallScore = 0;
        ResiliencyFeature[] resiliencyFeaturesList;
        IDictionary<string, Weight> _featuresDictionary;

        /// <summary>
        /// Constructor - Creates an instance of the ResiliencyResource class
        /// </summary>
        /// <param name="name">Name of the Resource (Web App name for example) being evaluated.</param>
        /// <param name="featuresDictionary"> Key pair values containing the name of the features evaluated and their Weight (Enum representing the Feature Weight).</param>        
        public ResiliencyResource(string name, IDictionary<string, Weight> featuresDictionary)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), $"{nameof(name)} cannot be null or empty");
            }
            else
            {
                this.Name = name;
            }

            if (featuresDictionary == null || featuresDictionary.Count == 0)
            {
                throw new ArgumentNullException(nameof(featuresDictionary), $"{nameof(featuresDictionary)} cannot be null or empty");
            }
            else
            {
                _featuresDictionary = featuresDictionary;
                this.resiliencyFeaturesList = new ResiliencyFeature[_featuresDictionary.Count];
                ResiliencyFeature[] resiliencyFeaturesList = new ResiliencyFeature[_featuresDictionary.Count];

                var i = 0;
                foreach (KeyValuePair<string, Weight> kvp in _featuresDictionary)
                {
                    ResiliencyFeature feature = new ResiliencyFeature(kvp.Key, kvp.Value);
                    resiliencyFeaturesList[i] = feature;
                    i++;
                }
                this.resiliencyFeaturesList = resiliencyFeaturesList;
            }
        }

        /// <summary>
        /// Name of the resource
        /// </summary>
        public string Name { get; set; }

        public ResiliencyFeature[] GetResiliencyFeaturesList()
        {
            return (ResiliencyFeature[])this.resiliencyFeaturesList.Clone();
        }

        public void SetResiliencyFeaturesList(ResiliencyFeature[] value)
        {
            this.resiliencyFeaturesList = value;
        }

        /// <summary>
        /// Overall Score for the Resource is calculated based on the grade of each feature.
        /// </summary>
        public double OverallScore
        {
            get
            {
                _overallScore = 0;
                double _overallScoreSum = 0;
                double _featureWeightsSum = 0;
                foreach (ResiliencyFeature rf in resiliencyFeaturesList)
                {
                    Weight _weight;
                    Grade _grade;
                    bool weightParsingResult = Enum.TryParse<Weight>(rf.FeatureWeight.ToString(), out _weight);
                    bool gradeParsingResult = Enum.TryParse<Grade>(rf.ImplementationGrade.ToString(), out _grade);
                    if (weightParsingResult && gradeParsingResult)
                    {
                        _overallScoreSum += (int)rf.FeatureWeight * (int)rf.ImplementationGrade;
                        _featureWeightsSum += (int)rf.FeatureWeight;
                    }
                }
                _overallScore = Math.Round((_overallScoreSum * 100) / (_featureWeightsSum * 2), 1);
                return Math.Round(_overallScore, 2);
            }


        }
    }
    /// <summary>
    /// ResiliencyFeature is the object that will contain the data for the check done on a particular detector
    /// </summary>
    public class ResiliencyFeature : IResiliencyFeature
    {
        /// <summary>
        /// Creates an instance of ResiliencyFeature class.
        /// </summary>
        /// <param name="name">Name of the Resiliency Feature evaluated.</param>
        /// <param name="featureWeight">Enum representing the Feature Weight.</param>
        string _name = string.Empty;


        public ResiliencyFeature(string name, Weight featureWeight)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), $"{nameof(name)} cannot be null or empty");
            }
            else
            {
                this.Name = name;
            }
            this.FeatureWeight = featureWeight;
            ImplementationGrade = 0;
            GradeComments = "";
            SolutionComments = "";
        }

        /// <summary>
        /// Name of the Resiliency Feature evaluated.
        /// </summary>
        public string Name
        {
            get { return this.Name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException(nameof(value), $"{nameof(value)} cannot be null or empty");
                }
                else
                {
                    this.Name = value;
                }
            }
        }

        /// <summary>
        /// Feature Weight explanation:
        /// Mandatory: 25 - Without this feature implemented, the app will most likely have availability issues.
        /// Important (ASE): 15 - Allows for reliability in special situations (for example Multiple zones/Regions, Regional pairing, etc.)
        /// Important: 5  - Allows for reliability in special situations (for example Multiple zones/Regions, Regional pairing, etc.)
        /// GoodToHave: 1
        /// Notcalculated): 0
        /// </summary>
        public Weight FeatureWeight { get; set; }

        /// <summary>
        /// Enum representing the Grade obtained in this feature:
        /// 
        /// </summary>
        public Grade ImplementationGrade { get; set; }

        /// <summary>
        /// Comments explaining the grade obtained by this site.
        /// </summary>
        public string GradeComments { get; set; }

        /// <summary>
        /// Comments to be included with the solution in case of a failing grade
        /// </summary>
        public string SolutionComments { get; set; }
    }

    /// <summary>
    /// Extension used by the Resiliency Report to generate the response with the data used to generate the PDF report
    /// </summary>
    public static class ResponseResiliencyReportExtension
    {
        /// <summary>
        /// It adds the serialized ResiliencyReportData object into the response table.
        /// </summary>
        public static DiagnosticData AddResiliencyReportData(this Response response, ResiliencyReportData resiliencyReportData)
        {
            if (response == null || resiliencyReportData == null)
            {
                return null;
            }
            string jsonResiliencyReport = JsonConvert.SerializeObject(resiliencyReportData, Formatting.Indented);
            string jsonResiliencyResourceList = JsonConvert.SerializeObject(resiliencyReportData.GetResiliencyResourceList(), Formatting.Indented);
            string jsonResiliencyFeaturesList = "";
            for (int siteNum = 0; siteNum < resiliencyReportData.GetResiliencyResourceList().GetLength(0); siteNum++)
            {
                jsonResiliencyFeaturesList = jsonResiliencyFeaturesList + JsonConvert.SerializeObject(resiliencyReportData.GetResiliencyResourceList()[siteNum].GetResiliencyFeaturesList(), Formatting.Indented);

            }
            var table = new DataTable();
            table.Columns.Add(new DataColumn("ResiliencyReport", typeof(string)));
            table.Columns.Add(new DataColumn("ResiliencyResourceList", typeof(string)));
            table.Columns.Add(new DataColumn("ResiliencyFeaturesList", typeof(string)));
            table.Rows.Add(new object[] { jsonResiliencyReport, jsonResiliencyResourceList, jsonResiliencyFeaturesList });
            var diagnosticData = new DiagnosticData()
            {
                Table = table,
                RenderingProperties = new Rendering(RenderingType.Report)
            };
            response.Dataset.Add(diagnosticData);
            return diagnosticData;
        }
    }
}

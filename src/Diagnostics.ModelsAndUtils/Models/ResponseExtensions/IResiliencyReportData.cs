﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models.ResponseExtensions
{
    public interface IResiliencyReportData
    {
        /// <summary>
        /// Customer's name used for the report's cover. This will normally be either customer's Company or simply Customer's name obtained from the subscription.
        /// </summary>
        string CustomerName { get; set; }

        /// <summary>
        /// Array containing a list of all the resources to be included in the report along with their 
        /// </summary>
        ResiliencyResource[] GetResiliencyResourceList();

        /// <summary>
        /// Array containing a list of all the resources to be included in the report along with their 
        /// </summary>
        void SetResiliencyResourceList(ResiliencyResource[] value);

        //      public JObject JSONObject();        
    }

    /// <summary>
    /// ResiliencyResources supported can be Web App or ASE
    /// </summary>
    public interface IResiliencyResource
    {
        /// <summary>
        /// Resource name
        /// </summary>

        string Name { get; set; }
        /// <summary>
        /// Overall Score for the Resource is calculated based on the grade of each feature
        /// </summary>
        double CalculateOverallScore();

        double OverallScore { get; }


        /// <summary>
        /// List of Resiliency Features
        /// </summary>
        ResiliencyFeature[] GetResiliencyFeaturesList();


        /// <summary>
        /// List of Resiliency Features
        /// </summary>
        void SetResiliencyFeaturesList(ResiliencyFeature[] value);
    }

    /// <summary>
    /// ResiliencyFeature is the object that will contain the data for the check done on a particular detector
    /// </summary>
    public interface IResiliencyFeature
    {
        /// <summary>
        /// Name of the Resiliency Feature evaluated. Used as the identifier.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Feature Weight explanation:
        /// Mandatory: 25 - Without this feature implemented, the app will most likely have availability issues.
        /// Important: 5  - Allows for reliability in special situations (for example Multiple zones/Regions, Regional pairing, etc.)
        /// GoodToHave: 1
        /// Notcalculated): 0
        /// </summary>
        Weight FeatureWeight { get; set; }

        /// <summary>
        /// Grade obtained while checking for this feature:
        /// Fully Implemented: 2
        /// Partially Implemented/Couldn't detect: 1 -- If we couldn't confirm whether the feature was implemented (due to error), we'll give the benefit of the doubt.
        /// Not Implemented: 0
        /// </summary>
        Grade ImplementationGrade { get; set; }

        /// <summary>
        /// Comments explaining the grade obtained by this site.
        /// </summary>
        string GradeComments { get; set; }

        /// <summary>
        /// Comments to be included with the solution in case of a failing grade
        /// </summary>
        string SolutionComments { get; set; }
    }

    public enum Weight
    {
        NotCounted = 0,
        GoodToHave = 1,
        Important = 5,
        Mandatory = 25,
    }

    public enum Grade
    {
        Implemented = 2,
        PartiallyImplemented = 1,
        NotImplemented = 0
    }

}

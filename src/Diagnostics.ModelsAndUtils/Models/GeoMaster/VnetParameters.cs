using System;
using System.Collections.Generic;
using System.Text;

namespace Diagnostics.ModelsAndUtils.Models
{
    public class VnetParameters
    {
        /// <summary>
        /// Required inputs to validate a VNET
        /// </summary>
        public VnetParameters()
        {

        }

        /// <summary>
        /// The Resource Group of the VNET to be validated
        /// </summary>
        public string VnetResourceGroup { get; set; }

        /// <summary>
        /// The name of the VNET to be validated
        /// </summary>
        public string VnetName { get; set; }

        /// <summary>
        /// The subnet name to be validated
        /// </summary>
        public string VnetSubnetName { get; set; }
    }
}

using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
namespace Diagnostics.RuntimeHost.Security.CertificateAuth
{
    public class CertificateAuthOptions: AuthenticationSchemeOptions
    {
        public CertificateAuthOptions()
        {

        }
        public new CertificateAuthEvents Events
        {
            get
            {
                return (CertificateAuthEvents)base.Events;
            }
            set
            {
                base.Events = value;
            }
        }

        /// <summary>
        /// List of Allowed Cert Subject Names.
        /// </summary>
        public  List<string> AllowedSubjectNames  { get; set; }

        /// <summary>
        /// List of Allowed Cert Issuers.
        /// </summary>
        public List<string> AllowedIssuers { get; set; }
    }
}

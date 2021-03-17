using Diagnostics.DataProviders;
using Diagnostics.Logger;
using Diagnostics.RuntimeHost.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Diagnostics.RuntimeHost.Services
{
    public interface IAutoHealMonitoringService
    {
    }

    /// <summary>
    /// This service is responsible for logging auto heal settings at periodic intervals.
    /// </summary>
    public class AuoHealMonitoringService : IAutoHealMonitoringService
    {
        private readonly bool isEnabled;
        private readonly int iterationDelayInSeconds;
        private readonly string appHostConfigFilePath;

        /// <summary>
        /// Creates a new instance of AutoHealMonitoringService
        /// </summary>
        /// <param name="configuration">Configuration object</param>
        /// <param name="environment">Environment object</param>
        public AuoHealMonitoringService(IConfiguration configuration, IHostingEnvironment environment)
        {
            isEnabled = configuration.GetValue("AutoHealMonitoringSettings:IsEnabled", true);
            iterationDelayInSeconds = configuration.GetValue("AutoHealMonitoringSettings:IterationDelayInSeconds", 30 * 60);
            appHostConfigFilePath = Environment.GetEnvironmentVariable("APP_POOL_CONFIG");

            if (!environment.IsDevelopment() && isEnabled)
            {
                DiagnosticsETWProvider.Instance.LogRuntimeHostMessage("Starting AutoHealSettings Log monitoring");
                LogAutoHealSettings();
            }
        }

        private async Task LogAutoHealSettings()
        {
            while (true)
            {
                try
                {
                    string appHostConfig = await File.ReadAllTextAsync(appHostConfigFilePath);
                    XDocument xDoc = XDocument.Parse(appHostConfig);
                    var configuration = xDoc.Element("configuration");
                    var monitoringSection = configuration.Descendants("monitoring").FirstOrDefault();

                    bool isAutoHealEnabled = IsAutoHealEnabled(monitoringSection);
                    string autohealSettings = monitoringSection == default(XElement) ? "None" : CredentialTrapper.Obfuscate(monitoringSection.ToString());
                    DiagnosticsETWProvider.Instance.LogMonitoringEventMessage("AutoHealSettings", $"Enabled : {isAutoHealEnabled}, Settings : {autohealSettings}");
                }
                catch (Exception ex)
                {
                    DiagnosticsETWProvider.Instance.LogMonitoringEventException("AutoHealSettings", "Unable to fetch auto-heal settings", ex.GetType().ToString(), ex.ToString());
                }
                finally
                {
                    await Task.Delay(iterationDelayInSeconds * 1000);
                }
            }
        }

        /// <summary>
        /// Checks the monitoring section and returns true if auto-heal is enabled.
        /// </summary>
        /// <param name="monitoringSection">Monitoring section</param>
        /// <returns>True, if auto-heal is enabled</returns>
        private bool IsAutoHealEnabled(XElement monitoringSection)
        {
            if (monitoringSection == default(XElement) || !monitoringSection.HasElements)
            {
                return false;
            }

            var triggers = monitoringSection.Elements("triggers").FirstOrDefault();
            var actions = monitoringSection.Elements("actions").FirstOrDefault();

            return
                triggers != default(XElement) && triggers.HasElements &&
                actions != default(XElement) && !string.IsNullOrWhiteSpace(GetAttributeValue(actions, "value"));
        }

        /// <summary>
        /// Checks if Attribute exists on XElement and returns its value
        /// If the Attribute doesn't exists, returns Null
        /// </summary>
        /// <param name="element">XElement</param>
        /// <param name="attributeName">Name of the attribute</param>
        /// <returns>Attribute value</returns>
        private string GetAttributeValue(XElement element, string attributeName)
        {
            if (element == default(XElement) || string.IsNullOrWhiteSpace(attributeName) || element.Attribute(attributeName) == null)
            {
                return null;
            }

            return element.Attribute(attributeName).Value.Trim();
        }
    }
}

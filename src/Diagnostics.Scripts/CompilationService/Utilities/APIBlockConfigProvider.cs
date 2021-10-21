using Diagnostics.Scripts.CompilationService.Models;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Diagnostics.ModelsAndUtils.Models;

namespace Diagnostics.Scripts.CompilationService.Utilities
{    
    public static class APIBlockConfigProvider
    {
        private const string CONFIG_LOCATION_APP_SETTING = "APPSETTINGS:APIBLOCKCONFIG:LOCATION";
        private static BlockConfig _blockConfig = null;
        private static object _lock = new object();
        public static BlockConfig GetConfig {
            get
            {
                if (_blockConfig == null)
                {
                    lock (_lock)
                    {
                        if (_blockConfig == null && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CONFIG_LOCATION_APP_SETTING)))
                        {
                            try
                            {
                                string fileContent = System.IO.File.ReadAllText(Environment.GetEnvironmentVariable(CONFIG_LOCATION_APP_SETTING));
                                _blockConfig = JsonConvert.DeserializeObject<BlockConfig>(fileContent);
                            }
                            catch (Exception)
                            {
                                //Consume the exception and continue
                            }
                        }
                    }
                }

                return _blockConfig;
            }
        }
    }
}

using Diagnostics.ModelsAndUtils.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Diagnostics.Scripts.CompilationService.Models
{    
    public class EntityBlockConfig
    {
        private const double TIME_IN_MILISECONDS_FOR_EACH_REGEX_MATCH = 500;
        private const string DEFAULT_REGEX_PATTERN_TO_MATCH = ".*";
        private string _blockMatchingRegEx = string.Empty;
        public string BlockMatchingRegExPattern
        {
            get
            {
                return _blockMatchingRegEx;
            }
            set
            {                
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        this.BlockingRegEx = new Regex(value, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(TIME_IN_MILISECONDS_FOR_EACH_REGEX_MATCH));
                        _blockMatchingRegEx = value;
                    }
                    catch (Exception)
                    {
                        //Consume the exception and continue
                    }
                    
                }
            }
        }
        public Regex BlockingRegEx { get; set; } = new Regex(DEFAULT_REGEX_PATTERN_TO_MATCH, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(TIME_IN_MILISECONDS_FOR_EACH_REGEX_MATCH));
        public string MessageToShowWhenBlocked { get; set; } = string.Empty;
    }

    public class ContainerBlockConfig : EntityBlockConfig
    {
        public List<EntityBlockConfig> MethodsToBlock { get; set; } = new List<EntityBlockConfig>();
    }
    public class ClassBlockConfig: ContainerBlockConfig
    {
        public bool IsObjectCreationBlocked { get; set; } = false;
        public List<EntityBlockConfig> PropertiesToBlock { get; set; } = new List<EntityBlockConfig>();
    }
    public class BlockConfig
    {
        public List<ContainerBlockConfig> pInvokeBlockConfig { get; set; }
        public List<ClassBlockConfig> classBlockConfig { get; set; }

        public static List<string> GetMatchingBlockMessageList(List<EntityBlockConfig> config, string stringToMatch)
            => config?.Where(eConf => eConf?.BlockingRegEx?.IsMatch(stringToMatch) == true).Select(s => s.MessageToShowWhenBlocked).ToList<string>();

        public List<ContainerBlockConfig> GetMatchingPInvokeConfig(string dllName) => (pInvokeBlockConfig != null) ? pInvokeBlockConfig.Where(pConfig => pConfig?.BlockingRegEx?.IsMatch(dllName) == true).ToList<ContainerBlockConfig>() : null;

        public bool MatchesPInvokeToBlock(string dllName) => GetMatchingPInvokeConfig(dllName)?.Any() == true;

        public List<ClassBlockConfig> GetMatchingClassConfig(string classTypeName) => (classBlockConfig != null) ? classBlockConfig.Where(cConfig => cConfig?.BlockingRegEx?.IsMatch(classTypeName) == true).ToList<ClassBlockConfig>() : null;

        public bool MatchesClassToBlock(string classTypeName) => GetMatchingClassConfig(classTypeName)?.Any() == true;
    }
}

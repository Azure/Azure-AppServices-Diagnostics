using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Models
{
    internal class SourceWatcherException : Exception
    {
        /// <summary>
        /// Create new instance of SourceWatcherException with the given source and message
        /// </summary>
        /// <param name="sourceName">Name of source watcher service</param>
        /// <param name="message"></param>
        public SourceWatcherException(string sourceName, string message) : base($"Exception occurred in {sourceName}Watcher service. {message}")
        {
        }

        /// <summary>
        /// Create new instance of SourceWatcherException with the given source, message, and inner exception
        /// </summary>
        /// <param name="sourceName">Name of source watcher service</param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public SourceWatcherException(string sourceName, string message, Exception innerException) : base($"Exception occurred in {sourceName}Watcher service. {message}", innerException)
        {
        }
    }
}

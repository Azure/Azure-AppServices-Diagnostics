﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Diagnostics.RuntimeHost.Utilities
{
    internal class FileHelper
    {
        internal static Task<string> GetFileContentAsync(string dir, string fileName)
        {
            string filePath = Path.Combine(dir, fileName);
            return GetFileContentAsync(filePath);
        }

        internal static async Task<string> GetFileContentAsync(string filePath)
        {
            string fileContent = string.Empty;
            if (File.Exists(filePath))
            {
                fileContent = await File.ReadAllTextAsync(filePath);
            }

            return fileContent;
        }

        internal static Task WriteToFileAsync(string dir, string fileName, string content)
        {
            return WriteToFileAsync(Path.Combine(dir, fileName), content);
        }

        internal static Task WriteToFileAsync(string filePath, string content)
        {
            return File.WriteAllTextAsync(filePath, content);
        }

        internal static void DeleteFileIfExists(string dir, string fileName)
        {
            string filePath = Path.Combine(dir, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        internal static async Task CopyContentsAsync(DirectoryInfo srcDirInfo, DirectoryInfo destinationDirInfo, string customNameForDllFile = null)
        {
            foreach (var fileInfo in srcDirInfo.GetFiles())
            {
                string desiredFileName = fileInfo.Name;
                
                if (IsDllorPdb(fileInfo) && !string.IsNullOrWhiteSpace(customNameForDllFile))
                {
                    desiredFileName = $"{customNameForDllFile}{fileInfo.Extension}";
                }

                using (FileStream SourceStream = File.Open(fileInfo.FullName, FileMode.Open))
                {
                    using (FileStream DestinationStream = File.Create(Path.Combine(destinationDirInfo.FullName, desiredFileName)))
                    {
                        await SourceStream.CopyToAsync(DestinationStream);
                    }
                }
            }
        }

        internal static bool IsDllorPdb(FileInfo fileInfo)
        {
            return !string.IsNullOrWhiteSpace(fileInfo.Extension)
                && ((fileInfo.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) 
                    || fileInfo.Extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase)));
        }
    }
}

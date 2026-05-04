
using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    public static class IO
    {
        /// <summary>
        /// Generates a GUID verifying if any file exists using that ID
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string GenerateGuidForFilename(string directoryPath, string extension)
        {
            string guid;

            do
            {
                guid = System.Guid.NewGuid().ToString();
            }
            while (File.Exists($"{directoryPath}/{guid}{extension}"));

            return guid;
        }


        /// <summary>
        /// Come on... this one is pretty self explanatory.
        /// </summary>
        /// <param name="path"></param>
        public static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void Unzip(byte[] zipFileData, string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                throw new ArgumentException("Target path does not exist.");
            }

            using (MemoryStream zipMemoryStream = new(zipFileData))
            {
                Debug.Log("Unzipping...");
                using (ZipArchive archive = new(zipMemoryStream, ZipArchiveMode.Read))
                {
                    Debug.Log("Unzipping... 2");
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fullOutputPath = Path.Combine(targetPath, entry.FullName);

                        // Create the directory if it doesn't exist
                        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));

                        // Extract the file
                        entry.ExtractToFile(fullOutputPath, true); // Overwrite existing files
                    }
                }
            }
        }
    }
}
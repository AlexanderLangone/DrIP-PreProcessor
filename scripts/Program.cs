using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string depositDirectory = Path.Combine("..", "..", "..", "..", "..", "..", "docs_deposit");
        string preProcessDirectory = Path.Combine("..", "..", "..", "..", "..", "..", "docs_pre");
        string indexDirectory = Path.Combine("..", "..", "..", "..", "..", "..", "index_files");

        // Ensure the necessary directories exist - create them if needed
        Directory.CreateDirectory(preProcessDirectory);
        Directory.CreateDirectory(indexDirectory);

        try
        {
            // Read configuration from config.ini
            string configFilePath = Path.Combine("..", "..", "..", "..", "..", "config.ini");
            var config = ReadConfig(configFilePath);

            if (config == null)
            {
                throw new FileNotFoundException("Configuration file not found: " + configFilePath);
            }

            // Get file extensions and corresponding codes from configuration
            var fileExtensions = config["FileExtensions"];
            string keywordSeparator = config.ContainsKey("Settings") && config["Settings"].ContainsKey("FILENAME_KEYWORD_SEPARATOR_CHARACTER")
                ? config["Settings"]["FILENAME_KEYWORD_SEPARATOR_CHARACTER"]
                : "_"; // Default to "_" if not specified

            // Get prefix for output file name
            string filenamePrefix = config.ContainsKey("Settings") && config["Settings"].ContainsKey("FILENAME_PREFIX")
                ? config["Settings"]["FILENAME_PREFIX"]
                : "data"; // Default to "data" if not specified

            // Get all files in the deposit directory
            var files = Directory.EnumerateFiles(depositDirectory, "*.*")
                                 .Where(s => IsSupportedFile(s, fileExtensions) && FileIsOldEnough(s));

            foreach (var filePath in files)
            {
                try
                {
                    // Move the file to the pre-process directory
                    string preProcessFilePath = Path.Combine(preProcessDirectory, Path.GetFileName(filePath));
                    File.Move(filePath, preProcessFilePath);

                    // Process each file and get the output data
                    string outputData = ProcessFile(preProcessFilePath, fileExtensions, keywordSeparator);

                    // Create the output file name based on the input file name
                    string inputFileName = Path.GetFileNameWithoutExtension(preProcessFilePath);
                    string currentDate = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-sstt");
                    string outputFileName = $"{filenamePrefix}{keywordSeparator}{inputFileName}{keywordSeparator}{currentDate}.txt"; // Use configured prefix, separator, and date with time
                    string outputFilePath = Path.Combine(indexDirectory, outputFileName);

                    // Write the output data to the output file
                    File.WriteAllText(outputFilePath, outputData);

                    Console.WriteLine("Processing complete. Output written to " + outputFilePath);
                }
                catch (Exception ex)
                {
                    // Log the error for the specific file
                    Console.WriteLine($"Error processing file '{filePath}': {ex.Message}");
                }
            }

            Console.WriteLine("All files processed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    static bool IsSupportedFile(string filePath, Dictionary<string, string> fileExtensions)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return fileExtensions.ContainsKey(extension);
    }

    static bool FileIsOldEnough(string filePath)
    {
        var lastWriteTime = File.GetLastWriteTime(filePath);
        return (DateTime.Now - lastWriteTime).TotalMinutes >= 1;
    }

    static string ProcessFile(string filePath, Dictionary<string, string> fileExtensions, string keywordSeparator)
    {
        // Get the file name without the directory path
        string fileName = Path.GetFileName(filePath);

        // Split the file name into keywords using the specified separator
        string[] keyword = fileName.Split(new string[] { keywordSeparator }, StringSplitOptions.None);

        // Ensure there are exactly 4 keywords (Facility#, CensusData, PreparerUserID, PagesCount)
        if (keyword.Length != 4)
        {
            throw new FormatException("Invalid file name format: " + fileName);
        }

        // Remove the file extension from the PagesCount keyword
        string pagesCount = Path.GetFileNameWithoutExtension(keyword[3]);

        // Get file format based on file extension
        string fileExtension = Path.GetExtension(fileName).ToLower();
        string fileFormat = fileExtensions[fileExtension];

        // Get the full file path
        string fullFilePath = Path.GetFullPath(filePath);

        // Construct the output line
        string outputLine = $"{keyword[0]}|{keyword[1]}|{keyword[2]}|{pagesCount}|{fullFilePath}|{fileFormat}";

        return outputLine;
    }

    static Dictionary<string, Dictionary<string, string>>? ReadConfig(string filePath)
    {
        try
        {
            var config = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";

            // Read all lines from the config file
            string[] lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                // Ignore comments and empty lines
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // Check if line starts with '[' and ends with ']'
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    // Extract section name
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    config[currentSection] = new Dictionary<string, string>();
                }
                else if (currentSection != "")
                {
                    // Split key-value pairs by '='
                    int index = trimmedLine.IndexOf('=');
                    if (index > 0)
                    {
                        string key = trimmedLine.Substring(0, index).Trim();
                        string value = trimmedLine.Substring(index + 1).Trim();

                        // Add key-value pair to the current section
                        config[currentSection][key] = value;
                    }
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading config file: " + ex.Message);
            return null;
        }
    }
}



//COMPLIE AND RUN - make sure in correct directory
// dotnet build --configuration Release
    //this creates the executable that once ran will run

// for servers with no dotNet framework installed(where you build it needs dotnet):
    // dotnet publish --configuration Release --self-contained true --runtime win-x64
    // or depending on RID - ex. win-x64, linux-x64, osx-x64, etc. 


// to run normally:
    //dotnet build
    //dotnet run
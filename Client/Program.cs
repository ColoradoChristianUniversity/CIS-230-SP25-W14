using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Generator;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Image Organizer";

            // Create an Image instance to use throughout
            var imageGenerator = new Image();
            DisplayWelcome(imageGenerator);

            if (!ConfirmStart())
            {
                Console.WriteLine("Operation cancelled. Press any key to exit.");
                Console.ReadKey();
                return;
            }

            try
            {
                ProcessImages(imageGenerator);
                OpenFileExplorer(imageGenerator);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        static void DisplayWelcome(Image imageGenerator)
        {
            Console.Clear();
            Console.WriteLine("=================================");
            Console.WriteLine("      Image Organizer Utility");
            Console.WriteLine("=================================");
            Console.WriteLine($"Root Folder: {imageGenerator.RootFolderPath}");
            Console.WriteLine();
        }

        static bool ConfirmStart()
        {
            Console.Write("Start image organization? [y/n]: ");
            string input = Console.ReadLine()?.Trim().ToLower() ?? "";
            return input == "y";
        }

        static void ProcessImages(Image imageGenerator)
        {
            // Generate images (use 100 for development, 1000 for final)
            Console.WriteLine("Generating images...");
            imageGenerator.Generate(1000); // Use 100 for faster testing during development

            // Get all image files
            string[] files = Directory.GetFiles(imageGenerator.RootFolderPath, "*.png");
            if (files.Length == 0)
            {
                Console.WriteLine("No images found to organize.");
                return;
            }

            // Start timing
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Initialize progress bar
            int totalFiles = files.Length;
            int processedFiles = 0;
            DisplayProgressBar(processedFiles, totalFiles);

            foreach (string file in files)
            {
                try
                {
                    OrganizeFile(file, imageGenerator);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nWarning: Failed to organize {Path.GetFileName(file)}: {ex.Message}");
                }
                processedFiles++;
                DisplayProgressBar(processedFiles, totalFiles);
            }

            stopwatch.Stop();
            Console.WriteLine($"\nOperation completed in {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Organized {processedFiles} images");
        }

        static void OrganizeFile(string filePath, Image imageGenerator)
        {
            // Parse date from file name (e.g., "2024-01-12.png")
            string fileName = Path.GetFileNameWithoutExtension(filePath); // Gets "2024-01-12"
            if (!DateTime.TryParse(fileName, out DateTime creationDate))
            {
                throw new InvalidOperationException($"Invalid file name format: {fileName}");
            }

            // Verify with GetFileCreationUtc as a fallback
            DateOnly? creationDateOnly = Image.GetFileCreationUtc(filePath);
            if (creationDateOnly == null)
            {
                throw new FileNotFoundException($"File not found or inaccessible: {filePath}");
            }

            // Build target path: Root/Year/Month/filename
            string yearFolder = Path.Combine(imageGenerator.RootFolderPath, creationDate.Year.ToString("D4"));
            string monthFolder = Path.Combine(yearFolder, creationDate.Month.ToString("D2"));
            string fileNameWithExt = Path.GetFileName(filePath);
            string targetPath = Path.Combine(monthFolder, fileNameWithExt);

            // Create directories if they don't exist
            Directory.CreateDirectory(monthFolder);

            // Move file to target location
            File.Move(filePath, targetPath, overwrite: false);
        }

        static void DisplayProgressBar(int current, int total)
        {
            const int barWidth = 50;
            double progress = (double)current / total;
            int filled = (int)(barWidth * progress);

            Console.CursorLeft = 0;
            Console.Write("[");
            Console.Write(new string('#', filled));
            Console.Write(new string('-', barWidth - filled));
            Console.Write($"] {current}/{total} ({progress:P0})");
        }

        static void OpenFileExplorer(Image imageGenerator)
        {
            Console.WriteLine($"\nOpening File Explorer to {imageGenerator.RootFolderPath}");
            Process.Start("explorer.exe", imageGenerator.RootFolderPath);
        }
    }
}
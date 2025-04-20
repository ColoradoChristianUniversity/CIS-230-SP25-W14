using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Generator;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Welcome to the Image Organizer!");
        Console.WriteLine("This program will generate and organize 1,000 images.");
        Console.Write("Do you want to start? [y/n]: ");
        var input = Console.ReadLine();

        if (input?.ToLower() != "y")
        {
            Console.WriteLine("Operation canceled.");
            return;
        }

        
        var imageGenerator = new Image();
        Console.WriteLine("Generating images...");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        imageGenerator.Generate(1000);

      
        Console.WriteLine("Organizing images...");
        var rootFolder = imageGenerator.RootFolderPath;
        var files = Directory.GetFiles(rootFolder, "*.png");

        int totalFiles = files.Length;
        int processedFiles = 0;
        int delay = totalFiles > 0 ? (5000 / totalFiles) : 0;

        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('-');
            if (parts.Length < 3)
            {
                continue;
            }

            var year = parts[0];
            var month = parts[1];

            var yearFolder = Path.Combine(rootFolder, year);
            var monthFolder = Path.Combine(yearFolder, month);

            Directory.CreateDirectory(monthFolder);

            var destinationPath = Path.Combine(monthFolder, Path.GetFileName(file));
            File.Move(file, destinationPath);

            processedFiles++;
            DisplayProgressBar(processedFiles, totalFiles);

            Thread.Sleep(delay);
        }

        stopwatch.Stop();
        Console.WriteLine("\nImages have been successfully organized!");
        Console.WriteLine($"Operation completed in {stopwatch.ElapsedMilliseconds} ms.");
        Console.WriteLine($"Open the folder to view: {rootFolder}");
        System.Diagnostics.Process.Start("explorer", rootFolder);
    }

    static async Task DisplayProgressBar(int progress, int total)
    {
        const int barWidth = 50;
        double percentage = (double)progress / total;
        int filledBars = (int)(percentage * barWidth);

        Console.CursorLeft = 0;
        Console.Write("[");
        Console.Write(new string('#', filledBars));
        Console.Write(new string('-', barWidth - filledBars));
        Console.Write($"] {progress}/{total} ({percentage:P0})");
    }
}

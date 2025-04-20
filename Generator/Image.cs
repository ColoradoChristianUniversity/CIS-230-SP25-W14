using System.Drawing;

using SkiaSharp;

namespace Generator;

public class Image
{
    public static DateOnly? GetFileCreationUtc(string filePath)
    {
        return File.Exists(filePath) ? DateOnly.FromDateTime(File.GetCreationTimeUtc(filePath)) : null;
    }

    public readonly string RootFolderPath;
    private readonly Size size;

    // Add to class
    private readonly Func<DateTime> _dateFactory;

    // Modify constructor
    public Image(string path = @"c:\ccu", Size? size = null, Func<DateTime>? dateFactory = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(null, nameof(path));
        }

        ValidatePath(path);
        this.RootFolderPath = path;

        size ??= new Size(200, 100);
        ValidateSize(size);
        this.size = size.Value;

        _dateFactory = dateFactory ?? (() => DateTime.Now);
    }

    public void Generate(int count)
    {
        if (count <= 0)
        {
            return;
        }

        EnsureDirectoryExists(RootFolderPath);

        for (int i = 0; i < count; i++)
        {
            var value = Random.Shared.Next(-1000, 1000);
            var date = _dateFactory().AddDays(value);
            var text = date.ToString("MMM. dd, yyyy");
            var fileName = date.ToString("yyyy-MM-dd") + ".png";
            var filePath = System.IO.Path.Combine(RootFolderPath, fileName);

            CreateImage(filePath, text);
        }
    }

    private void CreateImage(string filePath, string text)
    {
        using var surface = SKSurface.Create(new SKImageInfo(size.Width, size.Height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        DrawBorder(canvas);
        DrawText(canvas, text);

        SaveImage(surface, filePath);
    }

    private void DrawBorder(SKCanvas canvas)
    {
        var borderPaint = new SKPaint
        {
            Color = SKColors.SkyBlue,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 5
        };
        canvas.DrawRect(2.5f, 2.5f, size.Width - 5, size.Height - 5, borderPaint);
    }

    private void DrawText(SKCanvas canvas, string text)
    {
        var font = new SKFont(SKTypeface.FromFamilyName("Arial"), 20);
        var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };
        var x = size.Width / 2; // Center X
        var y = (size.Height / 2) + (font.Size / 2); // Center Y
        canvas.DrawText(text, x, y, SKTextAlign.Center, font, textPaint);
    }

    private void SaveImage(SKSurface surface, string filePath)
    {
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);

        var timestamp = _dateFactory().ToUniversalTime();
        File.SetCreationTimeUtc(filePath, timestamp);
        File.SetLastWriteTimeUtc(filePath, timestamp);
    }

    private static void ValidateSize(Size? size)
    {
        if (size?.Width < 200 || size?.Height < 100)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size dimensions must be greater than 200x100.");
        }
    }

    private void ValidatePath(string directoryPath)
    {
        var systemPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (systemPaths.Any(systemPath => directoryPath.StartsWith(systemPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The provided path is a system directory and cannot be used.");
        }

        if (Directory.Exists(directoryPath) && Directory.EnumerateFileSystemEntries(directoryPath).Any(entry => System.IO.Path.GetFileName(entry) == ".git"))
        {
            throw new InvalidOperationException("The provided path is a Git-initialized directory and cannot be used.");
        }
    }

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
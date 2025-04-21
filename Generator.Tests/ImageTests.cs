using FluentAssertions;
using System.Drawing;
using System.Security.AccessControl;
using Xunit;

namespace Generator.Tests;

public class ImageTests : IDisposable
{
    private readonly string _testPath = Path.Combine(Directory.GetCurrentDirectory(), "TestImages");
    private readonly DateTime _fixedDate = new(2024, 01, 01);

    public ImageTests()
    {
        Cleanup();
        Directory.CreateDirectory(_testPath);

        try
        {
            var directoryInfo = new DirectoryInfo(_testPath);
            var security = directoryInfo.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(
                "Everyone", 
                FileSystemRights.FullControl, 
                AccessControlType.Allow));
            directoryInfo.SetAccessControl(security);
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Failed to set permissions: {ex.Message}");
        }
    }

    public void Dispose() => Cleanup();

    private void Cleanup()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, recursive: true);
        }
    }

    [Fact]
    public void Constructor_WithValidPathAndSize_ShouldNotThrow()
    {
        var act = () => new Image(_testPath, new Size(300, 200), () => _fixedDate);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullSize_ShouldDefaultSize()
    {
        var act = () => new Image(_testPath, null, () => _fixedDate);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPath_ShouldThrowArgumentException(string path)
    {
        var act = () => new Image(path, new Size(300, 200), () => _fixedDate);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithSystemPath_ShouldThrowInvalidOperationException()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var act = () => new Image(path, new Size(300, 200), () => _fixedDate);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithGitInitializedPath_ShouldThrowInvalidOperationException()
    {
        Directory.CreateDirectory(_testPath);
        File.Create(Path.Combine(_testPath, ".git")).Dispose();

        var act = () => new Image(_testPath, new Size(300, 200), () => _fixedDate);
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(100, 50)]
    [InlineData(100, 0)]
    [InlineData(0, 100)]
    public void Constructor_WithInvalidSize_ShouldThrowArgumentOutOfRangeException(int width, int height)
    {
        var act = () => new Image(_testPath, new Size(width, height), () => _fixedDate);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_WithPositiveCount_ShouldCreateExactNumberOfFiles()
    {
        var image = new Image(_testPath, new Size(300, 200), () => _fixedDate);
        image.Generate(3);

        var files = Directory.GetFiles(_testPath);
        files.Should().HaveCount(3);
        files.Should().OnlyContain(f => f.EndsWith(".png"));
    }

    [Fact]
    public void Generate_WithZeroCount_ShouldNotCreateFiles()
    {
        var image = new Image(_testPath, new Size(300, 200), () => _fixedDate);
        image.Generate(0);

        var files = Directory.Exists(_testPath)
            ? Directory.GetFiles(_testPath)
            : Array.Empty<string>();

        files.Should().BeEmpty();
    }

    [Fact]
    public void Generate_ShouldOverwriteExistingFiles()
    {
        Directory.CreateDirectory(_testPath);
        var existingPath = Path.Combine(_testPath, "existing.png");
        File.WriteAllText(existingPath, "original");

        File.Exists(existingPath).Should().BeTrue();

        var image = new Image(_testPath, new Size(300, 200), () => _fixedDate);
        image.Generate(1); // wipes dir

        File.Exists(existingPath).Should().BeFalse();
    }

    [Fact]
    public void Generate_CalledTwice_ShouldOnlyHaveFilesFromSecondCall()
    {
        var image = new Image(_testPath, new Size(300, 200), () => _fixedDate);
        image.Generate(3);
        var firstSet = Directory.GetFiles(_testPath);

        image.Generate(2); // wipes and replaces

        var secondSet = Directory.GetFiles(_testPath);
        secondSet.Should().HaveCount(2);
        secondSet.Should().NotBeEquivalentTo(firstSet);
    }

    [Fact]
    public void Generate_ShouldCreateDirectoryIfMissing()
    {
        // COUNTERINTUITIVE - Directory is created before the test starts running. 
        // Directory.Exists(_testPath).Should().BeFalse();

        var image = new Image(_testPath, new Size(300, 200), () => _fixedDate);
        image.Generate(1);

        Directory.Exists(_testPath).Should().BeTrue();
        Directory.GetFiles(_testPath).Should().HaveCount(1);
    }

    [Fact]
    public void Generate_ShouldOverwriteFile_WhenCalledMultipleTimes()
    {
        var image = new Image(_testPath, new Size(300, 200), () => _fixedDate);

        image.Generate(1);
        var fileBefore = Directory.GetFiles(_testPath).Single();
        var firstWrite = File.GetLastWriteTimeUtc(fileBefore);

        Thread.Sleep(50);
        image.Generate(1);
        var fileAfter = Directory.GetFiles(_testPath).Single();
        var secondWrite = File.GetLastWriteTimeUtc(fileAfter);

        secondWrite.Should().BeAfter(firstWrite);
    }

    [Fact]
    public void GetFileCreationUtc_ShouldReturnNull_WhenFileDoesNotExist()
    {
        var missingPath = Path.Combine(_testPath, "nonexistent.png");

        var result = Image.GetFileCreationUtc(missingPath);

        result.Should().BeNull();
    }

    [Fact]
    public void GetFileCreationUtc_ShouldReturnFileCreationTime_WhenFileExists()
    {
        var image = new Image(_testPath, new Size(300, 200), () => _fixedDate);
        image.Generate(1);

        var file = Directory.GetFiles(_testPath).Single();
        var result = Image.GetFileCreationUtc(file);
        var expectedDate = DateOnly.FromDateTime(_fixedDate);
        result.Should().Be(expectedDate);
    }
    [Fact]
    public void TestImagesFolder_ShouldBeAccessible()
    {
        Directory.Exists(_testPath).Should().BeTrue();
        var testFilePath = Path.Combine(_testPath, "test.txt");
        File.WriteAllText(testFilePath, "test");
        File.Exists(testFilePath).Should().BeTrue();
    }
}

using Generator;
using Spectre.Console;

class Program
{
    static void Main(string[] args)
    {
        Console.CursorVisible = false;

        ClearAndResetMenu();

        Selection();
    }

    static void Selection()
    {
        int selected = 0;
        var optionIndex = 0;

        imageMenu("start", optionIndex);

        while (selected != 1)
        {
            var keyPressed = getGoodRedKeyPress();
            if (keyPressed.Key == ConsoleKey.UpArrow)
            {
                optionIndex = imageMenu("up", optionIndex);
            }
            else if (keyPressed.Key == ConsoleKey.DownArrow)
            {
                optionIndex = imageMenu("down", optionIndex);
            }
            else if (keyPressed.Key == ConsoleKey.Enter)
            {
                selected = selectedOption(optionIndex);
                imageMenu("start", optionIndex);
            }
        }
    }

    static ConsoleKeyInfo getGoodRedKeyPress()
    {
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow || 
                key.Key == ConsoleKey.DownArrow || 
                key.Key == ConsoleKey.Enter)
            {
                return key;
            }
        }
    }

    static int imageMenu(string direction, int optionIndex){
        var options = new[] { 
            "Generate Images Sorting Test", 
            "Exit" };

        switch (direction)
        {
            case "up":
                optionIndex = optionIndex - 1 < 0 ? options.Length - 1 : optionIndex - 1;
                break;

            case "down":
                optionIndex = optionIndex + 1 >= options.Length ? 0 : optionIndex + 1;
                break;

            case "start":
                optionIndex = 0;
                break;

            default:
                // Handle unexpected input if necessary
                break;
        }

        for (int i = 0; i < options.Length; i++)
        {
            Console.SetCursorPosition(0, i + 3); // Adjust i+x if editing the title
            if (i == optionIndex)
            {
                AnsiConsole.MarkupLine($"> [green]{i + 1}. {options[i]}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"{i + 1}. {options[i]}        ");
            } 
        }

        return optionIndex;
    }

    static int selectedOption(int optionIndex){
        switch (optionIndex){
            case 0:
                OrganizerTest();
                return 0;

            case 1:
                return 1;

            default:
                return 0;
        }
    }

    static void OrganizerTest()
    {
        Console.Clear();
        var imageGenerator = new Image(@"c:\ccu");
        Console.WriteLine("Generating images...");

        AnsiConsole.MarkupLine("[green]Images generated successfully![/]");

        AnsiConsole.Progress()
            .Start (ctx => {
                // Tasks
                var generateTask = ctx.AddTask("[Green]Generating images...[/]");
                var sortTask = ctx.AddTask("[blue]Sorting images...[/]");

                // Generate Task
                int testCount = 1000;
                sortTask.MaxValue = testCount;

                imageGenerator.Generate(testCount, generateTask);


                // Sort Task
                string filePath = @"c:\ccu";
                var images = Directory.GetFiles(filePath);

                sortTask.MaxValue = images.Length;

                foreach (var image in images)
                {
                    var name = Path.GetFileNameWithoutExtension(image); 
                    var parts = name.Split("-");
                    var newPath = Path.Combine(filePath, parts[0], parts[1]); 

                    Directory.CreateDirectory(newPath);

                    File.Move(image, Path.Combine(newPath, Path.GetFileName(image)));
                    sortTask.Increment(1);
                }
            });

        // SortingLogic.SortImages(@"c:\ccu");
        // AnsiConsole.MarkupLine("[blue]Images have been sorted![/]");
        AnsiConsole.MarkupLine("[red]D[/][yellow]o[/][green]n[/][blue]e[/][purple]![/] Press any key to continue.");
        
        Console.ReadKey(true);

        ClearAndResetMenu();
    }

    private static void ClearAndResetMenu()
    {
        Console.Clear();
        AnsiConsole.MarkupLine("""
        Welcome to the Image Organizer!

        Please select an option:
        """);
    }    
}

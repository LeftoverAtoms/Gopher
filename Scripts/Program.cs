using System;
using System.IO;
using System.Linq;

namespace Gopher;

internal static class Program
{
    private const byte IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x0020;

    private static bool IsLAAEnabled => (m_COFFHeader[0] & IMAGE_FILE_LARGE_ADDRESS_AWARE) != 0;

    private static readonly byte[] m_COFFHeader = new byte[2];
    private static readonly byte[] m_PEBuffer = new byte[4];

    private static void Main(string[] args)
    {
        string? path = args.ElementAtOrDefault(0);

        if (path == null)
        {
            ConsoleColor.Red.WriteLine("No arguments were provided");
            Console.Read();
            return;
        }

        if (!Path.Exists(path) || Path.GetExtension(path) != ".exe")
        {
            ConsoleColor.Red.WriteLine("Did not provide a path to an EXE");
            Console.Read();
            return;
        }

        using (var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
        {
            long PEOffset = 0x3C;
            Read(file, PEOffset, m_PEBuffer);

            long COFFOffset = BitConverter.ToUInt32(m_PEBuffer, 0) + 0x4 + 0x12;
            Read(file, COFFOffset, m_COFFHeader);

            ConsoleColor.DarkCyan.WriteLine($"Large Address Aware is {((IsLAAEnabled) ? "enabled" : "disabled")}");
            ConsoleColor.DarkCyan.WriteLine("Enter [y/n] to [modify/exit]");

            // Wait for input.
            while (true)
            {
                char? input = Console.ReadLine()?.ToLower().FirstOrDefault();

                switch (input)
                {
                    case 'y':
                    m_COFFHeader[0] ^= IMAGE_FILE_LARGE_ADDRESS_AWARE; // Flip bit.
                    Write(file, COFFOffset, m_COFFHeader);
                    file.Close(); // Closing the patcher before execution exits scope would result in the file not being saved.
                    ConsoleColor.DarkGreen.WriteLine($"Large Address Aware has been {((IsLAAEnabled) ? "enabled" : "disabled")}");
                    Console.Read();
                    return; // Exit.

                    case 'n':
                    ConsoleColor.DarkGreen.WriteLine("EXE has not been modified");
                    Console.Read();
                    return; // Exit.

                    // Must enter [y/n] to continue.
                    default:
                    break;
                }
            }
        }
    }

    private static void Read(FileStream file, long offset, byte[] buffer)
    {
        try
        {
            file.Seek(offset, SeekOrigin.Begin);
            file.Read(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            ConsoleColor.Red.WriteLine(ex.Message);
            Console.Read();
        }
    }
    private static void Write(FileStream file, long offset, byte[] buffer)
    {
        try
        {
            file.Seek(offset, SeekOrigin.Begin);
            file.WriteAsync(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            ConsoleColor.Red.WriteLine(ex.Message);
            Console.Read();
        }
    }
}

internal static class Extensions
{
    internal static void WriteLine(this ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor(); // Produces inconsistent results in an asyncronous context.
    }
}
using System;

using Typin.Console;

namespace VNLib.Tools.Build.Executor.Extensions
{
    internal static class ConsoleHelpers
    {
        public static void WriteRed(this IConsole console, string message)
            => WriteColor(console, ConsoleColor.Red, message);

        public static void WriteYellow(this IConsole console, string message)
            => WriteColor(console, ConsoleColor.Yellow, message);

        public static void WriteGreen(this IConsole console, string message) 
            => WriteColor(console, ConsoleColor.Green, message);

        public static void WriteColor(this IConsole console, ConsoleColor color, string message)
        {
            console.WithForegroundColor(color, o => o.Error.WriteLine(message));
        }
    }
}
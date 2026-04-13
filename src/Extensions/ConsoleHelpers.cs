using System;

using Typin.Console;

namespace VNLib.Tools.Build.Executor.Extensions
{
    internal static class ConsoleHelpers
    {
        /// <summary>
        /// Writes an error message to the console in red color to the error output stream
        /// </summary>
        /// <param name="console">The console to write to</param>
        /// <param name="message">The error message to write</param>
        public static void WriteError(this IConsole console, string message)
            => console.WithForegroundColor(ConsoleColor.Red, o => o.Error.WriteLine(message));

        /// <summary>
        /// Writes a message to the console in yellow color
        /// </summary>
        /// <param name="console">The console to write to</param>
        /// <param name="message">The warning message to write</param>
        public static void WriteYellow(this IConsole console, string message)
            => WriteColor(console, ConsoleColor.Yellow, message);

        /// <summary>
        /// Writes a success message to the console in green color
        /// </summary>
        /// <param name="console">The console to write to</param>
        /// <param name="message">The success message to write</param>
        public static void WriteGreen(this IConsole console, string message)
            => WriteColor(console, ConsoleColor.Green, message);

        /// <summary>
        /// Writes a message to the console in the specified color
        /// </summary>
        /// <param name="console">The console to write to</param>
        /// <param name="color">The color to write the message in</param>
        /// <param name="message">The message to write</param>
        public static void WriteColor(this IConsole console, ConsoleColor color, string message) 
            => console.WithForegroundColor(color, o => o.Output.WriteLine(message));
    }
}

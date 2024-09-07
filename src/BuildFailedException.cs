using System;

namespace VNLib.Tools.Build.Executor
{
    internal class BuildFailedException : Exception
    {
        public BuildFailedException()
        { }

        public BuildFailedException(string? message) : base(message)
        { }

        public BuildFailedException(string? message, Exception? innerException) : base(message, innerException)
        { }
    }
}
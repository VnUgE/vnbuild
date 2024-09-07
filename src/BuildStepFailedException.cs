using System;

namespace VNLib.Tools.Build.Executor
{
    internal sealed class BuildStepFailedException : BuildFailedException
    {
        public string? ArtifactName { get; set; }

        public BuildStepFailedException()
        { }

        public BuildStepFailedException(string? message) : base(message)
        { }
       

        public BuildStepFailedException(string? message, Exception? innerException) 
            : base(message, innerException)
        { }

        public BuildStepFailedException(string? message, Exception? innerException, string name) 
            : base(message, innerException) 
            => ArtifactName = name;

        public BuildStepFailedException(string message, string artifactName) : base(message) 
            => ArtifactName = artifactName;

        public override string Message => $"in: {ArtifactName} msg -> {base.Message}";
    }
}
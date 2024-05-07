using System;

namespace VNLib.Tools.Build.Executor
{
    sealed class BuildStepFailedException : Exception
    {
        public string? ArtifactName { get; set; }

        public BuildStepFailedException()
        { }

        public BuildStepFailedException(string? message) : base(message)
        { }
       

        public BuildStepFailedException(string? message, Exception? innerException) : base(message, innerException)
        { }

        public BuildStepFailedException(string? message, Exception? innerException, string name) : base(message, innerException)
        {
            ArtifactName = name;
        }

        public BuildStepFailedException(string message, string artifactName):base(message)
        {
            this.ArtifactName = artifactName;
        }
    }
}
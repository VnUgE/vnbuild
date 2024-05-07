namespace VNLib.Tools.Build.Executor.Model
{
    public interface IFeedManager
    {
        /// <summary>
        /// Adds taskfile variables for the feed manager
        /// </summary>
        /// <param name="vars">The taskfile variable container</param>
        void AddVariables(TaskfileVars vars);

        /// <summary>
        /// The output directory of the feed
        /// </summary>
        string FeedOutputDir { get; }
    }
}
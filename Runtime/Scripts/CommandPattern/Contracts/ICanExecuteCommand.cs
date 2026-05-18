namespace IndieGabo.HandyTools.CommandPatternModule
{
    /// <summary>
    /// Defines optional pre-execution validation for one command.
    /// </summary>
    public interface ICanExecuteCommand
    {
        /// <summary>
        /// Evaluates whether the command can execute for the provided context.
        /// </summary>
        /// <param name="context">Execution context for the current request.</param>
        /// <param name="failureReason">Human-readable reason when execution is blocked.</param>
        /// <returns>True when execution is allowed.</returns>
        bool CanExecute(
            ICommandExecutionContext context,
            out string failureReason);
    }
}
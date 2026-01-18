namespace MSPaint.Commands
{
    /// <summary>
    /// Command interface for undo/redo functionality
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command (apply changes)
        /// </summary>
        void Execute();
        
        /// <summary>
        /// Undo the command (restore previous state)
        /// </summary>
        void Undo();
    }
}

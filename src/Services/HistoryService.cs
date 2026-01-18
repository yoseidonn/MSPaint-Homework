using System.Collections.Generic;
using System.Linq;
using MSPaint.Commands;

namespace MSPaint.Services
{
    /// <summary>
    /// Manages undo/redo history using Command Pattern
    /// Maintains separate stacks for undo and redo operations
    /// </summary>
    public class HistoryService
    {
        private readonly Stack<ICommand> _undoStack;
        private readonly Stack<ICommand> _redoStack;
        private readonly int _maxHistorySize;

        public HistoryService(int maxHistorySize = 50)
        {
            _maxHistorySize = maxHistorySize;
            _undoStack = new Stack<ICommand>();
            _redoStack = new Stack<ICommand>();
        }

        /// <summary>
        /// Add a command to history (assumes command is already executed, clears redo stack)
        /// </summary>
        public void AddCommand(ICommand command)
        {
            if (command == null) return;

            // Command is already executed by the tool, just add to history
            // (Execute() is only called during Redo)
            _undoStack.Push(command);

            // Enforce max history size
            if (_undoStack.Count > _maxHistorySize)
            {
                // Remove oldest command (bottom of stack)
                var commands = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = commands.Length - 2; i >= 0; i--) // Skip the last one (oldest)
                {
                    _undoStack.Push(commands[i]);
                }
            }

            // Clear redo stack when new command is added
            _redoStack.Clear();
        }

        /// <summary>
        /// Undo the last command
        /// </summary>
        public bool Undo()
        {
            if (!CanUndo) return false;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            return true;
        }

        /// <summary>
        /// Redo the last undone command
        /// </summary>
        public bool Redo()
        {
            if (!CanRedo) return false;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            return true;
        }

        /// <summary>
        /// Clear all history
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        // Debug methods
        public int GetUndoStackCount() => _undoStack.Count;
        public int GetRedoStackCount() => _redoStack.Count;
    }
}

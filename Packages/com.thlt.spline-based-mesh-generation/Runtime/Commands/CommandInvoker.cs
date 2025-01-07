using System.Collections.Generic;
using UnityEngine;
using System;
namespace THLT.SplineMeshGeneration.Scripts.Commands
{
    [Serializable]
    public class CommandInvoker
    {
        public readonly Stack<ICommand> RedoStack = new Stack<ICommand>();
        public readonly Stack<ICommand> UndoStack = new Stack<ICommand>();


        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            UndoStack.Push(command);
        }

        public void RedoCommand()
        {
            if (RedoStack.Count <= 0) return;
            ICommand command = RedoStack.Pop();
            command.Execute();
            UndoStack.Push(command);
        }

        public void UndoCommand()
        {
         
            if (UndoStack.Count <= 0) return;
            ICommand command = UndoStack.Pop(); 
            command.Undo();
            RedoStack.Clear();
            RedoStack.Push(command);
        }
    }
}

namespace THLT.SplineMeshGeneration.Scripts.Commands
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }
}

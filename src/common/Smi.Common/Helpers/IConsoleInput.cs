using JetBrains.Annotations;

namespace Smi.Common.Helpers
{
    /// <summary>
    /// Interface useful when testing interactive console input
    /// </summary>
    public interface IConsoleInput
    {
        [CanBeNull] public string GetNextLine();
    }
}

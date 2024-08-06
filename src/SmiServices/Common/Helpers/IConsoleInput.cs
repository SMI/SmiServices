namespace SmiServices.Common.Helpers
{
    /// <summary>
    /// Interface useful when testing interactive console input
    /// </summary>
    public interface IConsoleInput
    {
        public string? GetNextLine();
    }
}

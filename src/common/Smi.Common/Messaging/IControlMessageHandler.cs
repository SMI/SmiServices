namespace Smi.Common.Messaging
{
    public interface IControlMessageHandler
    {
        void ControlMessageHandler(string action, string? message);
    }
}

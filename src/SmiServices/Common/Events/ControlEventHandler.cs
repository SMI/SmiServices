namespace SmiServices.Common.Events
{
    /// <summary>
    /// Event handler for hosts to implement if they wish to listen to specific control commands
    /// </summary>
    /// <param name="routingKey"></param>
    /// <param name="message"></param>
    public delegate void ControlEventHandler(string routingKey, string? message = null);
}

using Equ;
using SmiServices.Common.Messaging;

namespace SmiServices.Common.Options
{
    /// <summary>
    /// Configuration options needed to send messages to a RabbitMQ exchange
    /// </summary>
    public class ProducerOptions : MemberwiseEquatable<ProducerOptions>, IOptions
    {
        /// <summary>
        /// Name of the RabbitMQ exchange to send messages to
        /// </summary>
        public string? ExchangeName { get; set; }

        /// <summary>
        /// Maximum number of times to retry the publish confirmations
        /// </summary>
        public int MaxConfirmAttempts { get; set; } = 1;

        /// <summary>
        /// Specify the <see cref="IBackoffProvider"/> to use when handling publish failures
        /// </summary>
        public string? BackoffProviderType { get; set; }

        /// <summary>
        /// Verifies that the individual options have been populated
        /// </summary>
        /// <returns></returns>
        public bool VerifyPopulated()
        {
            return !string.IsNullOrWhiteSpace(ExchangeName);
        }

        public override string ToString() => $"ExchangeName={ExchangeName}, MaxConfirmAttempts={MaxConfirmAttempts}, BackoffProviderType={BackoffProviderType}";
    }
}

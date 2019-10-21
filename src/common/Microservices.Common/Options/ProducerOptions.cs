
namespace Microservices.Common.Options
{
    /// <summary>
    /// Configuration options needed to send messages to a RabbitMQ exchange
    /// </summary>
    public class ProducerOptions
    {
        /// <summary>
        /// Name of the RabbitMQ exchange to send messages to
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// Maximum number of times to retry the publish confirmations
        /// </summary>
        public int MaxConfirmAttempts { get; set; }


        /// <summary>
        /// Verifies that the individual options have been populated
        /// </summary>
        /// <returns></returns>
        public bool VerifyPopulated()
        {
            return !string.IsNullOrWhiteSpace(ExchangeName);
        }

        public override string ToString()
        {
            return "ExchangeName: " + ExchangeName + ", MaxConfirmAttempts: " + MaxConfirmAttempts;
        }

        #region Equality Members

        protected bool Equals(ProducerOptions other)
        {
            return string.Equals(ExchangeName, other.ExchangeName) &&
                   MaxConfirmAttempts == other.MaxConfirmAttempts;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProducerOptions)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ExchangeName != null ? ExchangeName.GetHashCode() : 0) * 397) ^ MaxConfirmAttempts;
            }
        }

        public static bool operator ==(ProducerOptions left, ProducerOptions right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ProducerOptions left, ProducerOptions right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

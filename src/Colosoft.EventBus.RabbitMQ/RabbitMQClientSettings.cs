namespace Colosoft.EventBus.RabbitMQ
{
    public sealed class RabbitMQClientSettings
    {
        public string ConnectionString { get; set; }

        public int MaxConnectRetryCount { get; set; }

        public bool HealthChecks { get; set; }

        public bool Tracing { get; set; }
    }
}

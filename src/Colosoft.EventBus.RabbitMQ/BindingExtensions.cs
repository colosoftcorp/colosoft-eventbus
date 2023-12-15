using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Buffers;
using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace Colosoft.EventBus.RabbitMQ
{
    internal static class BindingExtensions
    {
        private static uint ParseUint(string value, Func<string> getPath)
        {
            try
            {
                return uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(uint)}'.", exception);
            }
        }

        public static T ParseEnum<T>(string value, Func<string> getPath)
            where T : struct
        {
            try
            {
                return Enum.Parse<T>(value, ignoreCase: true);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(T)}'.", exception);
            }
        }

        public static bool ParseBool(string value, Func<string> getPath)
        {
            try
            {
                return bool.Parse(value);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(bool)}'.", exception);
            }
        }

        public static long ParseLong(string value, Func<string> getPath)
        {
            try
            {
                return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(long)}'.", exception);
            }
        }

        public static TimeSpan ParseTimeSpan(string value, Func<string> getPath)
        {
            try
            {
                return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(TimeSpan)}'.", exception);
            }
        }

        public static Guid ParseGuid(string value, Func<string> getPath)
        {
            try
            {
                return Guid.Parse(value);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(Guid)}'.", exception);
            }
        }

        public static ushort ParseUshort(string value, Func<string> getPath)
        {
            try
            {
                return ushort.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(ushort)}'.", exception);
            }
        }

        public static int ParseInt(string value, Func<string> getPath)
        {
            try
            {
                return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(int)}'.", exception);
            }
        }

        public static Uri ParseUri(string value, Func<string> getPath)
        {
            try
            {
                return new Uri(value, UriKind.RelativeOrAbsolute);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to convert configuration value at '{getPath()}' to type '{typeof(Uri)}'.", exception);
            }
        }

        public static IConfiguration AsConfigWithChildren(IConfiguration configuration)
        {
            using (var enumerator = configuration.GetChildren().GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    _ = enumerator.Current;
                    return configuration;
                }
            }

            return null;
        }

        public static void BindCore(
            IConfiguration configuration,
            ArrayPool<byte> instance,
            bool defaultValueIfNotFound)
        {
            if (AsConfigWithChildren(configuration.GetSection("Shared")) is IConfigurationSection section4)
            {
                var shared = ArrayPool<byte>.Shared;
                if (shared == null)
                {
                    throw new InvalidOperationException("Cannot create instance of type 'System.Buffers.ArrayPool<byte>' because it is missing a public instance constructor.");
                }

                BindCore(section4, shared, defaultValueIfNotFound: false);
            }
        }

        private static void BindCore(
            IConfiguration configuration,
            RabbitMQClientSettings instance,
            bool defaultValueIfNotFound)
        {
            var connectionString = configuration["ConnectionString"];
            if (connectionString != null)
            {
                instance.ConnectionString = connectionString;
            }

            var maxConnectRetryCount = configuration["MaxConnectRetryCount"];

            if (maxConnectRetryCount != null)
            {
                instance.MaxConnectRetryCount = int.Parse(maxConnectRetryCount);
            }
            else if (defaultValueIfNotFound)
            {
                instance.MaxConnectRetryCount = 0;
            }
        }

        public static void BindCore(IConfiguration configuration, IDictionary<string, object> instance, bool defaultValueIfNotFound)
        {
            foreach (IConfigurationSection section in configuration.GetChildren())
            {
                string value = section.Value;
                if (value != null)
                {
                    instance[section.Key] = value;
                }
            }
        }

        public static void BindCore(IConfiguration configuration, IProtocol instance, bool defaultValueIfNotFound)
        {
            // ignore
        }

        public static void BindCore(IConfiguration configuration, SslOption instance, bool defaultValueIfNotFound)
        {
            var acceptablePolicyErrors = configuration["AcceptablePolicyErrors"];
            if (acceptablePolicyErrors != null)
            {
                instance.AcceptablePolicyErrors = ParseEnum<SslPolicyErrors>(acceptablePolicyErrors, () => configuration.GetSection("AcceptablePolicyErrors").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.AcceptablePolicyErrors = SslPolicyErrors.None;
            }

            var certPassphrase = configuration["CertPassphrase"];
            if (certPassphrase != null)
            {
                instance.CertPassphrase = certPassphrase;
            }

            var certPath = configuration["CertPath"];
            if (certPath != null)
            {
                instance.CertPath = certPath;
            }

            var checkCertificateRevocation = configuration["CheckCertificateRevocation"];
            if (checkCertificateRevocation != null)
            {
                instance.CheckCertificateRevocation = ParseBool(checkCertificateRevocation, () => configuration.GetSection("CheckCertificateRevocation").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.CheckCertificateRevocation = false;
            }

            var enabled = configuration["Enabled"];
            if (enabled != null)
            {
                instance.Enabled = ParseBool(enabled, () => configuration.GetSection("Enabled").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.Enabled = false;
            }

            var serverName = configuration["ServerName"];
            if (serverName != null)
            {
                instance.ServerName = serverName;
            }

            var version = configuration["Version"];
            if (version != null)
            {
                instance.Version = ParseEnum<SslProtocols>(version, () => configuration.GetSection("Version").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.Version = SslProtocols.None;
            }
        }

        public static void BindCore(IConfiguration configuration, AmqpTcpEndpoint instance, bool defaultValueIfNotFound)
        {
            var hostName = configuration["HostName"];
            if (hostName != null)
            {
                instance.HostName = hostName;
            }

            var port = configuration["Port"];
            if (port != null)
            {
                instance.Port = ParseInt(port, () => configuration.GetSection("Port").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.Port = 0;
            }

            if (AsConfigWithChildren(configuration.GetSection("Protocol")) is IConfigurationSection protocolSection)
            {
                IProtocol protocol = instance.Protocol;
                if (protocol == null)
                {
                    throw new InvalidOperationException("Cannot create instance of type 'RabbitMQ.Client.IProtocol' because it is missing a public instance constructor.");
                }

                BindCore(protocolSection, protocol, defaultValueIfNotFound: false);
            }

            var addressFamily = configuration["AddressFamily"];
            if (addressFamily != null)
            {
                instance.AddressFamily = ParseEnum<AddressFamily>(addressFamily, () => configuration.GetSection("AddressFamily").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.AddressFamily = AddressFamily.Unspecified;
            }

            if (AsConfigWithChildren(configuration.GetSection("Ssl")) is IConfigurationSection sslSection)
            {
                SslOption ssl = instance.Ssl;
                if (ssl == null)
                {
                    ssl = new SslOption();
                }

                BindCore(sslSection, ssl, defaultValueIfNotFound: false);
                instance.Ssl = ssl;
            }
        }

        public static void BindCore(
            IConfiguration configuration,
            ConnectionFactory instance,
            bool defaultValueIfNotFound)
        {
            var defaultAmqpUriSslProtocols = configuration["DefaultAmqpUriSslProtocols"];
            if (defaultAmqpUriSslProtocols != null)
            {
                ConnectionFactory.DefaultAmqpUriSslProtocols = ParseEnum<SslProtocols>(defaultAmqpUriSslProtocols, () => configuration.GetSection("DefaultAmqpUriSslProtocols").Path);
            }
            else if (defaultValueIfNotFound)
            {
                ConnectionFactory.DefaultAmqpUriSslProtocols = SslProtocols.None;
            }

            var amqpUriSslProtocols = configuration["AmqpUriSslProtocols"];
            if (amqpUriSslProtocols != null)
            {
                instance.AmqpUriSslProtocols = ParseEnum<SslProtocols>(amqpUriSslProtocols, () => configuration.GetSection("AmqpUriSslProtocols").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.AmqpUriSslProtocols = SslProtocols.None;
            }

            var defaultAddressFamily = configuration["DefaultAddressFamily"];

            if (defaultAddressFamily != null)
            {
                ConnectionFactory.DefaultAddressFamily = ParseEnum<AddressFamily>(defaultAddressFamily, () => configuration.GetSection("DefaultAddressFamily").Path);
            }
            else if (defaultValueIfNotFound)
            {
                ConnectionFactory.DefaultAddressFamily = AddressFamily.Unspecified;
            }

            var automaticRecoveryEnabled = configuration["AutomaticRecoveryEnabled"];
            if (automaticRecoveryEnabled != null)
            {
                instance.AutomaticRecoveryEnabled = ParseBool(automaticRecoveryEnabled, () => configuration.GetSection("AutomaticRecoveryEnabled").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.AutomaticRecoveryEnabled = false;
            }

            var dispatchConsumersAsync = configuration["DispatchConsumersAsync"];
            if (dispatchConsumersAsync != null)
            {
                instance.DispatchConsumersAsync = ParseBool(dispatchConsumersAsync, () => configuration.GetSection("DispatchConsumersAsync").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.DispatchConsumersAsync = false;
            }

            var consumerDispatchConcurrency = configuration["ConsumerDispatchConcurrency"];
            if (consumerDispatchConcurrency != null)
            {
                instance.ConsumerDispatchConcurrency = ParseInt(consumerDispatchConcurrency, () => configuration.GetSection("ConsumerDispatchConcurrency").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.ConsumerDispatchConcurrency = 0;
            }

            var hostName = configuration["HostName"];
            if (hostName != null)
            {
                instance.HostName = hostName;
            }

            var networkRecoveryInterval = configuration["NetworkRecoveryInterval"];
            if (networkRecoveryInterval != null)
            {
                instance.NetworkRecoveryInterval = ParseTimeSpan(networkRecoveryInterval, () => configuration.GetSection("NetworkRecoveryInterval").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.NetworkRecoveryInterval = default;
            }

            if (AsConfigWithChildren(configuration.GetSection("MemoryPool")) is IConfigurationSection memortPoolSection)
            {
                ArrayPool<byte> memoryPool = instance.MemoryPool;
                if (memoryPool == null)
                {
                    throw new InvalidOperationException("Cannot create instance of type 'System.Buffers.ArrayPool<byte>' because it is missing a public instance constructor.");
                }

                BindCore(memortPoolSection, memoryPool, defaultValueIfNotFound: false);
                instance.MemoryPool = memoryPool;
            }

            var handshakeContinuationTimeout = configuration["HandshakeContinuationTimeout"];
            if (handshakeContinuationTimeout != null)
            {
                instance.HandshakeContinuationTimeout = ParseTimeSpan(handshakeContinuationTimeout, () => configuration.GetSection("HandshakeContinuationTimeout").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.HandshakeContinuationTimeout = default;
            }

            var continuationTimeout = configuration["ContinuationTimeout"];
            if (continuationTimeout != null)
            {
                instance.ContinuationTimeout = ParseTimeSpan(continuationTimeout, () => configuration.GetSection("ContinuationTimeout").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.ContinuationTimeout = default;
            }

            var port = configuration["Port"];
            if (port != null)
            {
                instance.Port = ParseInt(port, () => configuration.GetSection("Port").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.Port = 0;
            }

            var requestedConnectionTimeout = configuration["RequestedConnectionTimeout"];
            if (requestedConnectionTimeout != null)
            {
                instance.RequestedConnectionTimeout = ParseTimeSpan(requestedConnectionTimeout, () => configuration.GetSection("RequestedConnectionTimeout").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.RequestedConnectionTimeout = default;
            }

            var socketReadTimeout = configuration["SocketReadTimeout"];
            if (socketReadTimeout != null)
            {
                instance.SocketReadTimeout = ParseTimeSpan(socketReadTimeout, () => configuration.GetSection("SocketReadTimeout").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.SocketReadTimeout = default;
            }

            var socketWriteTimeout = configuration["SocketWriteTimeout"];
            if (socketWriteTimeout != null)
            {
                instance.SocketWriteTimeout = ParseTimeSpan(socketWriteTimeout, () => configuration.GetSection("SocketWriteTimeout").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.SocketWriteTimeout = default;
            }

            if (AsConfigWithChildren(configuration.GetSection("Ssl")) is IConfigurationSection sslSection)
            {
                SslOption ssl = instance.Ssl;
                if (ssl == null)
                {
                    ssl = new SslOption();
                }

                BindCore(sslSection, ssl, defaultValueIfNotFound: false);
                instance.Ssl = ssl;
            }

            var topologyRecoveryEnabled = configuration["TopologyRecoveryEnabled"];
            if (topologyRecoveryEnabled != null)
            {
                instance.TopologyRecoveryEnabled = ParseBool(topologyRecoveryEnabled, () => configuration.GetSection("TopologyRecoveryEnabled").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.TopologyRecoveryEnabled = false;
            }

            if (AsConfigWithChildren(configuration.GetSection("Endpoint")) is IConfigurationSection endpointSection)
            {
                AmqpTcpEndpoint endpoint = instance.Endpoint;
                if (endpoint == null)
                {
                    endpoint = new AmqpTcpEndpoint();
                }

                BindCore(endpointSection, endpoint, defaultValueIfNotFound: false);
                instance.Endpoint = endpoint;
            }

            if (AsConfigWithChildren(configuration.GetSection("ClientProperties")) is IConfigurationSection clientPropertiesSection)
            {
                IDictionary<string, object> clientProperties = instance.ClientProperties;
                if (clientProperties == null)
                {
                    clientProperties = new Dictionary<string, object>();
                }

                BindCore(clientPropertiesSection, clientProperties, defaultValueIfNotFound: false);
                instance.ClientProperties = clientProperties;
            }

            var userName = configuration["UserName"];
            if (userName != null)
            {
                instance.UserName = userName;
            }

            var password = configuration["Password"];
            if (password != null)
            {
                instance.Password = password;
            }

            if (AsConfigWithChildren(configuration.GetSection("CredentialsRefresher")) is IConfigurationSection)
            {
                throw new InvalidOperationException("Cannot create instance of type 'RabbitMQ.Client.ICredentialsRefresher' because it is missing a public instance constructor.");
            }

            var requestedChannelMax = configuration["RequestedChannelMax"];
            if (requestedChannelMax != null)
            {
                instance.RequestedChannelMax = ParseUshort(requestedChannelMax, () => configuration.GetSection("RequestedChannelMax").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.RequestedChannelMax = 0;
            }

            var requestedFrameMax = configuration["RequestedFrameMax"];
            if (requestedFrameMax != null)
            {
                instance.RequestedFrameMax = ParseUint(requestedFrameMax, () => configuration.GetSection("RequestedFrameMax").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.RequestedFrameMax = 0u;
            }

            string requestedHeartbeat = configuration["RequestedHeartbeat"];
            if (requestedHeartbeat != null)
            {
                instance.RequestedHeartbeat = ParseTimeSpan(requestedHeartbeat, () => configuration.GetSection("RequestedHeartbeat").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.RequestedHeartbeat = default(TimeSpan);
            }

            var useBackgroundThreadsForIO = configuration["UseBackgroundThreadsForIO"];
            if (useBackgroundThreadsForIO != null)
            {
                instance.UseBackgroundThreadsForIO = ParseBool(useBackgroundThreadsForIO, () => configuration.GetSection("UseBackgroundThreadsForIO").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.UseBackgroundThreadsForIO = false;
            }

            var virtualHost = configuration["VirtualHost"];
            if (virtualHost != null)
            {
                instance.VirtualHost = virtualHost;
            }

            var maxMessageSize = configuration["MaxMessageSize"];
            if (maxMessageSize != null)
            {
                instance.MaxMessageSize = ParseUint(maxMessageSize, () => configuration.GetSection("MaxMessageSize").Path);
            }
            else if (defaultValueIfNotFound)
            {
                instance.MaxMessageSize = 0u;
            }

            var uri = configuration["Uri"];
            if (uri != null)
            {
                instance.Uri = ParseUri(uri, () => configuration.GetSection("Uri").Path);
            }

            var clientProvidedName = configuration["ClientProvidedName"];
            if (clientProvidedName != null)
            {
                instance.ClientProvidedName = clientProvidedName;
            }
        }

        public static void Bind_RabbitMQClientSettings(this IConfiguration configuration, object instance)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (instance != null)
            {
                var typedObj = (RabbitMQClientSettings)instance;
                BindCore(configuration, typedObj, defaultValueIfNotFound: false);
            }
        }

        public static void Bind_ConnectionFactory(this IConfiguration configuration, object instance)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (instance != null)
            {
                var typedObj = (ConnectionFactory)instance;
                BindCore(configuration, typedObj, defaultValueIfNotFound: false);
            }
        }
    }
}

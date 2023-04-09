using System.Configuration;
using Confluent.Kafka;

namespace KafkaProducerApp;

public class AppConfig
{
    public AppConfig()
    {
        KafkaConfig = new ProducerConfig
        {
            BootstrapServers = ConfigurationManager.AppSettings["bootstrap.servers"],
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = ConfigurationManager.AppSettings["sasl.username"],
            SaslPassword = ConfigurationManager.AppSettings["sasl.password"]
        };

        TopicName = ConfigurationManager.AppSettings["topic.name"] ?? "datagen-topic";
    }
    public ProducerConfig KafkaConfig { get; private set; }
    public string TopicName { get; private set; }
}
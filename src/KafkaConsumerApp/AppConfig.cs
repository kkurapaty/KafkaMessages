using System.Configuration;
using Confluent.Kafka;

namespace KafkaConsumerApp;

public class AppConfig
{
    public AppConfig()
    {
        KafkaConfig = new ConsumerConfig
        {
            BootstrapServers = ConfigurationManager.AppSettings["bootstrap.servers"],
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SaslUsername = ConfigurationManager.AppSettings["sasl.username"],
            SaslPassword = ConfigurationManager.AppSettings["sasl.password"],
            GroupId = "test-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false,
            //SessionTimeoutMs = 5000,
            EnablePartitionEof = true
        };
        //session.timeout.ms=45000

        TopicName = ConfigurationManager.AppSettings["topic.name"] ?? "datagen-topic";
        ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString ?? string.Empty;
        DbTableName = ConfigurationManager.AppSettings["DbTableName"] ?? string.Empty;
    }
    public ConsumerConfig KafkaConfig { get; private set; }
    public string TopicName { get; private set; }
    public string ConnectionString { get; private set; }
    public string DbTableName {get; private set; }
}
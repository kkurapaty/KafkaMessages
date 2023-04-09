using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace KafkaFeedPlugin
{
    public class ConsumeMessages : IDisposable
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ConcurrentDictionary<string, double> _latestMessages = new ConcurrentDictionary<string, double>();
        public ConsumeMessages()
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "kurapaty.uksouth.azure.confluent.cloud:9092",
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "<USER_NAME>",
                SaslPassword = "<PASSWORD>",
                GroupId = "test-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Latest
            };
            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _consumer.Subscribe("datagen-topic");
        }

        public void StartConsuming(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = _consumer.Consume(cancellationToken);
                    if (message == null)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    var messageJson = message.Message.Value;
                    var messageObj = JsonConvert.DeserializeObject<dynamic>(messageJson);
                    if (messageObj == null) continue;

                    var key = messageObj.key.ToString();
                    //var time = messageObj.value.time.ToString();
                    var value = (double)messageObj.value.value;

                    //Trace.WriteLine($"Notified: Key = {key}, Time = {time}, Value = {value}");
                    _latestMessages[key] = value;
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }
            }
        }
        public double GetMessage(string key)
        {
            _latestMessages.TryGetValue(key, out double value);
            return value;
        }

        public void Dispose()
        {
            _consumer?.Dispose();
        }
    }
}
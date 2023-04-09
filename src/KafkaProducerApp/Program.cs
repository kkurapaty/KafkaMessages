using System.Runtime.InteropServices.ComTypes;
using Confluent.Kafka;
using Newtonsoft.Json;

namespace KafkaProducerApp
{
    /// <summary>
    /// Kafka Producer App
    /// </summary>
    class Program
    {
        private const int CKeyCount = 2;
        static async Task Main()
        {
            var cancelled = false;
            Console.WriteLine("Press Ctrl+C to Cancel.\n");
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cancelled = true;
            };
            
            try
            {
                var config = new AppConfig();
                var producer = new ProducerBuilder<string, string>(config.KafkaConfig).Build();

                var random = new Random();
                var randKeys = new Random();
                while (!cancelled)
                {
                    var message = new
                    {
                        key = $"k{randKeys.Next(1, CKeyCount+1)}",
                        value = new { time = DateTime.UtcNow.ToString("o"), value = random.NextDouble() }
                    };

                    var messageJson = JsonConvert.SerializeObject(message);
                    var kafkaMessage = new Message<string, string>
                    {
                        Key = message.key,
                        Value = messageJson
                    };

                    var deliveryReport = await producer.ProduceAsync(config.TopicName, kafkaMessage);
                    
                    Console.WriteLine($"Published : {messageJson} to {deliveryReport.TopicPartitionOffset}");
                    Thread.Sleep(500);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
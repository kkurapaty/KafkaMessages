using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Confluent.Kafka;

namespace KafkaConsumerApp
{
    /// <summary>
    /// Kafka Consumer App
    /// </summary>
    class Program
    {
        static CancellationTokenSource _cts = new CancellationTokenSource();
        static void Main(string[] args)
        {
            bool canceled = false;
            Console.WriteLine("Press Ctrl+C to Cancel.\n");
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                canceled = true;
                _cts.Cancel();
            };

            try
            {
                var config = new AppConfig();

                using var consumer = new ConsumerBuilder<string, string>(config.KafkaConfig)
                    .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                    .Build();

                consumer.Subscribe(config.TopicName);

                using var connection = new SqlConnection(config.ConnectionString);

                while (!canceled)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(_cts.Token);

                        if (consumeResult.IsPartitionEOF)
                        {
                            Console.WriteLine($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                            continue;
                        }

                        Console.WriteLine($"Received message at {consumeResult.TopicPartitionOffset}: {consumeResult.Message.Value}");
                        try
                        {
                            var messageJson = consumeResult.Message.Value;
                            var messageObj = JsonConvert.DeserializeObject<dynamic>(messageJson);
                            if (messageObj == null) return;

                            var key = messageObj.key.ToString();
                            var time = messageObj.value.time.ToString();
                            var value = messageObj.value.value;

                            var sql = $"INSERT INTO {config.DbTableName} ([Key], [Time], [Value]) VALUES ('{key}', '{time}', {value})";
                            connection.Open();
                            using var command = new SqlCommand(sql, connection);
                            command.ExecuteNonQuery();
                            connection.Close();
                            Console.WriteLine($"Inserted : Key = {key}, Time = {time}, Value = {value}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error: {e.Message}");
                        }
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Consume error: {e.Error.Reason}");
                    }
                }
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                {
                    Console.WriteLine("User stopped the consumer");
                }
                else
                {
                    Console.WriteLine(e);
                }
            }
            _cts?.Dispose();
        }
    }
}
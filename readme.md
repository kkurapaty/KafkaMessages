Kafka Message Producer and Consumer, RTD Automation Demo
========================================================
## Requirement:
input data:
```json
{key:"k1", value:{"time":t1, "value":0.15}}
{key:"k1", value:{"time":t2, "value":0.134}}
{key:"k1", value:{"time":t3, "value":0.13}}
{key:"k1", value:{"time":t4, "value":0.1464}}
{key:"k2", value:{"time":t1, "value":0.134}}
{key:"k2", value:{"time":t2, "value":0.123}}
{key:"k2", value:{"time":t3, "value":0.12366}}
{key:"k2", value:{"time":t4, "value":0.1766}}
{key:"k1", value:{"time":t5, "value":0.12}}
{key:"k2", value:{"time":t5, "value":0.124}}
{key:"k1", value:{"time":t6, "value":0.157}}
```
Step1 : write a c# program that generates like above data and publish to kafka topic time is increasing order date time, value is float number between 0 and 1. use random generator generating 2 message in a second

Step2 : write c# program that connects to kafka topic and listen to the data produced above and write to sql server data base table in below format
table columns: key, time , value

step3: write a excel plugin with RTD that listen of above topic and update excel data showing only latest value by key, so there should only be number of rows as many distinct key
and value correspond to lastest time by each key

Also when excel closes and start again it should show latest data thats there in kafka not empty

## Assumptions:
_ **No assumptions made for requirements, provided as-is**
_ Assumes SQL Server `test-db` database is already in place
_ Uses SQL Server connection for Database inserts
_ Use Database Script to create a table

## Prerequisite:
* Add Kafka Broker Configurations for both Producer & Consumers
* Add Database ConnectionString and Table Name in app.config

## KafkaProducerApp

* We use the ProducerBuilder class from the [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka/) *[net462, net7]* library to create a Kafka producer that connects to the cloud Kafka broker or at localhost:9092.
* This Kafka broker must be configured in app.config, provide required information.
* We then generate two messages per second by creating message object inside the while loop that runs indefinitely or until user cancels by pressing Ctrl C. 
* Inside the loop, we generate a random floating-point value between 0 and 1 using the Random class, and we get the current UTC timestamp as a string using the `DateTime.UtcNow.ToString("o")` method.
* We then serialise each message object to JSON using the JsonConvert.SerializeObject method from the Newtonsoft.Json library.
* We then create a Kafka message object with the JSON message as its value, and send Kafka message object to the Kafka topic using `producer.Produce` method for each message. 
* Finally, we print published messags to the console for debugging purposes, and we wait for half a second using the Thread.Sleep method before generating the next message.

## KafkaConsumerApp

We use the `ConsumerBuilder` class from the `Confluent.Kafka` library to create a Kafka consumer that connects to the cloud or local Kafka broker at localhost:9092. We subscribe to the [datagen-topic]("datagen-topic") topic using the `consumer.Subscribe` method.

Inside the while loop, we consume a single message from the Kafka topic using the `consumer.Consume` method. We deserialize the consumed message JSON to a dynamic object using the `JsonConvert.DeserializeObject` method from the [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) *[net462, net7]* library, and we extract the key, time, and value fields from the message object as strings.

We then construct an SQL INSERT statement that inserts the key, time, and value into a table named `KafkaMessages` using string interpolation. We then open a connection to the SQL Server database using the `connection.Open` method, execute the INSERT statement using a SqlCommand object and the `command.ExecuteNonQuery()` method, and close the connection using the `connection.Close()` method.

Finally, we print the inserted record to the console for debugging purposes.

## KafkaFeedPlugin
This is built on Real Time Data (RTD) Server using Microsoft.Office.Interop classes, implements IRtdServer to provide the data to Excel spreadsheet.

__Note: You need to run Visual Studio 2022 or Visual Studio Code in `Administrator` Mode to install Excel Automation Server.__

Few important methods to note:
`ConnectData` is called for each “topic” that Excel wishes to “subscribe” to. It is called once for every unique subscription. As should be obvious, this implementation assumes there will only be a single topic. ConnectData also starts the timer and returns an initial value that Excel can display.

`DisconnectData` is called to tell the RTD server that Excel is no longer interested in data for the particular topic. In this case, we simply stop the timer and sets `_cancelationTokenSource.Cancel()` to our asynchronous Consume process to prevent the RTD server from notifying Excel of any further updates.

`RefreshData` is called when Excel is ready to retrieve any updated data for the topics that it has previously subscribed to via ConnectData. The implementation looks a bit strange. That’s mainly because Excel is expecting the data as a COM SAFEARRAY.Although it isn’t pretty, The CLR’s COM infrastructure does a commendable job of marshalling the data for you. All you need to do is populate the two-dimensional array with the topic Ids and values and set the topicCount parameter to the number of topics that are included in the update. Finally, the timer is restarted before returning the data.

As soon as `ServerStart` is invoked from Excel, we instantiate the `ConsumeMessages` class and start consuming messages asynchronously and keep latest value in the `ConcurrentDictionary` for each key that we are interested in.

As mentioned earlier, our timer will notify Excel that we have some updates for every 1 second. When the Excel requests for updates, we will read the value from the dictionary (called _lastestMessages) and returns the value for that key.

Once we build the plugin it will automatically register the Office automation server.
Use the `Book1` spreadsheet to see the updates when Producer is running. As long as we have updates, Excel keeps updating the spreadsheet. Once we stop the Producer, we will start noticing that excel will have final value and there will not be any further updates. To see excel updates, start the Producer again.

### Developer Notes
* Developed using simple approach, no fancy patterns, DI, file logging and not fully exception handled (though covered basic error handling).
* Since the requirements did not ask to write Unit Testing, hence did not provided.

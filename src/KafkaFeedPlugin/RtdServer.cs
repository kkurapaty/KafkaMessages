using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;

namespace KafkaFeedPlugin
{
    [Guid("220A2029-3BAD-4C9F-A121-34C881959453")]
    [ProgId("KafkaFeedPlugin.RtdServer.ProgId")]
    public class RtdServer : IRtdServer
    {
        private readonly Dictionary<int, string> _topics = new Dictionary<int, string>();
        private Timer _timer;
        private ConsumeMessages _consumer;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public int ServerStart(IRTDUpdateEvent rtdUpdateEvent)
        {
            _timer = new Timer(delegate { rtdUpdateEvent.UpdateNotify(); }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _consumer = new ConsumeMessages();
            Task.Factory.StartNew(() => _consumer.StartConsuming(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            return 1;
        }

        /// <summary>
        /// ConnectData is called for each “topic” that Excel wishes to “subscribe” to.
        /// It is called once for every unique subscription. As should be obvious, this implementation assumes there will only be a single topic.
        /// ConnectData also starts the timer and returns an initial value that Excel can display.
        /// </summary>
        /// <param name="topicId"></param>
        /// <param name="strings"></param>
        /// <param name="newValues"></param>
        /// <returns></returns>
        public object ConnectData(int topicId, ref Array strings, ref bool newValues)
        {
            var start = strings?.GetValue(0)?.ToString();
            newValues = true;
            _topics[topicId] = start;

            return _consumer.GetMessage(start);
        }

        /// <summary>
        /// RefreshData is called when Excel is ready to retrieve any updated data for the topics that it has previously subscribed to via ConnectData.
        /// The implementation looks a bit strange. That’s mainly because Excel is expecting the data as a COM SAFEARRAY.
        /// Although it isn’t pretty, The CLR’s COM infrastructure does a commendable job of marshalling the data for you.
        /// All you need to do is populate the two-dimensional array with the topic Ids and values and set the topicCount parameter to the number of topics that are included in the update.
        /// Finally, the timer is restarted before returning the data.
        /// </summary>
        /// <param name="topicCount"></param>
        /// <returns></returns>
        public Array RefreshData(ref int topicCount)
        {
            var data = new object[2, _topics.Count];
            var index = 0;

            foreach (var entry in _topics)
            {
                data[0, index] = entry.Key;
                data[1, index] = _consumer.GetMessage(entry.Value);
                ++index;
            }

            topicCount = _topics.Count;

            return data;
        }

        /// <summary>
        /// DisconnectData is called to tell the RTD server that Excel is no longer interested in data for the particular topic.
        /// In this case, we simply stop the timer to prevent the RTD server from notifying Excel of any further updates.
        /// </summary>
        /// <param name="topicId"></param>
        public void DisconnectData(int topicId)
        {
            _topics.Remove(topicId);
            _cancellationTokenSource.Cancel();
        }

        public int Heartbeat() { return 1; }

        public void ServerTerminate() { _consumer.Dispose(); _cancellationTokenSource.Dispose();}

        [ComRegisterFunction]
        public static void RegisterFunction(Type type)
        {
            Microsoft.Win32.Registry.ClassesRoot.CreateSubKey($@"CLSID\{{{type.GUID.ToString().ToUpper()}}}\Programmable");
            var key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"CLSID\{{{type.GUID.ToString().ToUpper()}}}\InprocServer32", true);
            if (key != null)
                key.SetValue(string.Empty, System.Environment.SystemDirectory + @"\mscoree.dll", Microsoft.Win32.RegistryValueKind.String);
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type type)
        {
            Microsoft.Win32.Registry.ClassesRoot.DeleteSubKey($@"CLSID\{{{type.GUID.ToString().ToUpper()}}}\Programmable");
        }
    }
}
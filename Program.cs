using System;
using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Net;
using System.Timers;

namespace device_messaging
{
    class Program
    {
        public static System.Timers.Timer aTimer;

        public static DeviceClient Client;
                                               
        static async Task Main(string[] args)
        {
            Client = DeviceClient.CreateFromConnectionString(args[0].ToString(), Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            aTimer = new System.Timers.Timer(1000);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.AutoReset = true; //This should be true if you want it actually looping
            aTimer.Enabled = true;

            await Client.SetReceiveMessageHandlerAsync(async (Message message, object userContext) => {

                var reader = new StreamReader(message.BodyStream);
                var messageContents = reader.ReadToEnd();

                Console.WriteLine($"Message Contents: {messageContents}");

                Console.WriteLine("Message Propeties:");

                foreach (var property in message.Properties)
                {
                    Console.WriteLine($"Key: {property.Key}, Value: {property.Value}");
                }

                await Client.CompleteAsync(message);

            }, null);

            await Client.SetMethodHandlerAsync("getIP", (MethodRequest methodRequest, object userContext) => {

                Console.WriteLine("IoT Hub invoked the 'getIP' method.");
                Console.WriteLine("Payload:");
                Console.WriteLine(methodRequest.DataAsJson);

                string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
                var externalIp = IPAddress.Parse(externalIpString);

                var responseMessage = "{\"IPAddress\": \"" + externalIp.ToString() + "\", \"response\": \"OK\"}";

                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(responseMessage), 200));

            }, null);

            while (true)
            {
                
            }

        }

        public static async void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {

            string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            var externalIp = IPAddress.Parse(externalIpString);

            var messageToSend = "{'IPAddress': '" + externalIp.ToString() + "'}";

            Message message = new Message(Encoding.ASCII.GetBytes(messageToSend));

            Console.WriteLine("Sending Message {0}", messageToSend);
            await Client.SendEventAsync(message);

        }
    }
}

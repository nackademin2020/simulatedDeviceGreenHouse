using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace simulated_device
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;


        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private readonly static string s_connectionString = "<insert connection string>";

        private static int s_telemetryInterval = 1;
        private static int TemperatureSensorOn = 1;
        private static int HumiditySensorOn = 1;
        private static int SunExposureSensorOn = 0;
        private static int CameraRunningSensorOn = 0;

        // Handle the direct method call
        private static Task<MethodResponse> TurnOnOffTemperatureSensor(MethodRequest methodRequest, object userContext)
        {
            if (TemperatureSensorOn == 1)
            {
                TemperatureSensorOn = 0;
            }
            else
            {
                TemperatureSensorOn = 1;
            }
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static Task<MethodResponse> TurnOnOffHumiditySensor(MethodRequest methodRequest, object userContext)
        {
            if (HumiditySensorOn == 1)
            {
                HumiditySensorOn = 0;
            }
            else
            {
                HumiditySensorOn = 1;
            }
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static Task<MethodResponse> TurnOnOffSunExposureeSensor(MethodRequest methodRequest, object userContext)
        {
            if (SunExposureSensorOn == 1)
            {
                SunExposureSensorOn = 0;
            }
            else
            {
                SunExposureSensorOn = 1;
            }
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static Task<MethodResponse> CameraOnOffSensor(MethodRequest methodRequest, object userContext)
        {
            if (CameraRunningSensorOn == 1)
            {
                CameraRunningSensorOn = 0;
            }
            else
            {
                CameraRunningSensorOn = 1;
            }
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }


        // Async method to send simulated telemetry
        private static async void SendDeviceToCloudMessagesAsync()
        {
            // Initial telemetry values
            double minTemperature = 20;
            double minHumidity = 60;
            double minSunExposure = 10;
            Random rand = new Random();

            SetInitialConfig();

            while (true)
            {
                UpdateConfiguration();
                var twin = GetCurrentConfiguration().Result;
                int configuration1 = twin.Properties.Reported["cameraConfiguration"]["cameraHeatVision"]["OnOff"];
                int configuration2 = twin.Properties.Reported["cameraConfiguration"]["cameraNightVision"]["OnOff"];

                double currentTemperature = (minTemperature + rand.NextDouble() * 15) * TemperatureSensorOn;
                double currentHumidity = (minHumidity + rand.NextDouble() * 20) * HumiditySensorOn;
                int cameraRunning = 1 * CameraRunningSensorOn;
                double currentSunExposure = (minSunExposure + rand.NextDouble() * 10) * SunExposureSensorOn;

                // Create JSON message
                var telemetryDataPoint = new
                {
                    temperature = currentTemperature,
                    humidity = currentHumidity,
                    camera = cameraRunning,
                    sunExposure = currentSunExposure,
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                message.Properties.Add("Configuration Error detected!", (cameraRunning == 1 && (configuration1 == 1) && (configuration2 == 1)) ? "true" : "false");

                // Send the telemetry message
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(s_telemetryInterval * 1000);
            }
        }

        private static void UpdateConfiguration()
        {
            var twins = s_deviceClient.GetTwinAsync().Result;
            JObject desired = twins.Properties.Desired["cameraConfiguration"];
            TwinCollection desiredProperties = new TwinCollection();
            desiredProperties["cameraConfiguration"] = desired;
            s_deviceClient.UpdateReportedPropertiesAsync(desiredProperties).Wait();
        }

        private static async Task<Twin>  GetCurrentConfiguration()
        {
            var twins = s_deviceClient.GetTwinAsync().Result;
            return twins;
        }

        private static void SetInitialConfig()
        {
            TwinCollection reportedProperties, cameraConfiguration, cameraHeatVision, cameraNightVision;
            reportedProperties = new TwinCollection();
            cameraConfiguration = new TwinCollection();
            cameraHeatVision = new TwinCollection();
            cameraNightVision = new TwinCollection();
            cameraHeatVision["OnOff"] = 0;
            cameraNightVision["OnOff"] = 0;
            cameraConfiguration["cameraHeatVision"] = cameraHeatVision;
            cameraConfiguration["cameraNightVision"] = cameraNightVision;
            reportedProperties["cameraConfiguration"] = cameraConfiguration;

            s_deviceClient.UpdateReportedPropertiesAsync(reportedProperties).Wait();
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Simulated device for labb. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Mqtt);

            // Create a handler for the direct method call
            s_deviceClient.SetMethodHandlerAsync("TurnOnOffTemperatureSensor", TurnOnOffTemperatureSensor, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("TurnOnOffHumiditySensor", TurnOnOffHumiditySensor, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("TurnOnOffSunExposureeSensor", TurnOnOffSunExposureeSensor, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("CameraOnOffSensor", CameraOnOffSensor, null).Wait();
            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }
    }
}

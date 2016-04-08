using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace RaspberryCMS
{
    public sealed class StartupTask : IBackgroundTask
    {

        private const float seaLevelPressure = 1013.25f;

        private BackgroundTaskDeferral _deferred;
        private BMP280 _bmp280;
        //private DeviceClient _deviceClient;

        static DeviceClient deviceClient;
        static string iotHubUri = "CMS-IotHub.azure-devices.net";
        static string deviceKey = "QWnaZULkl8geX/cplPCYqYZXqjljy+YQO3bxPvwdJmU=";

        private bool _isCancelled;
        private DateTime _lastReport;
        private IList<float> _tempuratures;
        private IList<float> _pressures;
        private IList<float> _altitudes;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferred = taskInstance.GetDeferral();
            try
            {
                taskInstance.Canceled += TaskInstance_Canceled;

                _bmp280 = new BMP280();
                await _bmp280.Initialize();

                var connectionString = "HostName=CMS-IotHub.azure-devices.net;DeviceId=myFirstDevice;SharedAccessKey=tSnbYN51pXJN1Mg9JyDTLNN9dVpIeyykklcgRnV8Rmo=";
                //var connectionString = "HostName=CMS-IotHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=7MOT33FRh2v7Pv2tQZq93ZixTVu88FPbVkDP1SeCPpc=";
                var connectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
                //_deviceClient = DeviceClient.CreateFromConnectionString(connectionString);

                deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey("myFirstDevice", deviceKey));

                await deviceClient.OpenAsync();

                _lastReport = DateTime.UtcNow.AddMinutes(5 * -1);
                _tempuratures = new List<float>();
                _pressures = new List<float>();
                _altitudes = new List<float>();

                while (!_isCancelled)
                {
                    await ProcessSensorData();

                    if (_lastReport.AddMinutes(5) < DateTime.UtcNow)
                    {
                        await SendSensorDataToIoTHub(connectionStringBuilder.DeviceId);
                        _lastReport = DateTime.UtcNow;
                    }

                    await Task.Delay(5000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during run RaspberryCMS: {ex.Message}");
            }
            finally
            {
                _deferred.Complete();
            }
        }

        private async Task ProcessSensorData()
        {
            _tempuratures.Add(await _bmp280.ReadTemperature());
            _pressures.Add(await _bmp280.ReadPreasure());
            _altitudes.Add(await _bmp280.ReadAltitude(seaLevelPressure));
        }

        private async Task SendSensorDataToIoTHub(string deviceId)
        {
            var temperature = _tempuratures.Average();
            var pressure = _pressures.Average();
            var altitude = _altitudes.Average();
            var weatherStationMessage = new WeatherStationMessage
            {
                DeviceId = deviceId,
                PreciseTime = DateTime.UtcNow,
                Temperature = _tempuratures.Average(),
                Pressure = _pressures.Average()
            };
            await deviceClient.SendEventAsync(
                new Message(
                    Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(weatherStationMessage))));
            _tempuratures.Clear();
            _pressures.Clear();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _isCancelled = true;
        }

    }
}

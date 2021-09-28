using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace  InnovateFPGA2021_WebApp.Models
{
    public class IotHubSetting
    {
        public string ConnectionString { get; set; }
    }
    public class SignalrSetting
    {
        public string ConnectionString { get; set; }
    }

    public class DpsSetting
    {
        public string IdScope { get; set; }
        public string ConnectionString { get; set; }
        public string WebHookUrl { get; set; }
    }

    public class AppSettings
    {
        public SignalrSetting SignalR { get; set; }
        public IotHubSetting IoTHub { get; set; }
        public DpsSetting Dps { get; set; }
    }
}


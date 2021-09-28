using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InnovateFPGA2021_WebApp.Helper;

namespace InnovateFPGA2021_WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventGridController : ControllerBase
    {
        private readonly ILogger<EventGridController> logger;
        private readonly IHubContext<TelemetryHub> _hubContext;

        public EventGridController(IHubContext<TelemetryHub> signalrHubContext, ILogger<EventGridController> Logger)
        {
            logger = Logger;
            _hubContext = signalrHubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] EventGridEvent[] EventGridEvents)
        {
            var ret = Ok();

            foreach (EventGridEvent eventGridEvent in EventGridEvents)
            {
                logger.LogInformation($"Event Type {eventGridEvent.EventType.ToString()}");

                if (HttpContext.Request.Headers.ContainsKey("aeg-event-type"))
                {
                    var eventType = HttpContext.Request.Headers["aeg-event-type"];

                    switch (eventType)
                    {
                        case "SubscriptionValidation":
                            if (eventGridEvent.EventType == EventTypes.EventGridSubscriptionValidationEvent)
                            {
                                var data = JsonConvert.DeserializeObject<SubscriptionValidationEventData>(eventGridEvent.Data.ToString());
                                var response = new SubscriptionValidationResponse(data.ValidationCode);
                                return Ok(response);
                            }
                            break;

                        case "Notification":

                            var deviceId = String.Empty;
                            var model_id = String.Empty;

                            JObject gridData = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

                            // https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-event-grid

                            if (eventGridEvent.EventType == "Microsoft.Devices.DeviceTelemetry")
                            {

                                #region Telemetry Event Sample
                                // Telemetry message format
                                //{
                                //  "properties": { },
                                //  "systemProperties": {
                                //    "iothub-connection-device-id": "WebClient",
                                //    "iothub-connection-auth-method": "{\"scope\":\"device\",\"type\":\"sas\",\"issuer\":\"iothub\",\"acceptingIpFilterRule\":null}",
                                //    "iothub-connection-auth-generation-id": "637313410059604710",
                                //    "iothub-enqueuedtime": "2020-07-27T10:05:24.3710000Z",
                                //    "iothub-message-source": "Telemetry",
                                //    "dt-dataschema": "dtmi:com:example:sampledevice;1"
                                //  },
                                //  "body": "eyAidGVtcGVyYXR1cmUiOiAyIH0="
                                //}
                                #endregion
                                if (gridData.ContainsKey("systemProperties"))
                                {
                                    JObject systemProperties = (JObject)JsonConvert.DeserializeObject(gridData["systemProperties"].ToString());

                                    if (systemProperties.ContainsKey("iothub-connection-device-id"))
                                    {
                                        deviceId = systemProperties["iothub-connection-device-id"].ToString();
                                    }

                                    if (systemProperties.ContainsKey("dt-dataschema"))
                                    {
                                        model_id = systemProperties["dt-dataschema"].ToString();
                                    }
                                }

                                if (gridData.ContainsKey("body"))
                                {
                                    // Body is Base64 encoded.  Convert now.
                                    // System.Text.ASCIIEncoding.ASCII.GetString(System.Convert.FromBase64String(gridData["body"].ToString()));
                                    gridData["body"] = System.Text.ASCIIEncoding.ASCII.GetString(System.Convert.FromBase64String(gridData["body"].ToString()));
                                }

                            }
                            else if (eventGridEvent.EventType == "Microsoft.Devices.DeviceConnected" || eventGridEvent.EventType == "Microsoft.Devices.DeviceDisconnected" ||
                                eventGridEvent.EventType == "Microsoft.Devices.DeviceCreated" || eventGridEvent.EventType == "Microsoft.Devices.DeviceDeleted")
                            {
                                #region Device Creade/delete Event Sample
                                //{
                                //  "twin": {
                                //    "deviceId": "test1",
                                //    "etag": "AAAAAAAAAAA=",
                                //    "deviceEtag": null,
                                //    "status": "enabled",
                                //    "statusUpdateTime": "0001-01-01T00:00:00",
                                //    "connectionState": "Disconnected",
                                //    "lastActivityTime": "0001-01-01T00:00:00",
                                //    "cloudToDeviceMessageCount": 0,
                                //    "authenticationType": "sas",
                                //    "x509Thumbprint": {
                                //      "primaryThumbprint": null,
                                //      "secondaryThumbprint": null
                                //    },
                                //    "version": 1,
                                //    "properties": {
                                //      "desired": {
                                //        "$metadata": {
                                //          "$lastUpdated": "2020-07-27T10:19:37.0424894Z"
                                //        },
                                //        "$version": 1
                                //      },
                                //      "reported": {
                                //        "$metadata": {
                                //          "$lastUpdated": "2020-07-27T10:19:37.0424894Z"
                                //        },
                                //        "$version": 1
                                //      }
                                //    }
                                //  },
                                //  "hubName": "PnPDemo-Hub",
                                //  "deviceId": "test1"
                                //}
                                #endregion

                                #region Device Connect/Disconnect Sample
                                //{
                                //  "deviceConnectionStateEventInfo": {
                                //    "sequenceNumber": "000000000000000001D662AA1D3153C10000000200000000000000000000002F"
                                //  },
                                //  "hubName": "PnPDemo-Hub",
                                //  "deviceId": "WebClient"
                                //}
                                #endregion

                                if (gridData.ContainsKey("deviceId"))
                                {
                                    deviceId = gridData["deviceId"].ToString();
                                }
                            }

                            SIGNALR_NOTIFICATION_DATA eventData = new SIGNALR_NOTIFICATION_DATA
                            {
                                eventId = eventGridEvent.Id,
                                deviceId = deviceId,
                                dtDataSchema = model_id,
                                eventType = "Event Grid",
                                eventTime = eventGridEvent.EventTime.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"), //.ToLongTimeString(),
                                eventSource = eventGridEvent.EventType.ToString(),
                                data = gridData.ToString(),
                            };

                            await _hubContext.Clients.All.SendAsync(
                                "EventGrid",
                                JsonConvert.SerializeObject(eventData)
                            );
                            break;

                        default:
                            logger.LogWarning($"Unknown Event Type {eventType}");
                            return BadRequest($@"Unknown request type");
                    }
                }
            }
            return Ok();

        }

        public class SIGNALR_NOTIFICATION_DATA
        {
            public string eventId { get; set; }
            public string eventType { get; set; }
            public string deviceId { get; set; }
            public string eventSource { get; set; }
            public string eventTime { get; set; }
            public string data { get; set; }
            public string dtDataSchema { get; set; }
        }
    }



}

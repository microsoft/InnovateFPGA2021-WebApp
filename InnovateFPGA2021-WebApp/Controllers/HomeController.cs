using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InnovateFPGA2021_WebApp.Helper;
using InnovateFPGA2021_WebApp.Models;
using Microsoft.Azure.Devices;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Provisioning.Service;
using System.Threading;
using System.Dynamic;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _appSettings;
        private readonly IIoTHubDps _iothubdpshelper;
        private IWebHostEnvironment _environment;
        private string _deploymentManifestReferenceApp;

        public HomeController(IWebHostEnvironment environment, IOptions<AppSettings> optionsAccessor, ILogger<HomeController> logger, IIoTHubDps iothubdpshelper)
        {
            _logger = logger;
            _appSettings = optionsAccessor.Value;
            _logger.LogInformation("HomeController");
            _iothubdpshelper = iothubdpshelper;
            _environment = environment;
            FileInfo fileInfoDeploymentManifestReferenceApp = new FileInfo(Path.Combine(this._environment.WebRootPath, "appdata/DeploymentManifestReferenceApp.json"));
            _deploymentManifestReferenceApp = System.IO.File.ReadAllText(fileInfoDeploymentManifestReferenceApp.ToString());
        }

        public IActionResult Index()
        {
            HomeView homeView = new HomeView();
            ViewData["DpsIdScope"] = _appSettings.Dps.IdScope.ToString();
            ViewData["IoTHubName"] = _iothubdpshelper.IoTHubHubNameGet(_appSettings.IoTHub.ConnectionString);

            FileInfo fileInfoDeploymentManifestEmpty = new FileInfo(Path.Combine(this._environment.WebRootPath, "appdata/DeploymentManifestEmpty.json"));
            string deploymentManifestEmpty = System.IO.File.ReadAllText(fileInfoDeploymentManifestEmpty.ToString());

            ViewData["DeploymentManifestEmpty"] = deploymentManifestEmpty;

            //ViewBag.IoTHubDeviceList = await _iothubdpshelper.IoTHubDeviceListGet();
            return View(homeView);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region IOTHUB
        // Retrieves a list of IoTHub devices
        //
        [HttpGet]
        public async Task<ActionResult> IoTHubDeviceListGet()
        {
            try
            {
                ViewBag.IoTHubDeviceList = await _iothubdpshelper.IoTHubDeviceListGet();
                return PartialView("IoTHubDeviceListPartialView");
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in RefreshIoTHubDevices() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }
        }

        // Retrieves a list of IoTHub modules
        //
        [HttpGet]
        public async Task<ActionResult> IoTHubModuleListGet(string deviceId)
        {
            try
            {
                ViewBag.IoTHubModuleList = await _iothubdpshelper.IoTHubModuleListGet(deviceId);
                return PartialView("IoTHubModuleListPartialView");
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in RefreshIoTHubDevices() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }
        }

        // Retrieve the specified Device object.
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.registrymanager.getdeviceasync?view=azure-dotnet#Microsoft_Azure_Devices_RegistryManager_GetDeviceAsync_System_String
        //
        [HttpGet]
        public async Task<ActionResult> IoTHubGetDeviceInfo(string deviceId)
        {
            Device device = null;
            IOTHUB_DEVICE_DATA deviceData = new IOTHUB_DEVICE_DATA();
            Twin twin = null;

            // Retrieve device
            device = await _iothubdpshelper.IoTHubDeviceGet(deviceId).ConfigureAwait(false);

            if (device == null)
            {
                return StatusCode(500, new { message = $"Could not find Device ID : {deviceId}" });
            }

            // Retrieve Deivce Twin for the device
            twin = await _iothubdpshelper.IoTHubDeviceTwinGet(deviceId).ConfigureAwait(false);

            if (twin == null)
            {
                return StatusCode(500, new { message = $"Could not find Twin for Device ID : {deviceId}" });
            }

            deviceData.deviceId = device.Id;
            deviceData.connectionState = device.ConnectionState.ToString();
            deviceData.status = device.Status.ToString();
            deviceData.authenticationType = device.Authentication.Type.ToString();

            if (device.Authentication.Type == AuthenticationType.Sas)
            {
                deviceData.symmetricKey = device.Authentication.SymmetricKey.PrimaryKey;
            }

            JObject twinJson = (JObject)JsonConvert.DeserializeObject(twin.ToJson());

            deviceData.isEdge = twin.Capabilities.IotEdge;

            if (deviceData.isEdge == true)
            {

            }

            // Check if this is IoT Plug and Play device or not
            if (twinJson.ContainsKey("modelId"))
            {
                deviceData.deviceModelId = twin.ModelId;
            }
            return Json(deviceData);
        }

        // Retrieve the Device Twin.
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.registrymanager.gettwinasync?view=azure-dotnet
        //
        [HttpGet]
        public async Task<ActionResult> IoTHubGetDeviceTwin(string deviceId)
        {
            Device device = null;
            Twin twin = null;

            // Retrieve device
            device = await _iothubdpshelper.IoTHubDeviceGet(deviceId).ConfigureAwait(false);

            if (device == null)
            {
                return StatusCode(500, new { message = $"Could not find Device ID : {deviceId}" });
            }

            // Retrieve Deivce Twin for the device
            twin = await _iothubdpshelper.IoTHubDeviceTwinGet(deviceId).ConfigureAwait(false);

            if (twin == null)
            {
                return StatusCode(500, new { message = $"Could not find Twin for Device ID : {deviceId}" });
            }

            JObject twinJson = (JObject)JsonConvert.DeserializeObject(twin.ToJson());

            return Json(twinJson.ToString());
        }

        // Deletes a previously registered device from IoT Hub
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.registrymanager.removedeviceasync?view=azure-dotnet#Microsoft_Azure_Devices_RegistryManager_RemoveDeviceAsync_Microsoft_Azure_Devices_Device_System_Threading_CancellationToken_
        [HttpDelete]
        public async Task<ActionResult> IoTHubDeviceDelete(string deviceId)
        {
            bool bDeleted = false;
            try
            {
                bDeleted = await _iothubdpshelper.IoTHubDeviceDelete(deviceId);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubDeviceDelete() : {e.InnerException.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            if (bDeleted)
            {
                // make sure it's removed from IoT Hub
                // We do this because if we query too fast, IoT Hub may return deleted device id.

                while (await _iothubdpshelper.IoTHubDeviceCheck(deviceId) == true)
                {
                    Thread.Sleep(500);
                }

                return Ok();
            }
            else
            {
                return StatusCode(400, new { message = $"Failed to delete {deviceId}" });
            }
        }


        // Register a new device with IoT Hub
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.registrymanager.adddeviceasync?view=azure-dotnet#Microsoft_Azure_Devices_RegistryManager_AddDeviceAsync_Microsoft_Azure_Devices_Device_
        [HttpPost]
        public async Task<ActionResult> IoTHubDeviceCreate(string deviceId, bool isEdge)
        {
            bool bCreated = false;
            try
            {
                bCreated = await _iothubdpshelper.IoTHubDeviceCreate(deviceId, isEdge);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubDeviceCreate() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            if (bCreated == true)
            {

                while (await _iothubdpshelper.IoTHubDeviceCheck(deviceId) == false)
                {
                    Thread.Sleep(500);
                }

                return Ok();
            }
            else
            {
                return StatusCode(400);
            }
        }

        // Deploy IoT Edge Module
        // https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.registrymanager.applyconfigurationcontentondeviceasync?view=azure-dotnet
        [HttpPost]
        public async Task<ActionResult> IoTHubDeployManifest(string deviceId, string deploymentManifest)
        {
            try
            {
                await _iothubdpshelper.IoTHubDeployManifest(deviceId, deploymentManifest);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubDeployManifest() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            return Ok();
        }

        // Add IoT Edge Module
        // https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.registrymanager.addmoduleasync?view=azure-dotnet
        [HttpPost]
        public async Task<ActionResult> IoTHubAddModule(string deviceId, string moduleId, string image, string createOption)
        {
            try
            {
                dynamic expando = new ExpandoObject();

                await _iothubdpshelper.IoTHubAddModule(deviceId, moduleId, image, createOption);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubAddModule() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            return Ok();
        }

        // Remove IoT Edge Module
        // https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.registrymanager.addmoduleasync?view=azure-dotnet
        [HttpPost]
        public async Task<ActionResult> IoTHubRemoveModule(string deviceId, string moduleId)
        {
            try
            {
                await _iothubdpshelper.IoTHubRemoveModule(deviceId, moduleId);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubRemoveModule() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            return Ok();
        }

        // Deploy Reference Application.  Deployment Manifest is hard coded
        [HttpPost]
        public async Task<ActionResult> IoTHubDeployReferenceApp(string deviceId)
        {
            try
            {
                await _iothubdpshelper.IoTHubDeployReferenceApp(deviceId, this._deploymentManifestReferenceApp);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubDeployReferenceApp() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            return Ok();
        }
        // Invoke Device Method
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.serviceclient.invokedevicemethodasync?view=azure-dotnet
        [HttpPost]
        public async Task<ActionResult> IoTHubSendCommand(string deviceId, string commandName, string payLoad)
        {
            try
            {
                var response = await _iothubdpshelper.IoTHubDeviceSendCommand(deviceId, commandName, payLoad);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubDeviceCreate() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            return Ok();
        }

        // Invoke Device Method
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.serviceclient.invokedevicemethodasync?view=azure-dotnet
        [HttpPost]
        public async Task<ActionResult> IoTHubDeployIoTEdgeModule(string deviceId, string config)
        {
            try
            {
                await _iothubdpshelper.IoTHubApplyConfiguration(deviceId, config);
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in IoTHubDeviceCreate() : {e.Message}");
                return StatusCode(400, new { message = e.Message });
            }

            return Ok();
        }
        #endregion

        /*************************************************************
        * Device Provisioning Service (DPS)
        *************************************************************/
        #region DPS

        [HttpGet]
        public async Task<ActionResult> DpsEnrollmentListGet()
        {
            var enrollmentList = await _iothubdpshelper.DpsEnrollmentListGet();
            ViewBag.DpsEnrollmentList = enrollmentList;
            return PartialView("DpsEnrollmentListPartialView");
        }

        //
        // Retrieves individual entrollment info.
        // Supports Symmetric Key only.
        // To do : Add X.509 support
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.devices.provisioning.service.provisioningserviceclient.createindividualenrollmentquery?view=azure-dotnet
        [HttpPost]
        public async Task<ActionResult> DpsEnrollmentGet(string enrollmentName, bool isGroup)
        {
            DPS_ENROLLMENT_DATA enrollmentData = new DPS_ENROLLMENT_DATA();

            try
            {
                enrollmentData = await _iothubdpshelper.DpsEnrollmentGet(enrollmentName, isGroup).ConfigureAwait(false);

                if (enrollmentData != null)
                {
                    AttestationMechanism attestationMechanism = await _iothubdpshelper.DpsAttestationMethodGet(enrollmentData.registrationId, enrollmentData.isGroup).ConfigureAwait(false);

                    if (attestationMechanism == null)
                    {
                        _logger.LogWarning($"Attestation Mechanism for {enrollmentName} not found");
                        return StatusCode(400, new { message = $"Attestation Mechanism for {enrollmentName} not found"});
                    }

                    if (attestationMechanism.Type.Equals(AttestationMechanismType.SymmetricKey))
                    {
                        SymmetricKeyAttestation attestation = (SymmetricKeyAttestation)attestationMechanism.GetAttestation();
                        enrollmentData.symmetricKey = attestation.PrimaryKey;
                    }
                    else
                    {
                        return StatusCode(400, new { message = $"Attestation is not Symmetric Key but {attestationMechanism.Type.ToString()}"});
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in GetDpsEnrollment() : {e.InnerException.Message}");
                if (e.InnerException != null)
                {
                    return StatusCode(400, new { message = e.InnerException.Message });
                }
                else
                {
                    return StatusCode(400, new { message = e.Message });
                }
            }

            return Json(enrollmentData);
        }

        // Add a new individual enrollment
        // https://docs.microsoft.com/en-us/rest/api/iot-dps/createorupdateindividualenrollment
        [HttpPost]
        public async Task<ActionResult> DpsEnrollmentCreate(string newRegistrationId, bool isGroup, bool isEdge)
        {
            bool bCreated = false;
            DpsEnrollmentListViewModel enrollmentList = null;

            try
            {
                bCreated = await _iothubdpshelper.DpsEnrollmentCreate(newRegistrationId, isGroup, isEdge);

                if (bCreated)
                {
                    bool bFound = false;

                    while (!bFound)
                    {
                        enrollmentList = await _iothubdpshelper.DpsEnrollmentListGet();

                        foreach (var item in enrollmentList.Enrollments)
                        {
                            if (item.RegistrationId == newRegistrationId && item.isGroup == isGroup)
                            {
                                enrollmentList.SelectedEnrollment = newRegistrationId;
                                ViewBag.DpsEnrollmentList = enrollmentList;
                                bFound = true;
                                break;
                            }
                        }

                        if (bFound != true)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in AddDpsEnrollment() : {e.InnerException.Message}");
                if (e.InnerException != null)
                {
                    return StatusCode(400, new { message = e.InnerException.Message });
                }
                else
                {
                    return StatusCode(400, new { message = e.Message });
                }
            }

            return PartialView("DpsEnrollmentListPartialView");
        }

        // Delete a device enrollment record
        // https://docs.microsoft.com/en-us/rest/api/iot-dps/deleteindividualenrollment/deleteindividualenrollment
        [HttpDelete]
        public async Task<ActionResult> DpsEnrollmentDelete(string registrationId, bool isGroup)
        {
            bool bDeleted = false;
            DpsEnrollmentListViewModel enrollmentList = null;

            try
            {
                bDeleted = await _iothubdpshelper.DpsEnrollmentDelete(registrationId, isGroup);

                if (bDeleted)
                {
                    bool bFound = true;

                    while (bFound)
                    {
                        enrollmentList = await _iothubdpshelper.DpsEnrollmentListGet();

                        if (enrollmentList.Enrollments.Count == 0)
                        {
                            bFound = false;
                            break;
                        }

                        foreach (var item in enrollmentList.Enrollments)
                        {
                            if (item.RegistrationId == registrationId)
                            {
                                bFound = true;
                                break;
                            }
                            else
                            {
                                bFound = false;
                            }
                        }

                        if (bFound == true)
                        {
                            Thread.Sleep(100);
                        }
                    }
                    ViewBag.DpsEnrollmentList = enrollmentList;
                }

            }
            catch (Exception e)
            {
                _logger.LogError($"Exception in DeleteDpsEnrollment() : {e.InnerException.Message}");
                if (e.InnerException != null)
                {
                    return StatusCode(400, new { message = e.InnerException.Message });
                }
                else
                {
                    return StatusCode(400, new { message = e.Message });
                }
            }

            return PartialView("DpsEnrollmentListPartialView");
        }

        #endregion
    }
}

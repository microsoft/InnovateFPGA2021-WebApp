using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace  InnovateFPGA2021_WebApp.Models
{
    public class HomeView
    {
        [DisplayName("Select Device")]
        public string deviceId { get; set; }
        public string moduleId { get; set; }
        public IEnumerable<SelectListItem> deviceList { get; set; }
        public IEnumerable<SelectListItem> moduleList { get; set; }
        public string newDeviceId { get; set; }
        public string registrationId { get; set; }
        public DpsEnrollmentListViewModel dpsEnrollmentList { get; set; }
        public DpsEnrollmentListViewModel dpsGroupEnrollmentList { get; set; }
    }

    public class IoTHubDeviceListViewModel
    {
        public IoTHubDeviceListViewModel()
        {
            Devices = new List<IoTHubDeviceViewModel>();
        }

        [Display(Name = "IoT Hub Device List")]
        [Required]
        public string SelectedIoTHubDevice { get; set; }
        public IList<IoTHubDeviceViewModel> Devices { get; set; }
    }

    public class IoTHubDeviceViewModel
    {
        public string DeviceId { get; set; }
        public string AuthenticationType {get;set;}
        public bool IsEdge {get;set;}
        public string ModelId {get;set;}
        public string Status {get;set;}
    }

    public class IoTHubModuleListViewModel
    {
        public IoTHubModuleListViewModel()
        {
            Modules = new List<IoTHubModuleViewModel>();
        }

        [Display(Name = "IoT Hub Module List")]
        [Required]
        public string SelectedIoTHubModule { get; set; }
        public IList<IoTHubModuleViewModel> Modules { get; set; }
    }

    public class IoTHubModuleViewModel
    {
        public string ModuleId { get; set; }
        public string ModelId { get; set; }
        public string Status { get; set; }
    }

    public class DpsEnrollmentListViewModel
    {
        public DpsEnrollmentListViewModel()
        {
            Enrollments = new List<EnrollmentViewModel>();
        }

        [Display(Name = "DPS Enrollment List")]
        [Required]
        public string SelectedEnrollment { get; set; }
        public IList<EnrollmentViewModel> Enrollments { get; set; }
    }

    public class DpsGroupEnrollmentListViewModel
    {
        public DpsGroupEnrollmentListViewModel()
        {
            Enrollments = new List<EnrollmentViewModel>();
        }

        [Display(Name = "DPS Group Enrollment List")]
        [Required]
        public string SelectedEnrollment { get; set; }
        public IList<EnrollmentViewModel> Enrollments { get; set; }
    }

    public class EnrollmentViewModel
    {
        public string RegistrationId { get; set; }
        public bool isGroup { get; set; }
    }

    public class IOTHUB_DEVICE_DATA
    {
        public string deviceId { get; set; }
        public string connectionState { get; set; }
        public string status { get; set; }
        public string authenticationType { get; set; }
        public string symmetricKey { get; set; }
        public string deviceConnectionString { get; set; }
        public string deviceModelId { get; set; }
        public bool isEdge { get; set; }
    }
    public class DPS_ENROLLMENT_DATA
    {
        public string registrationId { get; set; }
        public string symmetricKey { get; set; }
        public string status { get; set; }
        public bool isEdge { get; set; }
        public bool isGroup { get; set; }
    }
}

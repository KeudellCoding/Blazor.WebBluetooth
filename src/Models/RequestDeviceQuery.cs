using Newtonsoft.Json;
using System.Collections.Generic;

namespace KeudellCoding.Blazor.WebBluetooth.Models {
    public class RequestDeviceQuery {
        [JsonProperty(propertyName: "filters")]
        public List<Filter> Filters { get; set; } = new List<Filter>();

        [JsonProperty(propertyName: "acceptAllDevices")]
        public bool? AcceptAllDevices { get; set; } = null;

        [JsonProperty(propertyName: "optionalServices")]
        public List<string> OptionalServices { get; set; } = new List<string>();
    }
}

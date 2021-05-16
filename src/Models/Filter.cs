using Newtonsoft.Json;
using System.Collections.Generic;

namespace KeudellCoding.Blazor.WebBluetooth.Models {
    public class Filter {
        [JsonProperty(propertyName: "services")]
        public List<string> Services { get; set; } = new List<string>();

        [JsonProperty(propertyName: "name")]
        public string Name { get; set; }
    }
}

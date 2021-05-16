using System.Linq;

namespace KeudellCoding.Blazor.WebBluetooth.Models {
    public class Notification {
        public Device Device { get; set; }
        public string ServiceId { get; set; }
        public string CharacteristicId { get; set; }
        public uint[] RawUIntValue { get; set; }

        public byte[] Value => RawUIntValue?.Select(v => (byte)(v & 0xFF)).ToArray();
    }
}

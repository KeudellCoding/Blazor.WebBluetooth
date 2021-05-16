using KeudellCoding.Blazor.WebBluetooth.Models;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeudellCoding.Blazor.WebBluetooth {
    public class WebBluetoothControllerService : IAsyncDisposable {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
        private readonly DotNetObjectReference<WebBluetoothControllerService> _dotNetRef;

        private Timer updateDeviceListTimer;
        private CancellationTokenSource updateDeviceListTimerCts;
        private bool initialized = false;

        private List<Device> connectedDevices = new List<Device> { };
        public ReadOnlyCollection<Device> ConnectedDevices => connectedDevices.AsReadOnly();

        public event Action<Device> OnDeviceConnected;
        public event Action<Device> OnDeviceDisconnected;
        public event Action<Notification> OnNotification;

        // ============================================================================================================

        public WebBluetoothControllerService(IJSRuntime jsRuntime) {
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
               "import", "./_content/KeudellCoding.Blazor.WebBluetooth/WebBluetoothFunctions.js").AsTask());

            _dotNetRef = DotNetObjectReference.Create(this);

            updateDeviceListTimer = new Timer(updatePairedBluetoothDevicesTask, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        // ============================================================================================================

        private async Task setDotNetRefAsync(CancellationToken cancellationToken = default) {
            if (initialized) return;
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("WebBluetoothFunctions.SetDotNetRef", cancellationToken, _dotNetRef);
            initialized = true;
        }

        private void updatePairedBluetoothDevicesTask(object state) {
            updateDeviceListTimerCts?.Cancel();
            updateDeviceListTimerCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            _ = UpdatePairedBluetoothDevicesAsync(updateDeviceListTimerCts.Token);
        }

        // ============================================================================================================

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<bool>("WebBluetoothFunctions.CheckAvailability", cancellationToken);
        }

        public async Task UpdatePairedBluetoothDevicesAsync(CancellationToken cancellationToken = default) {
            await setDotNetRefAsync(cancellationToken);

            var module = await _moduleTask.Value;
            var jsDevices = await module.InvokeAsync<Device[]>("WebBluetoothFunctions.GetPairedBluetoothDevices", cancellationToken);

            // Remove disconnected devices
            for (int i = connectedDevices.Count - 1; i >= 0; i--) {
                if (!jsDevices.Select(d => d.Id).Contains(connectedDevices[i].Id)) {
                    OnDeviceDisconnected?.Invoke(connectedDevices[i]);
                    connectedDevices.RemoveAt(i);
                }
            }

            // Add connected Devices
            foreach (var newDevice in jsDevices) {
                if (!connectedDevices.Select(d => d.Id).Contains(newDevice.Id)) {
                    connectedDevices.Add(newDevice);
                    OnDeviceConnected?.Invoke(newDevice);
                }
            }
        }

        public async Task<Device> RequestDeviceAsync(RequestDeviceQuery query, CancellationToken cancellationToken = default) {
            await setDotNetRefAsync(cancellationToken);

            var jsonQuery = JsonConvert.SerializeObject(query, Formatting.None, new JsonSerializerSettings() {
                NullValueHandling = NullValueHandling.Ignore
            });

            var module = await _moduleTask.Value;
            var newDevice = await module.InvokeAsync<Device>("WebBluetoothFunctions.RequestDevice", cancellationToken, jsonQuery);
            if (newDevice != null) {
                connectedDevices.Add(newDevice);
                OnDeviceConnected?.Invoke(newDevice);
            }
            return newDevice;
        }
        public async Task DisconnectAsync(Device device, CancellationToken cancellationToken = default) {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("WebBluetoothFunctions.DisconnectDevice", cancellationToken, device.Id);
            await UpdatePairedBluetoothDevicesAsync(cancellationToken);
        }

        public async Task<bool> SendValueAsync(Device device, string serviceId, string characteristicId, byte[] data, CancellationToken cancellationToken = default) {
            var module = await _moduleTask.Value;
            var bytes = data.Select(v => (uint)v).ToArray();
            return await module.InvokeAsync<bool>("WebBluetoothFunctions.SendValue", cancellationToken, device.Id, serviceId, characteristicId, bytes);
        }
        public async Task<byte[]> ReadValueAsync(Device device, string serviceId, string characteristicId, CancellationToken cancellationToken = default) {
            var module = await _moduleTask.Value;
            var rawValue = await module.InvokeAsync<uint[]>("WebBluetoothFunctions.ReadValue", cancellationToken, device.Id, serviceId, characteristicId);
            return rawValue?.Select(v => (byte)(v & 0xFF)).ToArray();
        }

        public async Task<bool> SetupNotificationAsync(Device device, string serviceId, string characteristicId, CancellationToken cancellationToken = default) {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<bool>("WebBluetoothFunctions.SetupNotification", cancellationToken, device.Id, serviceId, characteristicId);
        }
        
        // ============================================================================================================

        [JSInvokable("OnDisconnect")]
        public void JsOnDisconnect(Device device) {
            var deviceObj = connectedDevices.SingleOrDefault(d => d.Id == device.Id);
            if (deviceObj != null) {
                OnDeviceDisconnected?.Invoke(deviceObj);
                connectedDevices.Remove(deviceObj);
            }
        }

        [JSInvokable("OnNotify")]
        public void JsOnNotify(Notification notification) {
            var deviceObj = connectedDevices.SingleOrDefault(d => d.Id == notification.Device.Id);
            if (deviceObj != null) {
                notification.Device = deviceObj;
                OnNotification?.Invoke(notification);
            }
        }

        // ============================================================================================================

        public async ValueTask DisposeAsync() {
            if (updateDeviceListTimer != null) {
                updateDeviceListTimer.Change(Timeout.Infinite, 0);
                await updateDeviceListTimer.DisposeAsync();
            }

            _dotNetRef?.Dispose();

            if (_moduleTask.IsValueCreated) {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}

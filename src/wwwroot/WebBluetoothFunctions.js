export const WebBluetoothFunctions = {
    DotNetReference: null,
    PairedBluetoothDevices: [],

    async CheckAvailability() {
        try {
            return await navigator.bluetooth.getAvailability();
        } catch (e) {
            return false;
        }
    },

    SetDotNetRef(dotNetRef) {
        WebBluetoothFunctions.DotNetReference = dotNetRef;
    },
    GetPairedBluetoothDevices() {
        WebBluetoothFunctions.PairedBluetoothDevices = WebBluetoothFunctions.PairedBluetoothDevices.filter(function (item) {
            return item.gatt.connected;
        });

        return WebBluetoothFunctions.PairedBluetoothDevices.map(function (item) {
            return { "Id": item.id, "Name": item.name };
        });
    },
    GetPairedBluetoothDeviceObj(deviceId) {
        try {
            WebBluetoothFunctions.PairedBluetoothDevices = WebBluetoothFunctions.PairedBluetoothDevices.filter(function (item) {
                return item.gatt.connected;
            });

            return WebBluetoothFunctions.PairedBluetoothDevices.filter(function (item) {
                return item.id == deviceId;
            })[0];
        } catch (e) {
            return null;
        }
    },

    async RequestDevice(jsonQuery) {
        try {
            var query = JSON.parse(jsonQuery);
            var newDevice = await navigator.bluetooth.requestDevice(query);
            newDevice.addEventListener('gattserverdisconnected', WebBluetoothFunctions.OnDisconnect);
            await newDevice.gatt.connect();

            if (newDevice.gatt.connected) {
                WebBluetoothFunctions.PairedBluetoothDevices.push(newDevice);
                return { "Id": newDevice.id, "Name": newDevice.name };
            }
            else {
                return null;
            }
        } catch (e) {
            return null;
        }
    },
    async DisconnectDevice(deviceId) {
        var device = WebBluetoothFunctions.GetPairedBluetoothDeviceObj(deviceId);

        if (device) {
            try {
                device.gatt.disconnect();
            } catch (e) { }
        }
    },

    async SendValue(deviceId, serviceId, characteristicId, data) {
        var device = WebBluetoothFunctions.GetPairedBluetoothDeviceObj(deviceId);

        if (device) {
            try {
                var service = await device.gatt.getPrimaryService(serviceId);
                var characteristic = await service.getCharacteristic(characteristicId);

                var byteArrayToSend = Uint8Array.from(data);
                await characteristic.writeValue(byteArrayToSend);
                return true;
            } catch (e) {
                return false;
            }
        }
        else {
            return false;
        }
    },
    async ReadValue(deviceId, serviceId, characteristicId) {
        var device = WebBluetoothFunctions.GetPairedBluetoothDeviceObj(deviceId);

        if (device) {
            try {
                var service = await device.gatt.getPrimaryService(serviceId);
                var characteristic = await service.getCharacteristic(characteristicId);

                var value = await characteristic.readValue();
                var byteArray = new Uint8Array(value.buffer);
                return Array.from(byteArray);
            } catch (e) {
                return null;
            }
        }
        else {
            return null;
        }
    },

    async SetupNotification(deviceId, serviceId, characteristicId) {
        var device = WebBluetoothFunctions.GetPairedBluetoothDeviceObj(deviceId);

        if (device) {
            try {
                var service = await device.gatt.getPrimaryService(serviceId);
                var characteristic = await service.getCharacteristic(characteristicId);
                await characteristic.startNotifications();

                characteristic.addEventListener('characteristicvaluechanged', WebBluetoothFunctions.OnNotification, false);
                return true;
            } catch (e) {
                console.error(e);
                return false;
            }
        }
        else {
            return false;
        }
    },

    async OnDisconnect(event) {
        if (WebBluetoothFunctions.DotNetReference) {
            var device = {
                "Id": event.target.id,
                "Name": event.target.name
            };
            await WebBluetoothFunctions.DotNetReference.invokeMethodAsync("OnDisconnect", device);

            WebBluetoothFunctions.PairedBluetoothDevices = WebBluetoothFunctions.PairedBluetoothDevices.filter(function (item) {
                return item.gatt.connected;
            });
        }
    },
    async OnNotification(event) {
        if (WebBluetoothFunctions.DotNetReference) {
            var value = event.target.value;
            var byteArray = new Uint8Array(value.buffer);

            var notification = {
                "Device": {
                    "Id": event.target.service.device.id,
                    "Name": event.target.service.device.name
                },
                "ServiceId": event.target.service.uuid,
                "CharacteristicId": event.target.uuid,
                "RawUIntValue": Array.from(byteArray)
            };
            await WebBluetoothFunctions.DotNetReference.invokeMethodAsync("OnNotify", notification);
        }
    }
};
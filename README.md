# Blazor.WebBluetooth

## Installation
1. Install the NuGet package `KeudellCoding.Blazor.WebBluetooth`.
2. Add the following lines to _Imports.razor
```csharp
@using KeudellCoding.Blazor.WebBluetooth
@using KeudellCoding.Blazor.WebBluetooth.Models
```
3. Register Service
  * Option 1: If you are using Blazor WebAssembly, add the following lines to Program.cs
  ```csharp
  using KeudellCoding.Blazor.WebBluetooth;
  
  builder.Services.AddWebBluetooth();
  ```
  * Option 2: If you are using Blazor Server, add the following lines to the Startup.cs in the ConfigureServices function
  ```csharp
  using KeudellCoding.Blazor.WebBluetooth;
  
  services.AddWebBluetooth();
  ```

## Usage Example
```csharp
@page "/"
@inject WebBluetoothControllerService __WebBluetooth

<div>
    @if (isAvailable.HasValue) {
        if (isAvailable == true) {
            <button @onclick="connect">Connect</button>
            <button @onclick="disconnect">Disconnect</button>

            <br />
            
            @if (connectedDevice != null) {
                <span>@connectedDevice.Id</span><br /><span>@connectedDevice.Name</span>
            }
        }
        else {
            <b>Bluetooth not available!</b>
        }
    }
    else {
        <i>Loading Bluetooth...</i>
    }
</div>

@code {
    private bool? isAvailable = null;
    private Device connectedDevice;

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            isAvailable = await __WebBluetooth.IsAvailableAsync();
            StateHasChanged();
        }
    }
    
    private async Task connect() {
        connectedDevice = await __WebBluetooth.RequestDeviceAsync(new RequestDeviceQuery() {
            Filters = new List<Filter> {
                new Filter() {
                    Services = new List<string> {
                        "<<SERVICE_ID_OR_NAME>>"
                    }
                }
            }
        });
        StateHasChanged();
    }
    private async Task disconnect() {
        if (connectedDevice == null) return;
        await __WebBluetooth.DisconnectAsync(connectedDevice);
    }
}
```

## Notes
1. I programmed this project based on [EngstromJimmy](https://github.com/EngstromJimmy/Blazm.Bluetooth) and adapted it for my personal needs. Maybe it will help others too.
2. Currently, WebBluetooth is not yet supported by all browsers. A current implementation status can be found [here](https://github.com/WebBluetoothCG/web-bluetooth/blob/main/implementation-status.md).
3. WebBluetooth is a feature that is still under development. There may still be spontaneous massive changes in the JavaScript API that would result in this project no longer working.

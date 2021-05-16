using Microsoft.Extensions.DependencyInjection;

namespace KeudellCoding.Blazor.WebBluetooth {
    public static class WebBluetoothControllerServiceExtensions {
        public static IServiceCollection AddWebBluetooth(this IServiceCollection services) {
            return services.AddScoped<WebBluetoothControllerService>();
        }
    }
}

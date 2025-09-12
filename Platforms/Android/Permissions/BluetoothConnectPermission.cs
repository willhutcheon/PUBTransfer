using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBTransfer.Platforms.Android.Permissions
{
    public class BluetoothConnectPermission : Microsoft.Maui.ApplicationModel.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new (string, bool)[]
            {
                ("android.permission.BLUETOOTH_CONNECT", true)
            };
    }
}

using System.Runtime.InteropServices;

namespace OutlookNextEvent.App.Widgets;

internal sealed class WidgetProviderComServer : IDisposable
{
    private const uint ClsctxLocalServer = 0x4;
    private const uint RegclsMultipleUse = 0x1;

    private uint _registrationCookie;

    public void Register()
    {
        if (_registrationCookie != 0)
        {
            return;
        }

        var classId = typeof(NextEventsWidgetProvider).GUID;
        Marshal.ThrowExceptionForHR(CoRegisterClassObject(
            classId,
            new WidgetProviderFactory<NextEventsWidgetProvider>(),
            ClsctxLocalServer,
            RegclsMultipleUse,
            out _registrationCookie));
    }

    public void Dispose()
    {
        if (_registrationCookie == 0)
        {
            return;
        }

        CoRevokeClassObject(_registrationCookie);
        _registrationCookie = 0;
    }

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);
}

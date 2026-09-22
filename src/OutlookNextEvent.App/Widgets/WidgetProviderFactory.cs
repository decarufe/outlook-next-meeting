using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace OutlookNextEvent.App.Widgets;

internal static class ComGuids
{
    public const string IClassFactory = "00000001-0000-0000-C000-000000000046";
    public const string IUnknown = "00000000-0000-0000-C000-000000000046";
}

[ComImport]
[ComVisible(false)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid(ComGuids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}

[ComVisible(true)]
internal sealed class WidgetProviderFactory<T> : IClassFactory
    where T : IWidgetProvider, new()
{
    private static readonly Guid IUnknownGuid = Guid.Parse(ComGuids.IUnknown);
    private static readonly Guid IWidgetProviderGuid = typeof(IWidgetProvider).GUID;

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            return ClassENoAggregation;
        }

        if (riid != IUnknownGuid && riid != IWidgetProviderGuid && riid != typeof(T).GUID)
        {
            return ENoInterface;
        }

        ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(new T());
        return 0;
    }

    public int LockServer(bool fLock) => 0;

    private const int ClassENoAggregation = unchecked((int)0x80040110);
    private const int ENoInterface = unchecked((int)0x80004002);
}

using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;

namespace OutlookNextEvent.App.Widgets;

[ComVisible(true)]
[Guid("8F3D7C7B-8D0D-41BF-8D9E-608A70D03E95")]
public sealed class NextEventsWidgetProvider : IWidgetProvider
{
    public void Activate(WidgetContext widgetContext)
    {
        throw new NotImplementedException("Widget activation and refresh will be implemented in the provider work.");
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        throw new NotImplementedException("Initial Adaptive Card updates will be implemented in the provider work.");
    }

    public void Deactivate(string widgetId)
    {
        throw new NotImplementedException("Widget deactivation handling will be implemented in the provider work.");
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        throw new NotImplementedException("Widget deletion handling will be implemented in the provider work.");
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        throw new NotImplementedException("Widget actions will open or focus the companion sign-in window when auth is required.");
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        throw new NotImplementedException("Widget context changes will be handled in the provider work.");
    }
}

using Imagile.Framework.Blazor.ApplicationInsights.Models;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Imagile.Framework.Blazor.ApplicationInsights.Interfaces;

[EditorBrowsable(EditorBrowsableState.Never)]
[Browsable(false)]
public interface IPropertiesPlugin
{
    Task<TelemetryContext> Context();
}

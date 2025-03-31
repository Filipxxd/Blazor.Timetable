using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Timetable.Common.Utilities;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class RenderModeInteractiveServerAttribute : RenderModeAttribute
{
    public override IComponentRenderMode Mode => RenderMode.InteractiveServer;
}

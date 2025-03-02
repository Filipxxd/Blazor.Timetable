using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Timetable.Utilities;

internal sealed class RenderModeInteractiveServer : RenderModeAttribute
{
    public override IComponentRenderMode Mode => RenderMode.InteractiveServer;
}

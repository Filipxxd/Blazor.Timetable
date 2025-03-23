using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using Microsoft.AspNetCore.Components.Web;

namespace Timetable.Components
{
    public partial class TimetableEvent
    {
        private const int MousedownThreshold = 500;
        private Stopwatch _clickStopwatch = new();
        private bool _popoverVisible = false;

        [Parameter] public Guid EventId { get; set; }
        [Parameter] public string Title { get; set; } = default!;
        [Parameter] public int Span { get; set; }
        [Parameter] public bool IsWholeDay { get; set; }
        [Parameter] public RenderFragment DetailTemplate { get; set; } = default!;

        private void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == 0)
            {
                _clickStopwatch.Restart();
            }
        }

        private void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != 0 || !_clickStopwatch.IsRunning) return;
            _clickStopwatch.Stop();
                
            if (_clickStopwatch.ElapsedMilliseconds < MousedownThreshold)
            {
                TogglePopover();
            }
        }

        private void TogglePopover()
        {
            _popoverVisible = !_popoverVisible;
            StateHasChanged();
        }

        private void EditEvent()
        {
            // Edit event logic
        }

        private void DeleteEvent()
        {
            // Delete event logic
        }
    }
}
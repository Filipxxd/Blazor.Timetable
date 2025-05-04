using Microsoft.AspNetCore.Components;
using Blazor.Timetable.Services;

namespace Blazor.Timetable.Components.Shared.Modals;

public partial class ModalContainer
{
    [Inject] private ModalService ModalService { get; set; } = default!;

    protected override void OnInitialized()
    {
        ModalService.OnModalChanged += ModalChanged;
    }

    private void ModalChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private void Close() => ModalService.Close();

    public void Dispose()
    {
        ModalService.OnModalChanged -= ModalChanged;
    }
}
using Microsoft.AspNetCore.Components;
using Timetable.Services;

namespace Timetable.Components.Shared;

public partial class Modal
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
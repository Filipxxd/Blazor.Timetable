﻿using Blazor.Timetable.Services;
using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Components.Modals;

public partial class ModalContainer : IDisposable
{
    [CascadingParameter] internal ModalService ModalService { get; set; } = default!;

    protected override void OnInitialized()
    {
        ModalService.OnModalChanged += ModalChanged;
    }

    private void ModalChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private void Close()
    {
        if (!ModalService.IsClosable) return;

        ModalService.Close();
    }

    public void Dispose()
    {
        ModalService.OnModalChanged -= ModalChanged;
        GC.SuppressFinalize(this);
    }
}
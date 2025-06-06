﻿using Blazor.Timetable.Common.Enums;
using Blazor.Timetable.Models.Actions;
using Blazor.Timetable.Services;
using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Components.Modals;

public partial class ImportModal<TEvent> where TEvent : class
{
    [Inject] private Localizer Localizer { get; set; } = default!;

    public ImportType Type { get; set; } = ImportType.Append;

    public ImportType[] ImportTypes => (ImportType[])Enum.GetValues(typeof(ImportType));

    [Parameter] public EventCallback<ImportAction<TEvent>> OnSubmit { get; set; }
    [Parameter] public IList<TEvent> ImportedEvents { get; set; } = [];

    [CascadingParameter] internal ModalService ModalService { get; set; } = default!;

    private async Task SubmitAsync()
    {
        var p = new ImportAction<TEvent>
        {
            Events = ImportedEvents,
            Type = Type
        };

        await OnSubmit.InvokeAsync(p);

        ModalService.Close();
    }
}
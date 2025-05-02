using Microsoft.AspNetCore.Components;
using Timetable.Common.Enums;
using Timetable.Models.Props;
using Timetable.Services;

namespace Timetable.Components.Shared.Modals;

public partial class ImportModal<TEvent> where TEvent : class
{
    [Inject] ModalService ModalService { get; set; } = default!;

    public ImportType Type { get; set; } = ImportType.Append;

    public ImportType[] ImportTypes => (ImportType[])Enum.GetValues(typeof(ImportType));

    [Parameter] public EventCallback<ImportProps<TEvent>> OnSubmit { get; set; }
    [Parameter] public IList<TEvent> ImportedEvents { get; set; } = [];

    private async Task SubmitAsync()
    {
        var p = new ImportProps<TEvent>
        {
            Events = ImportedEvents,
            Type = Type
        };

        await OnSubmit.InvokeAsync(p);

        ModalService.Close();
    }
}
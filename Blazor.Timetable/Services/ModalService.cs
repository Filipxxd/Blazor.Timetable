using Microsoft.AspNetCore.Components;

namespace Blazor.Timetable.Services;

internal sealed class ModalService
{
    public event Action? OnModalChanged;

    public bool IsOpen { get; private set; }
    public RenderFragment? ModalContent { get; private set; }

    public void Show<TComponent>(IDictionary<string, object> parameters)
    {
        ModalContent = builder =>
        {
            builder.OpenComponent<DynamicComponent>(0);
            builder.AddAttribute(1, "Type", typeof(TComponent));
            if (parameters.Count != 0)
            {
                builder.AddAttribute(2, "Parameters", parameters);
            }
            builder.CloseComponent();
        };
        IsOpen = true;
        NotifyStateChanged();
    }

    public void Close()
    {
        IsOpen = false;
        ModalContent = null;

        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnModalChanged?.Invoke();
}

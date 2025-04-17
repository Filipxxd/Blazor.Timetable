using Microsoft.AspNetCore.Components;

namespace Timetable.Services;

public class ModalService
{
    public event Action? OnModalChanged;

    public bool IsOpen { get; private set; }
    public string? Title { get; private set; }
    public RenderFragment? ModalContent { get; private set; }

    public void Show(string? title, RenderFragment content)
    {
        ModalContent = content;
        IsOpen = true;
        Title = title;
        NotifyStateChanged();
    }

    public void Close()
    {
        IsOpen = false;
        ModalContent = null;
        Title = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnModalChanged?.Invoke();
}

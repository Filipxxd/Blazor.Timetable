# Blazor.Timetable

[![NuGet](https://img.shields.io/nuget/v/Blazor.Timetable)](https://www.nuget.org/packages/Blazor.Timetable) 
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE) 
[![Build Status](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/build-test.yml/badge.svg)](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/build-test.yml/badge.svg)
[![Deploy Status](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/deploy-nuget.yml/badge.svg)](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/deploy-nuget.yml/badge.svg)

A flexible, extensible, and feature-rich timetable/scheduler component for Blazor. It enables you to display, create, update, delete, import/export, and drag-and-drop events in day/week/month views with customization support.


## Table of Contents

1. [Features](#features)  
2. [Getting Started](#getting-started)  
   - [Prerequisites](#prerequisites)  
   - [Installation](#installation)  
   - [Minimal Setup](#minimal-setup)  
3. [Configuration](#configuration)  
   - [Timetable](#timetable)  
   - [Import](#import)   
   - [Export](#export)   
   - [Localization](#localization)   
6. [Advanced](#advanced)  
7. [Contributing](#contributing)  
8. [Acknowledgments](#acknowledgments)
9. [License](#license)  


## Features

- Day, Week, Month views  
- Extensible Create, Edit, Delete 
- Single & Group events
- Drag & Drop support  
- Import & Export functionality
- Additional fields with custom validation
- English and Czech language support
- Customization:
    - Time Range
    - Days
    - Months
    - Extensible or even custom Import & Export implementation

## Getting Started

### Prerequisites

- .NET 8+ SDK  
- Blazor Interactive Project (`WebAssembly` or `Server`)

### Installation

Via NuGet Package Manager:

~~~powershell
Install-Package Blazor.Timetable
~~~

Or via .NET CLI:

~~~bash
dotnet add package Blazor.Timetable
~~~

In your `Program.cs`, add timetable to service container via `AddBlazorTimetable` and allow usage of static files via `UseStaticFiles`:

~~~csharp
using Blazor.Timetable.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ...

builder.Services.AddBlazorTimetable();

var app = builder.Build();

app.UseStaticFiles();

// ...

app.Run();
~~~

Inside your `App.razor` head, add link to blazor stylesheet where `ASSEMBLY_NAME` is your projects name:

~~~html
<link rel="stylesheet" href="{{ASSEMBLY_NAME}}.styles.css"/> 
~~~

### Minimal Setup

Create your event model, which needs to define required props for title (`string`), start and end of the event (`DateTime`) and nullable group identifier (`string?`):

~~~csharp
class Appointment
{
    public string Title { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string? GroupId { get; set; }
    
    // extra properties as needed
}
~~~

Use the `<Timetable>` component inside your razor page:

~~~razor
@page "/example"
@using Blazor.Timetable.Components

<Timetable TEvent=Appointment
           @bind-Events=Appointments
           DateFrom="e => e.From"
           DateTo="e => e.To"
           Title="e => e.Title"
           GroupId="e => e.GroupId" />

@code {
    private List<Appointment> Appointments = [];
}
~~~


## Configuration

Library supports in total 3 different configuration classes, to specify:
  - `TimetableConfig`
  - `ExportConfig`
  - `ImportConfig`

These configuration classes can be supplied as parameters to your timetable component instance.

### TimetableConfig

~~~csharp
class TimetableConfig
{
    public ICollection<Month> Months { get; init; }
    public IList<DayOfWeek> Days { get; init; }
    public TimeOnly TimeFrom { get; init; }
    public TimeOnly TimeTo { get; init; }
    public bool Is24HourFormat { get; init; }
    public DisplayType DefaultDisplayType { get; init; }
}
~~~

1. **Months** - `Month` enum
   - Affects creation & update - possible only for these months
   - Must be within a valid range
   - Must be consecutive
   - Available enum options:
      - `Month.January`
      - `Month.February`
      - `Month.March`
      - `Month.April`
      - `Month.May`
      - `Month.June`
      - `Month.July`
      - `Month.August`
      - `Month.September`
      - `Month.October`
      - `Month.November`
      - `Month.December`

2. **Days** - `DayOfWeek` enum
   - Affects creation & update - possible only for these days
   - Timetable displays only those days (skips those not defined)
   - Must be consecutive
   - Available enum options:
      - `DayOfWeek.Sunday`
      - `DayOfWeek.Monday`
      - `DayOfWeek.Tuesday`
      - `DayOfWeek.Wednesday`
      - `DayOfWeek.Thursday`
      - `DayOfWeek.Friday`
      - `DayOfWeek.Saturday`

3. **Time From & Time To** - `TimeOnly`
   - Affects creation & update.
   - Must be within an hour range of `0` - `23`.
   - Minutes must be in quarters: `0`, `15`, `30`, `45`.
   - `TimeFrom` must be less than `TimeTo`.
   - Special case: `TimeTo` can be `23`:`59` to represent the end of the day.

4. **Is24HourFormat** - `bool`
   - Determines the time display format in grid, modals etc.

5. **DefaultDisplayType** - `DisplayType` enum
   - Sets the initial display type preference.
   - Available enum options:
      - `DisplayType.Day`
      - `DisplayType.Week`
      - `DisplayType.Month`

### ExportConfig  
  ~~~csharp
    public sealed class ImportConfig<TEvent>
      where TEvent : class
    {
        public string[] AllowedExtensions { get; init; }
        public long MaxFileSizeBytes { get; init; }
        public IImportTransformer Transformer { get; init; }
        public required IList<ISelector<TEvent>> Selectors { get; init; }
    }
  ~~~

  - `FileName` (no extension)  
  - `IExportTransformer` (e.g. `CsvTransformer`)  
  - `IList<ISelector<TEvent>>`

### ImportConfig  
  - `string[] AllowedExtensions` (e.g. `["csv"]`)  
  - `long MaxFileSizeBytes`  
  - `IImportTransformer` (e.g. `CsvImportTransformer`)
  - `IList<ISelector<TEvent>>`

### Localization
Library supports localization for English and Czech languages. This setting can only be applied globally when adding the service to the service container using `Localize` method chained onto `AddBlazorTimetable`.

~~~csharp
builder.Services.AddBlazorTimetable().Localize(Language.Czech);
~~~


## Callbacks

Library exposes several callbacks parameters that can be specified to respond to user interactions or state changes. Here are the available callbacks:

### State Change Callbacks
   - `OnPreviousClicked`: Triggered when the user navigates to the previous time period.
   - `OnNextClicked`: Triggered when the user navigates to the next time period.
   - `OnDisplayTypeChanged`: Invoked when the display type is changed. Provides the new display type.
   - `OnChangedToDay`: Invoked when the mode changes to a specific day. Provides the day of week.

### Event Management Callbacks
   - `OnEventCreated`: Triggered when an event is created. Provides the created event instance.
   - `OnGroupEventCreated`: Triggered when a group of events is created. Provides the list of created events.
   - `OnEventChanged`: Triggered when an event is updated. Provides the updated event instance.
   - `OnGroupEventChanged`: Triggered when a group of events is updated. Provides the list of updated events.
   - `OnEventDeleted`: Invoked when an event is deleted. Provides the deleted event instance.
   - `OnGroupEventDeleted`: Invoked when a group of events is deleted. Provides the list of deleted events.


## Additional Event Properties

In scenarios where events have limited occupancy or require additional custom attributes, our system allows for the integration of these properties during the creation or update of events.

### Steps to Add Additional Properties:

1. **Add Selector to AdditionalProps:**
   - Each additional property should have its selector specified and passed to the **Timetable** component via the **AdditionalProps** parameter.

2. **Specify UI in AdditionalFields:**
   - Use the **AdditionalFields** RenderFragment to incorporate the required input components for your additional properties. Predefined input components are available for integration, or you can implement custom input components if needed.

### Predefined Components:

These components facilitate the integration with the library, allowing for input, modification, and validation of events. Below are the predefined components available:

| Component Name | Data Type | Description             |
|----------------|-----------|-------------------------|
| **Input**      | string    | Text input              |
| **InputDateTime** | DateTime | Date and time input     |
| **Dropdown**   | T         | Generic dropdown selector |

**Example Implementation**:  
For instance, if you have a property **Description** in the **Subject** class, its selector can be added to **AdditionalProps**. The **AdditionalFields** RenderFragment would then include an instance of the **Input** component with the relevant parameters. The **context** object, which represents the current event instance, is passed as the **Model** to this component to enable binding of its label.

~~~razor
<Timetable @bind-Events=Subjects
           TEvent=Subject
           Title="e => e.Name"
           DateFrom="e => e.StartTime"
           DateTo="e => e.EndTime"
           GroupId="e => e.GroupId"
           AdditionalProps=@([x => x.Description])>
    <AdditionalFields>
        <Input Model="@context"
               Label="Description"
               Selector="x => x.Description" />
    </AdditionalFields>
</Timetable>
~~~

### Validation

Validation is performed via the **Validate** parameter, where you can specify a method to be executed when the property's value changes and before the event is saved. This method must accept a data type corresponding to the property and return a nullable string with an error message for the UI if the value is invalid, or **null** if the value is valid.

### Custom Input Components

If the predefined components do not provide the required functionality, you can implement custom components. These should extend the **BaseInput** class to utilize the predefined interface for property assignment and validation upon value change.


## Acknowledgments

This project uses the following open-source resources:

- [Tabler Icons](https://tablericons.com/): A set of free and open-source icons.
- [Interact.js](https://interactjs.io/): A JavaScript library for drag and drop, resizing, and multi-touch gestures.


## License

Distributed under the MIT License. See [LICENSE](LICENSE) for details.

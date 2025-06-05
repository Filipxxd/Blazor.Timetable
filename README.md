# Blazor.Timetable

[![NuGet](https://img.shields.io/nuget/v/Blazor.Timetable)](https://www.nuget.org/packages/Blazor.Timetable) 
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE) 
[![Build Status](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/build-test.yml/badge.svg)](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/build-test.yml/badge.svg)
[![Deploy Status](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/deploy-nuget.yml/badge.svg)](https://github.com/Filipxxd/Blazor.Timetable/actions/workflows/deploy-nuget.yml/badge.svg)

A flexible, extensible, and feature-rich timetable/scheduler component for Blazor. 
It enables you to display, create, update, delete, import/export, and drag-and-drop events in day/week/month views with customization support.

## Table of Contents

1. [Features](#features)
2. [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Project Setup](#project-setup)
    - [Minimal Setup](#minimal-setup)
3. [Configuration](#configuration)
    - [TimetableConfig](#timetableconfig)
    - [ExportConfig](#exportconfig)
    - [ImportConfig](#importconfig)
    - [Localization](#localization)
4. [Callbacks](#callbacks)
    - [State Change Callbacks](#state-change-callbacks)
    - [Event Management Callbacks](#event-management-callbacks)
5. [Additional Event Properties](#additional-event-properties)
    - [Steps to Add Additional Properties](#steps-to-add-additional-properties)
    - [Validation](#validation)
    - [Custom Input Components](#custom-input-components)
6. [Properties and Parameters](#properties-and-parameters)
7. [Acknowledgments](#acknowledgments)
8. [License](#license)

## Features
- Day, Week, Month views
- Extensible Create, Edit with custom validation
- Single & Group events
- Drag & Drop support
- Import & Export functionality
- English and Czech language support
- Customization:
    - Day Time Range
    - Days & Months
    - Extensible/Custom Import & Export Implementation

## Getting Started

### Prerequisites

- .NET 8+ SDK and Runtime
- Blazor-friendly IDE like Visual Studio

### Project Setup

This tutorial covers setting up a Blazor Server Interactive project.

To create a new Blazor project named ***BlazorTimetableExample***, open a terminal and execute the following command:

```bash
dotnet new blazor -n BlazorTimetableExample
```

Next, navigate into the project's directory:
```bash
cd BlazorTimetableExample
```

Add the `Blazor.Timetable` package either via NuGet Package Manager (Visual Studio):

~~~powershell
Install-Package Blazor.Timetable
~~~

Or via .NET CLI:

~~~bash
dotnet add package Blazor.Timetable
~~~

In your `Program.cs`, add necessary services to service container using `AddBlazorTimetable` and ensure that static files can be used by invoking `UseStaticFiles` (In .NET 9 use `MapStaticAssets`).

~~~csharp
using BlazorTimetableExample.Components;
using Blazor.Timetable.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorTimetable(); // Add Blazor.Timetable services

var app = builder.Build();

app.UseAntiforgery();

app.UseStaticFiles(); // Enable static files for serving stylesheets

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
~~~

In the `<head>` of your `App.razor` file, add a link to the Blazor stylesheet if not already present:

~~~html
<link rel="stylesheet" href="BlazorTimetableExample.styles.css"/> 
~~~

For .NET 9:
~~~html
<link rel="stylesheet" href="@Assets["BlazorTimetableExample.styles.css"]" />
~~~

### Minimal Setup

This section provides a step-by-step guide to integrating the `Timetable` component into your Blazor Server project.

#### Step 1: Navigate to parent component

Navigate to the `Home.razor` component, or another suitable component of your choice, which will act as the parent for the `Timetable` component. 
This is where you will define the timetable and manage events.

#### Step 2: Define the Appointment Model

Define an `Appointment` class anywhere in your project to represent events.
This model must define properties for the title (`string`), start and end of the event (`DateTime`) and nullable group identifier (`string?`):

```csharp
public class Appointment
{
    public string Title { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string? GroupId { get; set; }
    // Include any additional properties as needed
}
```

#### Step 3: Create a List of your Appointments

Inside the selected component, add `@code` block to initialize a list for `Appointment` objects.
This list will be bound to the `Timetable` component for interactive event management. 
Ensure the collection passed to the `Timetable` is of type `IList<TEvent>`, where `TEvent` is your event model, in this case, `Appointment`.

```csharp
@code {
    public IList<Appointment> Appointments { get; set; } = new List<Appointment>();
}
```

#### Step 4: Integrate the Timetable Component

Within your component, use the `@rendermode InteractiveServer` directive to enable interactivity. 
Then, implement the `Timetable` component, passing in the necessary parameters:
  - `TEvent`: Type of event (`Appointment`) 
  - `@bind-Events`: List of event objects (`Appointments`)
  - `DateFrom` and `DateTo`: Start and end time (`e => e.From` and `e => e.To`)
  - `Title`: Title property of each event displayed in the grid (`e => e.Title`)
  - `GroupId`: Identifier to allow grouping of same events e.g. (`e => e.GroupId`)

```razor
@page "/"

@using Blazor.Timetable.Components
@rendermode InteractiveServer

<Timetable TEvent=Appointment
           @bind-Events=Appointments
           DateFrom="e => e.From"
           DateTo="e => e.To"
           Title="e => e.Title"
           GroupId="e => e.GroupId" />
```

The complete code block of `Home.razor` should resemble the following:

```razor
@page "/"
@using Blazor.Timetable.Components
@rendermode InteractiveServer

<Timetable TEvent=Appointment
           @bind-Events=Appointments
           DateFrom="e => e.From"
           DateTo="e => e.To"
           Title="e => e.Title"
           GroupId="e => e.GroupId" />

@code {
    public IList<Appointment> Appointments { get; set; } = new List<Appointment>();

    public class Appointment
    {
        public string Title { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string? GroupId { get; set; }
    }
}
```

#### Step 5: Run the app

The project is now ready to run. You can start the application using the following command:
```bash
dotnet run
```


## Configuration

The library offers three configuration classes that allow you to customize your timetable component. 
These classes can be passed as parameters to the component instance. 

The available configuration classes are:
  - `TimetableConfig`
  - `ExportConfig`
  - `ImportConfig`

### TimetableConfig

Defines display settings for the timetable.

| Property            | Type                  | Description                                                                                                                  |
|---------------------|-----------------------|------------------------------------------------------------------------------------------------------------------------------|
| `Months`            | `ICollection<Month>`  | Specifies the months to display. Must be within a valid range and consecutive. Options: `Month.January` to `Month.December`. |
| `Days`              | `IList<DayOfWeek>`    | Specifies which days to display. Must be consecutive. Options: `DayOfWeek.Sunday` to `DayOfWeek.Saturday`.                   |
| `TimeFrom`          | `TimeOnly`            | Starting time, with hours from 0-23 and minutes in quarters (0, 15, 30, 45).                                                 |
| `TimeTo`            | `TimeOnly`            | Ending time, must be greater than `TimeFrom`. Special case: `23:59` for end of day.                                          |
| `Is24HourFormat`    | `bool`                | Determines if the timetable uses a 24-hour time format.                                                                      |
| `DefaultDisplayType`| `DisplayType`         | Sets the initial display view. Options: `DisplayType.Day`, `DisplayType.Week`, `DisplayType.Month`.                          |

Usage:
```razor
<Timetable TEvent=Appointment
           @bind-Events=Appointments
           DateFrom="e => e.From"
           DateTo="e => e.To"
           Title="e => e.Title"
           GroupId="e => e.GroupId"
           
           TimetableConfig="new TimetableConfig {
                DefaultDisplayType = DisplayType.Week,
                Days = new List<DayOfWeek> { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, 
                                             DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                TimeFrom = new TimeOnly(8, 0),
                TimeTo = new TimeOnly(16, 0),
                Is24HourFormat = true
           }" />
```

### ExportConfig  

Defines settings for exporting timetable data.

| Property     | Type                            | Description                                                                                                                 |
|--------------|---------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `FileName`   | `string`                        | The file name without the extension.                                                                                        |
| `Transformer`| `IExportTransformer`            | Transformer used for exporting, default is `CsvTransformer`. You can supply your own implementation of `IExportTransformer` |
| `Selectors`  | `IList<ISelector<TEvent>>`      | Defines the list of properties to be exported and how.                                                                      |

Usage:
```razor
<Timetable TEvent=Appointment
           @bind-Events=Appointments
           DateFrom="e => e.From"
           DateTo="e => e.To"
           Title="e => e.Title"
           GroupId="e => e.GroupId"
           
           ExportConfig=@(new ExportConfig<Appointment>{
                FileName = "FileName",
                Transformer = new CsvTransformer(),
                Selectors = new List<ISelector<Appointment>> {
                   new Selector<Appointment, DateTime>("DateFrom", e => e.From),
                   new Selector<Appointment, DateTime>("DateTo", e => e.To),
                   new Selector<Appointment, string>("Title", e => e.Title!),
                   new Selector<Appointment, string>("GroupIdentifier", e => e.GroupId)
                }
           }) />
```

You can supply `toStringConverter` argument with method to format the value into its `string`. 
For example converting the `DateTo` to specific format:
```csharp
new Selector<TEvent, DateTime>("DateTo", e => e.To, toStringConverter: e => e.ToString("dd__MM__yyyy")),
```

### ImportConfig  

Defines settings for importing timetable data.

| Property           | Type                            | Description                                                                   |
|--------------------|---------------------------------|-------------------------------------------------------------------------------|
| `AllowedExtensions`| `string[]`                      | Allowed file extensions. Note that they must be supported by your transformer |
| `MaxFileSizeBytes` | `long`                          | Maximum allowed file size in bytes.                                           |
| `Transformer`      | `IImportTransformer`            | Transformer used for importing, default is `CsvImportTransformer`.            |
| `Selectors`        | `IList<ISelector<TEvent>>`      | Defines properties to be imported and how.                                    |

Usage:
```razor
<Timetable TEvent=Appointment
           @bind-Events=Appointments
           DateFrom="e => e.From"
           DateTo="e => e.To"
           Title="e => e.Title"
           GroupId="e => e.GroupId"
           
           ImportConfig=@(new ImportConfig<Appointment>{
                AllowedExtensions = new string[]{ "csv", "txt" },
                MaxFileSizeBytes = 10_000_000,
                Transformer = new CsvImportTransformer(),
                Selectors = new List<ISelector<Appointment>> {
                   new Selector<Appointment, DateTime>("DateFrom", e => e.From),
                   new Selector<Appointment, DateTime>("DateTo", e => e.To),
                   new Selector<Appointment, string>("Title", e => e.Title!),
                   new Selector<Appointment, string>("GroupIdentifier", e => e.GroupId)
                }
           }) />
```

You can supply `parser` argument with method to format the value from the string to its property datatype.
For example converting string to the date time:
```csharp
new Selector<TEvent, DateTime>("DateTo", DateTo, parser: raw => DateTime.ParseExact(raw, "dd__MM__yyyy", CultureInfo.InvariantCulture)),
```

### Localization
The library supports English and Czech localizations with default English. 
Localization must be applied globally when adding services to the service container. 
Use the `Localize` method chained onto `AddBlazorTimetable`:

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

Example usage:
```razor
<Timetable TEvent=Appointment
           @bind-Events=Appointments
           DateFrom="e => e.From"
           DateTo="e => e.To"
           Title="e => e.Title"
           GroupId="e => e.GroupId" 

           OnEventChanged=HandleEventChanged />

@code{
    public void HandleEventChanged(Appointment appointment){
        // TODO: Update the appointment in the database or other action
    }
}
```

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

```razor
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
```

### Validation

Validation is performed via the `Validate` parameter on the input component, where you can specify a method to be executed when the property's value changes and before the event is saved. 
This method must accept a data type corresponding to the property and return a nullable string `string?` with an error message for the UI if the value is invalid, or `null` if the value is valid.

```razor
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
               Selector="x => x.Description"
               Validate="ValidationExample" />
    </AdditionalFields>
</Timetable>

@code{
    private string? ValidationExample(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "Cannot be empty";

        if (value.Length > 100)
            return "Maximum length is 100";

        return null; // No error -> valid
    }
}
```

### Custom Input Components

If the predefined components do not provide the required functionality, you can implement custom components. 
These must inherit from the `BaseInput` class to utilize the predefined interface for property assignment and validation upon value change.

## Properties and Parameters

| Name                | Type                                | Description                                                                  |
|---------------------|-------------------------------------|------------------------------------------------------------------------------|
| `Id`                | `Guid`                              | A unique identifier for the instance.                                        |
| `ErrorMessage`      | `string?`                           | A message indicating validation errors retrieved from the Validate, if any.  |
| `HasError`          | `bool`                              | Indicates if there is a validation error e.g. if ErrorMessage is null or not.|


For example to create a custom input component for `int` datatype:

```razor
@using Blazor.Timetable.Components.Shared.Forms
@inherits BaseInput<Appointment, int?>

<div>
    <input type="number" @bind=BindProperty id=@Id />
    <label for=@Id>@Label</label>
</div>

@if (HasError)
{
    <span class="error">@ErrorMessage</span>
}
```

Now you can use this custom input component in the `AdditionalFields` section of the `Timetable` component.
For example to allow supplying of **Maximum Occupancy** on the event:

```razor
<Timetable @bind-Events=Subjects
           TEvent=Subject
           Title="e => e.Name"
           DateFrom="e => e.StartTime"
           DateTo="e => e.EndTime"
           GroupId="e => e.GroupId"
           AdditionalProps=@([x => x.MaxOccupancy])>
    <AdditionalFields>
        <Input Model="@context"
               Label="Maximum Occupancy"
               Selector="x => x.MaxOccupancy" 
               Validate="ValidationExample" />
    </AdditionalFields>
</Timetable>

@code{
    private string? ValidationExample(int? value)
    {
        if (value <= 0)
            return "At least 1";

        if (value > 100)
            return "Maximum is 100";

        return null;
    }

    public class Appointment
    {
        public string Title { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string? GroupId { get; set; }

        public int MaxOccupancy { get; set; }
    }
}
```

## Acknowledgments

This project uses the following open-source resources:

- [Tabler Icons](https://tablericons.com/): A set of free and open-source icons.
- [Interact.js](https://interactjs.io/): A JavaScript library for drag and drop, resizing, and multi-touch gestures.


## License

Distributed under the MIT License. See [LICENSE](LICENSE) for details.

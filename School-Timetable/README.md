# School Timetable Library

This README provides the necessary steps to use the `School-Timetable` library in your project.

## Prerequisites

- .NET 8.0 SDK or later
- A Blazor project

## Installation

1. **Add the `School-Timetable` library to your project:**

    TODO

2. **Include the necessary services and middleware:**

   Update your `Program.cs` file to include the `School-Timetable` services and middleware:

   ```csharp
   using School_Timetable;
   using Web.Components;

   var builder = WebApplication.CreateBuilder(args);

   builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

   builder.Services.AddSchoolTimetable(); // Required

   var app = builder.Build();

   app.UseStaticFiles(); // Required

   app.MapRazorComponents<App>()
       .AddInteractiveServerRenderMode();

   await app.RunAsync();
   ```

3. **Update your `App.razor` file:**

   Ensure your `App.razor` file includes the necessary script and link references:

   ```razor
   <!DOCTYPE html>
   <html lang="en">

   <head>
       <meta charset="utf-8"/>
       <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
       <base href="/"/>
       <link rel="stylesheet" href="Web.styles.css"/>
       <HeadOutlet/>
   </head>

   <body>
   <Routes/>
   <script src="_framework/blazor.web.js"></script>
   <script src="_content/School-Timetable/timetable.js"></script>
   </body>

   </html>
   ```

## Usage

After completing the above steps, you can start using the `School-Timetable` library in your Blazor project. Ensure that your components and pages are correctly set up to utilize the features provided by the library.

## Additional Configuration

If you need to customize the library further, you can modify the `ServiceExtensions.cs` file or other parts of the library as needed.

## Support

For any issues or questions, please refer to the documentation or contact the library maintainers.
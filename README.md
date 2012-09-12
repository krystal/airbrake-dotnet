#airbrake-dotnet

This is a .NET library which allows you to report exceptions using the Airbrake API. aTech Media uses this
to report errors to projects in [Codebase](http://codebasehq.com).

Once included in a project, a call to report an exception can be placed in any "Catch" block or used as a
global error reporting tool.

##Installing

Include the 'airbrake' project in your solution and add a reference to it. Alternatively, compile it then
include and reference the DLL.

Next include the namespace in the file in which you are handling your exceptions:

C#
```cs
using airbrake;
```

VB.NET
```vb
Imports airbrake
```

##Usage

To use airbrake-dotnet, you will need an API key and some information about the endpoint.

###Step 1 - Initialisation

The initialiser for the class takes the following parameters:

*bool ssl - Should connections to the endpoint use HTTPS?
*string host - The hostname of the endpoint
*string path - Path to the Airbrake API
*string apikey - The API key for your applications
*string environment - An environment name to identify this application/environment

If you are reporting exceptions to Codebase, you might initialise the library with the following code:

```cs
ExceptionHandler handler = new ExceptionHandler(true, "exceptions.codebasehq.com", "/notifier_api/v2/notices", "MY-API-KEY", "WindowsProduction");
```

###Step 2 - Reporting errors

Now that the error handler is ready to use, you can report errors like so:

```cs
handler.Send(ex)
```

##Full Example

```cs
public bool doSomething()
{
    try
    {
        //My risky code here
        return true;
    }
    catch (Exception ex)
    {
        ExceptionHandler handler = new ExceptionHandler(true, "exceptions.codebasehq.com", "/notifier_api/v2/notices", "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX", "WindowsProduction");
        handler.Send(ex);
        return false;
    }
}
```
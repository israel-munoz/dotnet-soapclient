# SOAPClientService

Simple client for SOAP requests.

The `SOAPClient` class allows easy remote calls to web services
through SOAP protocol.

### Methods

#### `Post(string method, object data = null)`
Makes a POST request to the defined method in the configured
server.

#### `Post<T>(string method, object data = null)`
Makes a POST request to the defined method in the configured
server and returns the result as an object of the defined type.
Returns an object of type `T` with the result retrieved from
the SOAP response.

#### `PostAsync(string method, object data = null)`
Makes an asynchronous POST request to the defined method in the
configured server.

#### `PostAsync<T>(string method, object data = null)`
Makes an asynchronous POST request to the defined method in the
configured server and returns the result as an object of the
defined type. Returns an object of type `T` with the result
retrieved from the SOAP response.

#### Parameters

**`T`**
Type of the response object returned.

**`method`**
Name of the web method inside the web service request.

**`data`**
Parameters to send in the request message (optional).

### Requirements
This library requires System.Net.Http 4.0 or above, which can be
found in the .NET assemblies or downloaded through
[Nuget](https://www.nuget.org/packages/System.Net.Http/).

### Example
``` C#
using (SOAPClient client = new SOAPClient(
    "https://www.w3schools.com/xml/tempconvert.asmx",
    "https://www.w3schools.com/xml/"))
{
    string fahrenheit = client.PostAsync<string>(
        "CelsiusToFahrenheit",
        new
        {
            Celsius = celsius
        }).Result;
    return fahrenheit;
}
```

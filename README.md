# SOAPClientService

Simple client for SOAP requests.

The `SOAPClient` class allows easy remote calls to web services
through SOAP protocol.

### Methods

### `PostAsync\<T\>(string method, object data = null)`
Makes an asynchronous POST request to the defined method in
the server configured and returns the result as an object of
the defined type.

The `T` type is optional. If not set, the method won't return
anything.

##### `T`
Type of the response object returned.
##### `method`
Name of the web method inside the web service request.
##### `data`
Parameters to send in the request message (optional).

The method returns an object of the defined type `T` with the
result retrieved from the SOAP response.

#### Example
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

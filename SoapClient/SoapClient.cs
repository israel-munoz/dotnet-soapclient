namespace SoapClientService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    /// <summary>
    /// Client to make SOAP requests.
    /// </summary>
    public class SoapClient : IDisposable
    {
        /// <summary>
        /// HTTP client to make requests.
        /// </summary>
        private readonly HttpClient _client;
        /// <summary>
        /// <see cref="String"/> with the URL to the web service.
        /// </summary>
        private readonly string _serviceUrl;
        /// <summary>
        /// <see cref="String"/> with the web service namespace.
        /// </summary>
        private readonly string _serviceNamespace;

        /// <summary>
        /// Initializes a SOAP client pointing to the defined service.
        /// </summary>
        /// <param name="serviceUrl">
        /// <see cref="String"/> with the URL to the web service.
        /// </param>
        /// <param name="serviceNamespace">
        /// <see cref="String"/> with the web service namespace.
        /// </param>
        /// <remarks>
        /// This constructor sets the service properties and initializes
        /// the HTTP client with the parameters required for calling
        /// the service methods.
        /// </remarks>
        public SoapClient(string serviceUrl, string serviceNamespace)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/xml"));
            _serviceUrl = serviceUrl;
            _serviceNamespace = serviceNamespace;
        }

        /// <summary>
        /// Sets the SOAP action to the HTTP client.
        /// </summary>
        /// <param name="method">
        /// Name of the web service action as <see cref="String"/>.
        /// </param>
        /// <remarks>
        /// It sets the SOAPAction HTTP header to the client for the web method
        /// which will be requested.
        /// </remarks>
        private void SetSoapAction(string method)
        {
            const string header = "SOAPAction";
            if (_client.DefaultRequestHeaders.Contains(header))
            {
                _client.DefaultRequestHeaders.Remove(header);
            }
            _client.DefaultRequestHeaders.Add(header,
                $"{_serviceNamespace}{(_serviceNamespace.EndsWith("/") ? "" : "/")}{method}");
        }

        /// <summary>
        /// Serializes an object into XML.
        /// </summary>
        /// <param name="data">
        /// Object to be serialized.
        /// </param>
        /// <returns>
        /// Returns an <see cref="XElement"/> with the object properties as
        /// XML nodes.
        /// </returns>
        /// <remarks>
        /// The data object is converted to an XML node, and its children
        /// are returned as a nodes collection.
        /// </remarks>
        private IEnumerable<XElement> SerializeData(object data)
        {
            XElement xml = data?.ToXml(_serviceNamespace);
            return xml?.Elements();
        }

        /// <summary>
        /// Gets the error content inside an XML content.
        /// </summary>
        /// <param name="xml">
        /// XML <see cref="String"/> which may contain the error.
        /// </param>
        /// <returns>
        /// Returns a <see cref="SoapError"/> with the error detail.
        /// </returns>
        /// <remarks>
        /// If the XML content returned by a SOAP request contains an error,
        /// this method returns it parsed into a <see cref="SoapError"/>
        /// object.
        /// It searches for the error in the different namespaces it can come.
        /// In ASP.NET SOAP v1 responses, the children of the Fault element
        /// don't have a namespace, so this method looks for them with and
        /// without it.
        /// </remarks>
        private SoapError GetError(string xml)
        {
            XDocument xmlDoc = XDocument.Parse(xml);
            XNamespace xmlns = "http://schemas.xmlsoap.org/soap/envelope/";
            var fault = xmlDoc.Descendants(xmlns + "Fault").FirstOrDefault();
            if (fault != null)
            {
                return new SoapError
                {
                    Code = fault.Element(xmlns + "faultcode")?.Value ??
                           fault.Element("faultcode")?.Value,
                    Message =
                        fault.Element(xmlns + "faultstring")?.Value ??
                        fault.Element("faultstring")?.Value,
                    Detail =
                        fault.Element(xmlns + "detail")?.Value ??
                        fault.Element("detail")?.Value
                };
            }

            xmlns = "http://www.w3.org/2003/05/soap-envelope";
            fault = xmlDoc.Descendants(xmlns + "Fault").FirstOrDefault();
            if (fault != null)
            {
                return new SoapError
                {
                    Code = fault.Element(xmlns + "Code")?.Value,
                    Message = fault.Element(xmlns + "Reason")?.Value,
                    Detail = fault.Element(xmlns + "Detail")?.Value
                };
            }

            return null;
        }

        /// <summary>
        /// Deserializes an XML content into an object of type <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// <see cref="Type"/> for the deserialized result.
        /// </typeparam>
        /// <param name="nodeName">
        /// Name of the element inside the XML content to deserialize.
        /// </param>
        /// <param name="xml">
        /// XML content as <see cref="String"/> to deserialize.
        /// </param>
        /// <param name="xmlns">
        /// XML namespace.
        /// </param>
        /// <returns>
        /// Returns an object of type <see cref="T"/> with the deserialized
        /// content from the XML input.
        /// </returns>
        private T DeserializeData<T>(string nodeName, string xml, string xmlns)
        {
            XElement xelement = XElement.Parse(xml);
            return xelement.ToObject<T>(nodeName, xmlns);
        }


        /// <summary>
        /// Makes an asynchronous POST request to the defined method in the
        /// configured server and returns the result as an object of the
        /// defined type.
        /// </summary>
        /// <typeparam name="T">
        /// <see cref="Type"/> of the response object returned.
        /// </typeparam>
        /// <param name="method">
        /// Name of the web method inside the web service request.
        /// </param>
        /// <param name="data">
        /// Parameters to send in the request message.
        /// </param>
        /// <returns>
        /// Returns an object of type <see cref="T"/> with the result
        /// retrieved from the SOAP response.
        /// </returns>
        public async Task<T> PostAsync<T>(string method, object data = null)
        {
            SetSoapAction(method);
            XNamespace xmlns = _serviceNamespace;
            XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
            var xmlRequest = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(soap + "Envelope",
                    new XElement(soap + "Body",
                        new XElement(xmlns + method,
                            SerializeData(data)
                        )
                    )
                )
            ).ToString();

            var response = await _client.PostAsync(_serviceUrl,
                new StringContent(xmlRequest, Encoding.UTF8, "text/xml"));

            string xmlResult = await response.Content.ReadAsStringAsync();

            SoapError error = GetError(xmlResult);

            if (error != null)
            {
                throw new HttpRequestException(error.Message);
            }

            T result = DeserializeData<T>(
                method + "Result",
                xmlResult,
                _serviceNamespace);

            return result;
        }

        /// <summary>
        /// Makes an asynchronous POST request to the defined method in the
        /// configured server.
        /// </summary>
        /// <param name="method">
        /// Name of the web method inside the web service request.
        /// </param>
        /// <param name="data">
        /// Parameters to send in the request message.
        /// </param>
        public async Task PostAsync(string method, object data = null)
        {
            await PostAsync<object>(method, data);
        }

        /// <summary>
        /// Makes a POST request to the defined method in the configured
        /// server and returns the result as an object of the defined type.
        /// </summary>
        /// <typeparam name="T">
        /// <see cref="Type"/> of the response object returned.
        /// </typeparam>
        /// <param name="method">
        /// Name of the web method inside the web service request.
        /// </param>
        /// <param name="data">
        /// Parameters to send in the request message.
        /// </param>
        /// <returns>
        /// Returns an object of type <see cref="T"/> with the result
        /// retrieved from the SOAP response.
        /// </returns>
        public T Post<T>(string method, object data = null)
        {
            return PostAsync<T>(method, data).Result;
        }

        /// <summary>
        /// Makes a POST request to the defined method in the configured
        /// server.
        /// </summary>
        /// <param name="method">
        /// Name of the web method inside the web service request.
        /// </param>
        /// <param name="data">
        /// Parameters to send in the request message.
        /// </param>
        public void Post(string method, object data = null)
        {
            PostAsync(method, data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed
        /// resources used by the <see cref="HttpMessageInvoker"/>.
        /// </summary>
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}

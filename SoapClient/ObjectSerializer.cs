namespace SoapClientService
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    /// <summary>
    /// Serialize/Deserialize extension methods.
    /// </summary>
    public static class SerializerExtensions
    {
        /// <summary>
        /// Data types to be handled as simple types.
        /// </summary>
        private static readonly Type[] WriteTypes = {
            typeof(string), typeof(DateTime), typeof(Enum), typeof(decimal),
            typeof(Guid)
        };

        /// <summary>
        /// Checks if the parameter is a primitive type for a SOAP request.
        /// </summary>
        /// <param name="type">
        /// Type to check.
        /// </param>
        /// <returns>
        /// Returns a <see cref="Boolean"/> which indicates if the type
        /// is simple.
        /// </returns>
        private static bool IsSimpleType(this Type type)
        {
            return type.IsPrimitive || WriteTypes.Contains(type);
        }

        /// <summary>
        /// Gets the type name, as required for the SOAP protocol.
        /// Required for arrays of primitive types.
        /// </summary>
        /// <param name="type">
        /// Type to get the name from.
        /// </param>
        /// <returns>
        /// Returns a <see cref="string"/> with the type name.
        /// If it's a primitive type, it returns the name as
        /// defined by the SOAP standard.
        /// </returns>
        private static string GetTypeName(this Type type)
        {
            if (type == typeof(bool))
                return "boolean";
            if (type == typeof(short) ||
                type == typeof(int) ||
                type == typeof(long))
                return "int";
            if (type == typeof(byte))
                return "byte";
            if (type == typeof(DateTime))
                return "dateTime";
            if (type == typeof(double) ||
                type == typeof(decimal) ||
                type == typeof(float))
                return "double";
            if (type == typeof(string))
                return "string";
            return type.Name;
        }

        /// <summary>
        /// Creates an <see cref="XElement"/> based based on the element name
        /// and namespace if set, with the optional content defined.
        /// </summary>
        /// <param name="xmlNamespace">
        /// Optional <see cref="XNamespace"/>, if needed to set in the resulting
        /// element.
        /// </param>
        /// <param name="elementName">
        /// A <see cref="String"/> with the resulting element name.
        /// </param>
        /// <param name="content">
        /// Optional element content. It will be added as the element's value.
        /// </param>
        /// <returns>
        /// Returns an <see cref="XElement"/> with the parameters defined.
        /// </returns>
        private static XElement CreateElement(
            XNamespace xmlNamespace,
            string elementName,
            object content = null)
        {
            return new XElement(xmlNamespace == null
                    ? elementName
                    : xmlNamespace + elementName,
                content is DateTime ? $"{content:o}" : content);
        }

        /// <summary>
        /// Creates an <see cref="XElement"/> based on an array.
        /// </summary>
        /// <param name="info">
        /// <see cref="PropertyInfo"/> object with the resulting
        /// element properties.
        /// </param>
        /// <param name="input">
        /// <see cref="Array"/> of elements to set as the element's children.
        /// </param>
        /// <param name="xmlNamespace">
        /// Optional <see cref="XNamespace"/> for the resulting element.
        /// </param>
        /// <returns>
        /// Returns an <see cref="XElement"/> with the array items as
        /// children of it.
        /// </returns>
        private static XElement GetArrayElement(
            PropertyInfo info,
            Array input,
            XNamespace xmlNamespace = null)
        {
            var name = XmlConvert.EncodeName(info.Name) ?? "Object";
            XElement rootElement = CreateElement(xmlNamespace, name);
            var arrayCount = input?.GetLength(0) ?? 0;
            for (int i = 0; i < arrayCount; i += 1)
            {
                var val = input?.GetValue(i);
                var typeName = GetTypeName(val.GetType());
                XElement childElement = val.GetType().IsSimpleType()
                    ? CreateElement(xmlNamespace, $"{typeName}", val)
                    : val.ToXml(typeName, xmlNamespace);
                rootElement.Add(childElement);
            }

            return rootElement;
        }

        /// <summary>
        /// Serializes the input object to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="input">
        /// Object to convert to XML.
        /// </param>
        /// <param name="element">
        /// Name of the resulting element as <see cref="String"/>.
        /// If null or empty, the name will be set based on the input type.
        /// </param>
        /// <param name="xmlNamespace">
        /// Optional <see cref="XNamespace"/>.
        /// </param>
        /// <returns>
        /// Returns an <see cref="XElement"/> based on the input.
        /// </returns>
        private static XElement ToXml(
            this object input,
            string element,
            XNamespace xmlNamespace = null)
        {
            if (input == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(element))
            {
                string name = input.GetType().Name;
                element = name.Contains("AnonymousType")
                    ? "Object"
                    : name;
            }

            element = XmlConvert.EncodeName(element);
            var ret = CreateElement(xmlNamespace, element);

            var type = input.GetType();
            var props = type.GetProperties();

            var elements = props.Select(p =>
            {
                var pType =
                    Nullable.GetUnderlyingType(p.PropertyType) ??
                    p.PropertyType;
                var name = XmlConvert.EncodeName(p.Name);
                var val =
                    pType.IsArray ? "array" : p.GetValue(input, null);
                var value = pType.IsArray
                    ? GetArrayElement(p,
                        (Array)p.GetValue(input, null),
                        xmlNamespace)
                    : pType.IsSimpleType() || pType.IsEnum
                        ? CreateElement(xmlNamespace, name, val)
                        : val.ToXml(name, xmlNamespace);
                return value;
            })
                .Where(v => v != null);

            ret.Add(elements);

            return ret;
        }



        /// <summary>
        /// Serializes an object to XML as required by the SOAP protocol.
        /// </summary>
        /// <param name="input">
        /// Object to convert to XML.
        /// </param>
        /// <param name="xmlNamespace">
        /// Optional <see cref="XNamespace"/>.
        /// </param>
        /// <returns>
        /// Returns an <see cref="XElement"/> based on the input.
        /// </returns>
        public static XElement ToXml(this object input, XNamespace xmlNamespace = null)
        {
            return input.ToXml(null, xmlNamespace);
        }

        /// <summary>
        /// Deserializes an XML content into an object of the required type.
        /// </summary>
        /// <typeparam name="T">
        /// <see cref="Type"/> for the deserialized result.
        /// </typeparam>
        /// <param name="input">
        /// <see cref="XElement"/> with the content to deserialized.
        /// </param>
        /// <param name="elementName">
        /// Element's name inside the <paramref name="input"/>, in case
        /// the object to deserialize is inside the XML content.
        /// If not set. The whole XML input will be deserialized into the
        /// type requested.
        /// </param>
        /// <param name="xmlNamespace">
        /// <see cref="XNamespace"/> for the XML content. 
        /// </param>
        /// <returns>
        /// Returns an object of type <see cref="T"/> with the deserialized
        /// content from the XML input.
        /// </returns>
        public static T ToObject<T>(
            this XElement input,
            string elementName = null,
            string xmlNamespace = null)
        {
            T result;

            XElement xml;

            if (string.IsNullOrEmpty(elementName))
            {
                xml = input;
                elementName = input.Name.LocalName;
            }
            else
            {
                xml = input
                    .Descendants(string.IsNullOrEmpty(xmlNamespace)
                        ? elementName
                        : (XNamespace) xmlNamespace + elementName)
                    .FirstOrDefault();
            }

            if (xml == null)
            {
                result = default(T);
            }
            else
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T),
                    new XmlRootAttribute
                    {
                        ElementName = elementName,
                        Namespace = xmlNamespace
                    });

                result = (T)serializer.Deserialize(xml.CreateReader());
            }

            return result;
        }
    }
}

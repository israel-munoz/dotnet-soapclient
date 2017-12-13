namespace SoapClientTests
{
    using System;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SoapClientService;

    public class DataTestModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }

    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void SerializeDataTest()
        {
            XElement expected =
                XElement.Parse("<Object><value>asdf</value></Object>");
            var data = new
            {
                value = "asdf"
            };
            XElement result = data.ToXml();
            Assert.AreEqual(expected.ToString(), result.ToString());
        }

        [TestMethod]
        public void SerializeModelTest()
        {
            XElement expected =
                XElement.Parse(
                    "<DataTestModel><Id>1</Id><Name>John Smith</Name><Date>2005-10-23T12:00:00.0000000</Date></DataTestModel>");
            var data = new DataTestModel()
            {
                Id = 1,
                Name = "John Smith",
                Date = new DateTime(2005, 10, 23, 12, 0, 0)
            };
            XElement result = data.ToXml();
            Assert.AreEqual(expected.ToString(), result.ToString());
        }

        [TestMethod]
        public void SerializeArrayTest()
        {
            XElement expected =
                XElement.Parse(
                    "<Object><array><int>1</int><int>2</int><int>3</int><int>4</int></array></Object>");
            var data = new
            {
                array = new[] {1, 2, 3, 4}
            };
            XElement result = data.ToXml();
            Assert.AreEqual(expected.ToString(), result.ToString());
        }

        [TestMethod]
        public void SerializeWithNamespaceTest()
        {
            XElement expected =
                XElement.Parse(
                    "<DataTestModel xmlns=\"http://tempuri.org\"><Id>1</Id><Name>John Smith</Name><Date>2005-10-23T12:00:00.0000000</Date></DataTestModel>");
            var data = new DataTestModel()
            {
                Id = 1,
                Name = "John Smith",
                Date = new DateTime(2005, 10, 23, 12, 0, 0)
            };
            XElement result = data.ToXml("http://tempuri.org");
            Assert.AreEqual(expected.ToString(), result.ToString());
        }

        [TestMethod]
        public void DeserializeXmlTest()
        {
            DataTestModel expected = new DataTestModel
            {
                Id = 1,
                Name = "John Smith",
                Date = new DateTime(2005, 10, 23, 12, 0, 0)
            };
            var data = XElement.Parse(
                "<data><Id>1</Id><Name>John Smith</Name><Date>2005-10-23T12:00:00.0000000</Date></data>");
            DataTestModel result = data.ToObject<DataTestModel>();
            Assert.AreEqual(expected.Id, result.Id);
            Assert.AreEqual(expected.Name, result.Name);
            Assert.AreEqual(expected.Date, result.Date);
        }

        [TestMethod]
        public void DeserializeInnerNodeTest()
        {
            DataTestModel expected = new DataTestModel
            {
                Id = 1,
                Name = "John Smith",
                Date = new DateTime(2005, 10, 23, 12, 0, 0)
            };
            var data = XElement.Parse(
                "<data><model><Id>1</Id><Name>John Smith</Name><Date>2005-10-23T12:00:00.0000000</Date></model></data>");
            DataTestModel result = data.ToObject<DataTestModel>("model");
            Assert.IsNotNull(result);
            Assert.AreEqual(expected.Id, result.Id);
            Assert.AreEqual(expected.Name, result.Name);
            Assert.AreEqual(expected.Date, result.Date);
        }

        [TestMethod]
        public void DeserializeWithNamespaceTest()
        {
            DataTestModel expected = new DataTestModel
            {
                Id = 1,
                Name = "John Smith",
                Date = new DateTime(2005, 10, 23, 12, 0, 0)
            };
            var data = XElement.Parse(
                "<data xmlns=\"http://tempuri.org\"><model><Id>1</Id><Name>John Smith</Name><Date>2005-10-23T12:00:00.0000000</Date></model></data>");
            DataTestModel result = data.ToObject<DataTestModel>("model", "http://tempuri.org");
            Assert.IsNotNull(result);
            Assert.AreEqual(expected.Id, result.Id);
            Assert.AreEqual(expected.Name, result.Name);
            Assert.AreEqual(expected.Date, result.Date);
        }

        [TestMethod]
        public void DeserializeWrongNodeTest()
        {
            var data = XElement.Parse(
                "<data><model><Id>1</Id><Name>John Smith</Name><Date>2005-10-23T12:00:00.0000000</Date></model></data>");
            DataTestModel result = data.ToObject<DataTestModel>("wrongNode");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void DeserializeWrongNamespaceTest()
        {
            var data = XElement.Parse(
                "<data xmlns=\"http://tempuri.org\"><model><Id>1</Id><Name>John Smith</Name><Date>2005-10-23T12:00:00.0000000</Date></model></data>");
            DataTestModel result =
                data.ToObject<DataTestModel>("model",
                    "http://wrongnamespace.org");
            Assert.IsNull(result);
        }
    }
}

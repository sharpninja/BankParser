using System;

namespace PaytonByrd
{

    /// <summary>
    /// Summary description for
    /// SelfDocumentingExceptionUnitTests
    /// </summary>
    [TestClass]
    public class SelfDocumentingExceptionUnitTests
    {
        public SelfDocumentingExceptionUnitTests()
        {
        }

        [TestMethod]
        public void InstantiateWithData()
        {
            string strReturn = null;
            SelfDocumentingException objException = new SelfDocumentingException("Test.");
            objException.Add("Key 1", "Data 1");
            objException.Add("Key 2", "Data 2");
            Assert.AreEqual(objException.Count, 2);
            Assert.AreEqual(objException["Key 1"], "Data 1");
            Assert.AreEqual(objException["Key 2"], "Data 2");
            strReturn = objException.ToString();
            Console.WriteLine(strReturn);
        }

        [TestMethod]
        public void Merge()
        {
            SelfDocumentingException objException = new SelfDocumentingException("Test.");
            objException.Add("Key 1", "Data 1");
            objException.Add("Key 2", "Data 2");
            SelfDocumentingException objToMerge = new SelfDocumentingException("Test.");
            objToMerge.Add("Key 1", "Data 1");
            objToMerge.Add("Key 2", "Data 2");
            objException.Merge(objToMerge);
            Console.WriteLine(objException.ToString());
        }

        [TestMethod]
        public void AddObject()
        {
            Uri objUri = new Uri("http://www.ittoolbox.com");
            SelfDocumentingException objException = new SelfDocumentingException("Test");
            objException.AddObject("IT Toolbox", objUri);
            Console.WriteLine(objException.ToString());
        }
    }
}
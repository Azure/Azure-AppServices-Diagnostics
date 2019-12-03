namespace AspNetCoreSample.UnitTests
{
    using AppServiceSample.Controllers;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ValuesControllerTest
    {
        [TestMethod]
        public void IndexReturnsContentResult()
        {
            // Arrange
            var controller = new ValuesController();

            // Act
            var result = controller.Get(1).Value;

            // Assert
            Assert.AreEqual("value", result);
        }
    }
}

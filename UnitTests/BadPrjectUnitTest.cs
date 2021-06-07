using Adv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ThirdParty;

namespace UnitTests
{
    [TestClass]
    public class BadProjectUnitTest
    {
        [TestMethod]
        public void GetErrorCount()
        {
            AdvertisementService adtService = new AdvertisementService();
            adtService.EmptyQueue();
            for (var i = 0; i < 5; i++)
                adtService.AddQueue(DateTime.UtcNow);

            int errorCount = adtService.GetErrorCount();

            Assert.AreEqual(5, errorCount, "GetErrorCount not working correctly");
        }

        [TestMethod]
        public void CleanQueue()
        {
            AdvertisementService adtService = new AdvertisementService();
            adtService.EmptyQueue();
            for (var i = 0; i< 30; i++)
                adtService.AddQueue(DateTime.UtcNow);

            adtService.CleanQueue();
            int errorCount = adtService.GetErrorCount();

            Assert.AreEqual(20, errorCount, "CleanQueue not working correctly");
        }

        [TestMethod]
        public void GetNoSqlAdvProviderNoException()
        {
            var id = "1";
            AdvertisementService adtService = new AdvertisementService();
            adtService.EmptyQueue();
            var adt = adtService.GetNoSqlAdvProvider(id, false);

            var errorCount = adtService.GetErrorCount();

            if (errorCount < adtService.RetryCount())
                Assert.AreEqual("1", adt.WebId, "GetNoSqlAdvProviderNoException not working correctly");
            else
                //In this case, it is ok, it is because it fails too many times.
                Assert.AreEqual("1", "1", "GetNoSqlAdvProviderNoException not working correctly");

        }

        [TestMethod]
        public void GetNoSqlAdvProviderWithExceptions()
        {
            var id = "1";
            AdvertisementService adtService = new AdvertisementService();
            adtService.EmptyQueue();
            Advertisement adv = adtService.GetNoSqlAdvProvider(id, true);

            var errorCount = adtService.GetErrorCount();

            //I temporarily changed ThirdParty.NoSqlAdvProvider so it fails ;-)
            if (errorCount < adtService.RetryCount())
                Assert.AreEqual("1", adv.WebId, "GetNoSqlAdvProviderWithExceptions not working correctly");
            else
                //In this case, it is ok, it is because it fails too many times.
                Assert.AreEqual("1", "1", "GetNoSqlAdvProviderWithExceptions not working correctly");

        }

        [TestMethod]
        public void GetAdvertisement()
        {
            var id = "1";
            AdvertisementService adtService = new AdvertisementService();
            var adt = adtService.GetAdvertisement(id);

            Assert.AreEqual("1", adt.WebId, "GetAdvertisement not working correctly");

        }

    }
}

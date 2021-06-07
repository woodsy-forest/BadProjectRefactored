using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Caching;
using System.Threading;
using ThirdParty;

namespace Adv
{
    public class AdvertisementService
    {
        private static MemoryCache cache = new MemoryCache("test");
        private static Queue<DateTime> errors = new Queue<DateTime>();

        private Object lockObj = new Object();
        // **************************************************************************************************
        // Loads Advertisement information by id
        // from cache or if not possible uses the "mainProvider" or if not possible uses the "backupProvider"
        // **************************************************************************************************
        // Detailed Logic:
        // 
        // 1. Tries to use cache (and retuns the data or goes to STEP2)
        //
        // 2. If the cache is empty it uses the NoSqlDataProvider (mainProvider), 
        //    in case of an error it retries it as many times as needed based on AppSettings
        //    (returns the data if possible or goes to STEP3)
        //
        // 3. If it can't retrive the data or the ErrorCount in the last hour is more than 10, 
        //    it uses the SqlDataProvider (backupProvider)

        public void CleanQueue()
        {
            while (errors.Count > 20) errors.Dequeue();
        }

        public void EmptyQueue()
        {
            errors.Clear();
        }

        public int GetErrorCount()
        {
            int errorCount = 0;
            foreach (var dat in errors)
            {
                if (dat > DateTime.UtcNow.AddHours(-1))
                {
                    errorCount++;
                }
            }

            return errorCount;
        }

        public void AddQueue(DateTime dt)
        {
            errors.Enqueue(dt);
        }

        public int RetryCount()
        {
            try
            {
                return int.Parse(ConfigurationManager.AppSettings["RetryCount"]);
            }
            catch
            {
                return 3;
            }
        }

        public Advertisement GetNoSqlAdvProvider(string id, bool forceExpection)
        {
            Advertisement adv = null;

            int retry = 0;
            do
            {
                retry++;
                try
                {
                    if (forceExpection)
                        throw new Exception("For test purposes only");
                    else
                    {
                        var dataProvider = new NoSqlAdvProvider();
                        adv = dataProvider.GetAdv(id);
                    }
                }
                catch
                {
                    Thread.Sleep(1000);
                    errors.Enqueue(DateTime.Now); // Store HTTP error timestamp              
                }
            } while ((adv == null) && (retry < RetryCount()));

            return adv;
        }

        public Advertisement GetAdvertisement(string id)
        {
            Advertisement adv = null;

            lock (lockObj)
            {
                // Use Cache if available
                adv = (Advertisement)cache.Get($"AdvKey_{id}");

                // Count HTTP error timestamps in the last hour
                /*
                while (errors.Count > 20) errors.Dequeue();
                int errorCount = 0;
                foreach (var dat in errors)
                {
                    if (dat > DateTime.Now.AddHours(-1))
                    {
                        errorCount++;
                    }
                }
                */
              
                //Set them up as Methods, so they can be tested.
                CleanQueue();
                int errorCount = GetErrorCount();

                // If Cache is empty and ErrorCount<10 then use HTTP provider
                if ((adv == null) && (errorCount < 10))
                {
                    /*
                    int retry = 0;
                    do
                    {
                        retry++;
                        try
                        {
                            var dataProvider = new NoSqlAdvProvider();
                            adv = dataProvider.GetAdv(id);
                        }
                        catch
                        {
                            Thread.Sleep(1000);
                            errors.Enqueue(DateTime.Now); // Store HTTP error timestamp              
                        }
                    } while ((adv == null) && (retry < int.Parse(ConfigurationManager.AppSettings["RetryCount"])));
                    */
                    //Set up as Method, so it can be tested.
                    adv = GetNoSqlAdvProvider(id, false);

                    if (adv != null)
                    {
                        cache.Set($"AdvKey_{id}", adv, DateTimeOffset.Now.AddMinutes(5));
                    }
                }


                // if needed try to use Backup provider
                if (adv == null)
                {
                    adv = SQLAdvProvider.GetAdv(id);

                    if (adv != null)
                    {
                        cache.Set($"AdvKey_{id}", adv, DateTimeOffset.Now.AddMinutes(5));
                    }
                }
            }
            return adv;
        }
    }
}

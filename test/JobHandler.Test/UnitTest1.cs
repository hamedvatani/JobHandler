using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;

namespace JobHandler.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Test1_1Job()
        {
            var helper = new JobHelper("group1");
            helper.Start();
            helper.Send(new Job { Subject = "Job 1" });
            Thread.Sleep(1000);
            helper.Stop();
            Assert.AreEqual(1, helper.ReceivedJobs.Count);
            Assert.AreEqual("Job 1", helper.ReceivedJobs.ToList()[0].Subject);
        }

        [TestMethod]
        public void Test2_StopTest()
        {
            var helper = new JobHelper("group1");
            helper.Start();
            helper.Send(new Job { Subject = "Job 1" });
            Thread.Sleep(3000);
            helper.Stop();
            helper.Send(new Job { Subject = "Job 2" });
            Assert.AreEqual(1, helper.ReceivedJobs.Count);
            Assert.AreEqual("Job 1", helper.ReceivedJobs.ToList()[0].Subject);
        }

        [TestMethod]
        public void Test3_MaxThreadsTest1()
        {
            var helper = new JobHelper("group1", 5);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            helper.Start(5000);
            helper.Send(new Job { Subject = "Job 1" });
            helper.Send(new Job { Subject = "Job 2" });
            helper.Send(new Job { Subject = "Job 3" });
            helper.Send(new Job { Subject = "Job 4" });
            helper.Send(new Job { Subject = "Job 5" });
            while (helper.ReceivedJobs.Count < 5)
                Thread.Sleep(100);
            sw.Stop();
            helper.Stop();
            if (sw.ElapsedMilliseconds >= 10000)
                Assert.Fail();
        }

        [TestMethod]
        public void Test3_MaxThreadsTest2()
        {
            var helper = new JobHelper("group1", 5);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            helper.Start(5000);
            helper.Send(new Job { Subject = "Job 1" });
            helper.Send(new Job { Subject = "Job 2" });
            helper.Send(new Job { Subject = "Job 3" });
            helper.Send(new Job { Subject = "Job 4" });
            helper.Send(new Job { Subject = "Job 5" });
            helper.Send(new Job { Subject = "Job 6" });
            while (helper.ReceivedJobs.Count < 6)
                Thread.Sleep(100);
            sw.Stop();
            helper.Stop();
            if (sw.ElapsedMilliseconds < 10000)
                Assert.Fail();
        }

        [TestMethod]
        public void Test4_InvalidMessage()
        {
            var helper = new JobHelper("group1", 5);
            helper.Start();
            helper.Send("Job 1");
            Thread.Sleep(1500);
            helper.Stop();
            Assert.AreEqual(0, helper.ReceivedJobs.Count);

            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var res = channel.BasicGet("group1_FailJobs", false);
                Assert.IsNotNull(res);
            }
        }

        [TestMethod]
        public void Test5_Timeout()
        {
            var helper = new JobHelper("group1", timeout: 1000, maxRetries: 1);
            helper.Start(3000);
            helper.Send(new Job { Subject = "Job 1" });
            Thread.Sleep(5000);
            helper.Stop();
            Assert.AreEqual(0, helper.ReceivedJobs.Count);
        }

        [TestMethod]
        public void Test6_Retries()
        {
            var helper = new JobHelper("group1", timeout: 1000);
            helper.Start(10, false);
            helper.Send(new Job { Subject = "Job 1" });
            Thread.Sleep(20000);
            helper.Stop();
            Assert.AreEqual(3, helper.ReceivedJobs.Count);
        }

        [TestMethod]
        public void Test7_FailAction()
        {
            var helper = new JobHelper("group1", timeout: 1000);
            helper.Start(10, false, true);
            helper.Send(new Job { Subject = "Job 1" });
            Thread.Sleep(20000);
            helper.Stop();
            Assert.IsNotNull(helper.FailJob);
            Assert.AreEqual(3, helper.FailJobErrorList.Count);
        }
    }
}

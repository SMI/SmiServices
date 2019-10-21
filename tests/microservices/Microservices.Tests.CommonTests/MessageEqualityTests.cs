
using Microservices.Common.Messages;
using NUnit.Framework;
using System;
using System.Linq;

namespace Microservices.Common.Tests
{
    public class MessageEqualityTests
    {
        [Test]
        public void TestEqualityMembersExistForAllIMessages()
        {
            var allClassesImplementingIMessage = typeof (IMessage).Assembly.GetTypes().Where(t => typeof (IMessage).IsAssignableFrom(t));

            foreach (Type type in allClassesImplementingIMessage)
            {
                if(type.IsInterface || type.IsAbstract)
                    continue;

                var equalsMethods = type.GetMethods().Where(m => m.Name.Equals("Equals") && m.DeclaringType == type).ToArray();
                
                Assert.IsTrue(equalsMethods.Any(),"Type '" + type + "' does not have Equality members");
            }
        }

        [Test]
        public void TestEquals_AccessionDirectoryMessage()
        {
            var msg1 = new AccessionDirectoryMessage();
            var msg2 = new AccessionDirectoryMessage();
            
            Assert.AreEqual(msg1,msg2);
            Assert.AreEqual(msg1.GetHashCode(),msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "500";
            msg2.NationalPACSAccessionNumber = "500";

            Assert.AreEqual(msg1,msg2);
            Assert.AreEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "999";
            Assert.AreNotEqual(msg1, msg2);
            Assert.AreNotEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "500";

            msg1.DirectoryPath = @"c:\temp";
            msg2.DirectoryPath = @"c:\temp";

            Assert.AreEqual(msg1,msg2);
            Assert.AreEqual(msg1.GetHashCode(),msg2.GetHashCode());

            msg2.DirectoryPath = @"C:\Temp"; //caps is relevant

            Assert.AreNotEqual(msg1, msg2);
            Assert.AreNotEqual(msg1.GetHashCode(), msg2.GetHashCode());
        }

        [Test]
        public void TestEquals_DicomFileMessage()
        {
            var msg1 = new DicomFileMessage();
            var msg2 = new DicomFileMessage();

            Assert.AreEqual(msg1, msg2);
            Assert.AreEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "500";
            msg2.NationalPACSAccessionNumber = "500";

            Assert.AreEqual(msg1, msg2);
            Assert.AreEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "999";
            Assert.AreNotEqual(msg1, msg2);
            Assert.AreNotEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "500";

            msg1.DicomDataset = "jsonified string";
            msg2.DicomDataset = "jsonified string";

            Assert.AreEqual(msg1, msg2);
            Assert.AreEqual(msg1.GetHashCode(), msg2.GetHashCode());
        }
        
        [Test]
        public void TestEquals_SeriesMessage()
        {
            var msg1 = new SeriesMessage();
            var msg2 = new SeriesMessage();

            Assert.AreEqual(msg1, msg2);
            Assert.AreEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "500";
            msg2.NationalPACSAccessionNumber = "500";

            Assert.AreEqual(msg1, msg2);
            Assert.AreEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "999";
            Assert.AreNotEqual(msg1, msg2);
            Assert.AreNotEqual(msg1.GetHashCode(), msg2.GetHashCode());

            msg1.NationalPACSAccessionNumber = "500";

            msg1.DicomDataset = "jsonified string";
            msg2.DicomDataset = "jsonified string";

            Assert.AreEqual(msg1, msg2);
            Assert.AreEqual(msg1.GetHashCode(), msg2.GetHashCode());
        }
    }
}

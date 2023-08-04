using NUnit.Framework;
using Smi.Common.Messages.Updating;
using System;

namespace Microservices.UpdateValues.Tests
{
    public class TestUpdateValuesMessage
    {
        [Test]
        public void TestNoWhere()
        {
            var msg = new UpdateValuesMessage();
            var ex = Assert.Throws<Exception>(msg.Validate);

            Assert.AreEqual("There must be at least one search field for WHERE section.  Otherwise this would update entire tables",ex.Message);
        }

        [Test]
        public void TestNoWhereValue()
        {
            var msg = new UpdateValuesMessage();
            msg.WhereFields = new string[]{"ff" };

            var ex = Assert.Throws<Exception>(msg.Validate);

            Assert.AreEqual("WhereFields length must match HaveValues length",ex.Message);
        }
        [Test]
        public void TestNoSet()
        {
            var msg = new UpdateValuesMessage();
            msg.WhereFields = new string[]{"ff" };
            msg.HaveValues = new string?[] { null}; //where column ff has a null value

            var ex = Assert.Throws<Exception>(msg.Validate);

            Assert.AreEqual("There must be at least one value to write",ex.Message);
        }

        [Test]
        public void TestNoSetValue()
        {
            var msg = new UpdateValuesMessage();
            msg.WhereFields = new string[]{"ff" };
            msg.HaveValues = new string?[] { null}; //where column ff has a null value
            msg.WriteIntoFields = new string[]{ "ff"};

            var ex = Assert.Throws<Exception>(msg.Validate);

            Assert.AreEqual("WriteIntoFields length must match Values length",ex.Message);
        }

        [Test]
        public void TestTwoValuesOneOperator()
        {
            var msg = new UpdateValuesMessage();
            msg.WhereFields = new string[]{"ff","mm" };
            msg.HaveValues = new string[] { "111","123"}; 
            msg.Operators = new string[]{ "="};
            msg.WriteIntoFields = new string[]{ "ff"};
            msg.Values= new string[]{ "ff"};

            var ex = Assert.Throws<Exception>(msg.Validate);

            Assert.AreEqual("WhereFields length must match Operators length",ex.Message);
        }
        [Test]
        public void Test_GoodMessage()
        {
            var msg = new UpdateValuesMessage();
            msg.WhereFields = new string[]{"ff" };
            msg.HaveValues = new string?[] { null}; //where column ff has a null value
            msg.WriteIntoFields = new string[]{ "ff"};
            msg.Values = new string[] { "ddd"}; //write the value ddd

            msg.Validate();

        }
        
        [Test]
        public void TestEquality()
        {
            var m1 = new UpdateValuesMessage();
            var m2 = new UpdateValuesMessage();

            Assert.AreEqual(m1,m2);
            Assert.AreEqual(m1.GetHashCode(),m2.GetHashCode());

            m1.WhereFields = new []{ "fff"};
            
            Assert.AreNotEqual(m1,m2);
            
            m2.WhereFields = new []{ "fff"};

            Assert.AreEqual(m1,m2);
            Assert.AreEqual(m1.GetHashCode(),m2.GetHashCode());

            m1.WhereFields = new string[]{};
            m2.WhereFields = new string[]{};

            Assert.AreEqual(m1,m2);
            Assert.AreEqual(m1.GetHashCode(),m2.GetHashCode());

            foreach(var prop in typeof(UpdateValuesMessage).GetProperties())
            {
                if(prop.Name.Equals(nameof(UpdateValuesMessage.ExplicitTableInfo)))
                {
                    prop.SetValue(m1,new int[]{ 6});
                    Assert.AreNotEqual(m1,m2);

                    prop.SetValue(m2,new int[]{ 6});
                    Assert.AreEqual(m1,m2);
                    Assert.AreEqual(m1.GetHashCode(),m2.GetHashCode());

                    
                    prop.SetValue(m2,new int[]{ 7});
                    Assert.AreNotEqual(m1,m2);
                    prop.SetValue(m2,new int[]{ 6});

                    Assert.AreEqual(m1,m2);
                    Assert.AreEqual(m1.GetHashCode(),m2.GetHashCode());
                }
                else
                {
                    prop.SetValue(m1,new string[]{ "ss"});
                    Assert.AreNotEqual(m1,m2);

                    prop.SetValue(m2,new string[]{ "ss"});
                    Assert.AreEqual(m1,m2);
                    Assert.AreEqual(m1.GetHashCode(),m2.GetHashCode());
                }

            }

        }
    }
}

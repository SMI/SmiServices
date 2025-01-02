using NUnit.Framework;
using SmiServices.Common.Messages.Updating;
using System;

namespace SmiServices.UnitTests.Microservices.UpdateValues;

public class TestUpdateValuesMessage
{
    [Test]
    public void TestNoWhere()
    {
        var msg = new UpdateValuesMessage();
        var ex = Assert.Throws<Exception>(msg.Validate);

        Assert.That(ex!.Message, Is.EqualTo("There must be at least one search field for WHERE section.  Otherwise this would update entire tables"));
    }

    [Test]
    public void TestNoWhereValue()
    {
        var msg = new UpdateValuesMessage
        {
            WhereFields = ["ff"]
        };

        var ex = Assert.Throws<Exception>(msg.Validate);

        Assert.That(ex!.Message, Is.EqualTo("WhereFields length must match HaveValues length"));
    }
    [Test]
    public void TestNoSet()
    {
        var msg = new UpdateValuesMessage
        {
            WhereFields = ["ff"],
            HaveValues = [null] //where column ff has a null value
        };

        var ex = Assert.Throws<Exception>(msg.Validate);

        Assert.That(ex!.Message, Is.EqualTo("There must be at least one value to write"));
    }

    [Test]
    public void TestNoSetValue()
    {
        var msg = new UpdateValuesMessage
        {
            WhereFields = ["ff"],
            HaveValues = [null], //where column ff has a null value
            WriteIntoFields = ["ff"]
        };

        var ex = Assert.Throws<Exception>(msg.Validate);

        Assert.That(ex!.Message, Is.EqualTo("WriteIntoFields length must match Values length"));
    }

    [Test]
    public void TestTwoValuesOneOperator()
    {
        var msg = new UpdateValuesMessage
        {
            WhereFields = ["ff", "mm"],
            HaveValues = ["111", "123"],
            Operators = ["="],
            WriteIntoFields = ["ff"],
            Values = ["ff"]
        };

        var ex = Assert.Throws<Exception>(msg.Validate);

        Assert.That(ex!.Message, Is.EqualTo("WhereFields length must match Operators length"));
    }
    [Test]
    public void Test_GoodMessage()
    {
        var msg = new UpdateValuesMessage
        {
            WhereFields = ["ff"],
            HaveValues = [null], //where column ff has a null value
            WriteIntoFields = ["ff"],
            Values = ["ddd"] //write the value ddd
        };

        msg.Validate();

    }

    [Test]
    public void TestEquality()
    {
        var m1 = new UpdateValuesMessage();
        var m2 = new UpdateValuesMessage();

        Assert.That(m2, Is.EqualTo(m1));
        Assert.That(m2.GetHashCode(), Is.EqualTo(m1.GetHashCode()));

        m1.WhereFields = ["fff"];

        Assert.That(m2, Is.Not.EqualTo(m1));

        m2.WhereFields = ["fff"];

        Assert.That(m2, Is.EqualTo(m1));
        Assert.That(m2.GetHashCode(), Is.EqualTo(m1.GetHashCode()));

        m1.WhereFields = [];
        m2.WhereFields = [];

        Assert.That(m2, Is.EqualTo(m1));
        Assert.That(m2.GetHashCode(), Is.EqualTo(m1.GetHashCode()));

        foreach (var prop in typeof(UpdateValuesMessage).GetProperties())
        {
            if (prop.Name.Equals(nameof(UpdateValuesMessage.ExplicitTableInfo)))
            {
                prop.SetValue(m1, new int[] { 6 });
                Assert.That(m2, Is.Not.EqualTo(m1));

                prop.SetValue(m2, new int[] { 6 });
                Assert.That(m2, Is.EqualTo(m1));
                Assert.That(m2.GetHashCode(), Is.EqualTo(m1.GetHashCode()));


                prop.SetValue(m2, new int[] { 7 });
                Assert.That(m2, Is.Not.EqualTo(m1));
                prop.SetValue(m2, new int[] { 6 });

                Assert.That(m2, Is.EqualTo(m1));
                Assert.That(m2.GetHashCode(), Is.EqualTo(m1.GetHashCode()));
            }
            else
            {
                prop.SetValue(m1, new string[] { "ss" });
                Assert.That(m2, Is.Not.EqualTo(m1));

                prop.SetValue(m2, new string[] { "ss" });
                Assert.That(m2, Is.EqualTo(m1));
                Assert.That(m2.GetHashCode(), Is.EqualTo(m1.GetHashCode()));
            }

        }

    }
}

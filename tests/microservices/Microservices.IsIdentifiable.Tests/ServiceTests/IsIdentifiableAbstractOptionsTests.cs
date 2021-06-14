using FAnsi;
using Microservices.IsIdentifiable.Options;
using NUnit.Framework;
using Smi.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservices.IsIdentifiable.Tests.ServiceTests
{
    class IsIdentifiableAbstractOptionsTests
    {
        [Test]
        public void FillMissingWithValuesUsing_NoOptionsAnywhere()
        {
            var opts = new IsIdentifiableDicomFileOptions();            
            var globalOpts = new IsIdentifiableOptions();
            opts.FillMissingWithValuesUsing(globalOpts);
        }

        [Test]
        public void FillMissingWithValuesUsing_Override()
        {
            var opts = new IsIdentifiableDicomFileOptions();
            var globalOpts = new IsIdentifiableOptions();

            int propsCounted = 0;

            foreach(var gProp in typeof(IsIdentifiableOptions).GetProperties())
            {
                var cliProp = typeof(IsIdentifiableDicomFileOptions).GetProperty(gProp.Name);

                if(cliProp == null)
                {
                    continue;
                }


                var testVal = GetTestValue(cliProp);
                gProp.SetValue(globalOpts, testVal);

                Assert.AreNotEqual(testVal, cliProp.GetValue(opts));
                opts.FillMissingWithValuesUsing(globalOpts);
                Assert.AreEqual(testVal, cliProp.GetValue(opts));

                propsCounted++;
            }

            // we did test some properties right!
            Assert.Greater(propsCounted, 0);

        }

        private object GetTestValue(System.Reflection.PropertyInfo gProp)
        {
            if(gProp.PropertyType == typeof(int))
            {
                return 5123;
            }

            if (gProp.PropertyType == typeof(string))
            {
                return "troll doll!";
            }
            if (gProp.PropertyType == typeof(bool))
            {
                return true;
            }

            if (gProp.PropertyType == typeof(DatabaseType))
            {
                return DatabaseType.Oracle;
            }

            throw new ArgumentException($"Not sure what value to use in test for PropertyType {gProp.PropertyType}.  This is an error in the test harness coverage not the underlying code.");

        }
    }
}

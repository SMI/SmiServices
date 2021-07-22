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
            int propsCounted = 0;

            foreach(var gProp in typeof(IsIdentifiableOptions).GetProperties())
            {
                var cliProp = typeof(IsIdentifiableDicomFileOptions).GetProperty(gProp.Name);

                if(cliProp == null)
                {
                    continue;
                }

                var opts = new IsIdentifiableDicomFileOptions();
                var globalOpts = new IsIdentifiableOptions();

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

        [Test]
        public void FillMissingWithValuesUsing_NoOverride()
        {
            int propsCounted = 0;

            foreach (var gProp in typeof(IsIdentifiableOptions).GetProperties())
            {
                var cliProp = typeof(IsIdentifiableDicomFileOptions).GetProperty(gProp.Name);

                if (cliProp == null)
                {
                    continue;
                }

                var opts = new IsIdentifiableDicomFileOptions();
                var globalOpts = new IsIdentifiableOptions();

                var testVal1 = GetTestValue(cliProp);
                var testVal2 = GetTestValue2(cliProp);

                if(testVal1 is bool)
                {
                    // boolean cli false is the default so missing and false are the same
                    // so instead lets make sure that false in yaml config doesn't override 
                    // true in cli
                    testVal1 = false;
                    testVal2 = true;
                }

                // yaml says one value
                gProp.SetValue(globalOpts, testVal1);
                // cli says a different value
                cliProp.SetValue(opts, testVal2);
                
                // we should not have the yaml file entry
                Assert.AreNotEqual(testVal1, cliProp.GetValue(opts));
                
                // we ask to fill in missing values using the yaml entries
                opts.FillMissingWithValuesUsing(globalOpts);

                // but we had an entry on CLI already so that should take precedence
                Assert.AreNotEqual(testVal1, cliProp.GetValue(opts));
                Assert.AreEqual(testVal2, cliProp.GetValue(opts));

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
                return DatabaseType.MySql;
            }

            throw new ArgumentException($"Not sure what value to use in test for PropertyType {gProp.PropertyType}.  This is an error in the test harness coverage not the underlying code.");

        }
        private object GetTestValue2(System.Reflection.PropertyInfo gProp)
        {
            if (gProp.PropertyType == typeof(int))
            {
                return 66456;
            }

            if (gProp.PropertyType == typeof(string))
            {
                return "rylyly?";
            }
            if (gProp.PropertyType == typeof(bool))
            {
                return false;
            }

            if (gProp.PropertyType == typeof(DatabaseType))
            {
                return DatabaseType.Oracle;
            }

            throw new ArgumentException($"Not sure what value to use in test for PropertyType {gProp.PropertyType}.  This is an error in the test harness coverage not the underlying code.");

        }
    }
}

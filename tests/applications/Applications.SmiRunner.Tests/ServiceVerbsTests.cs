using CommandLine;
using NUnit.Framework;
using Smi.Common.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;


namespace Applications.SmiRunner.Tests
{
    public class ServiceVerbsTests
    {
        #region Fixture Methods

        private readonly IEnumerable<Type> _allVerbs =
            typeof(VerbBase)
            .Assembly
            .GetTypes()
            .Where(t => typeof(VerbBase).IsAssignableFrom(t) && !t.IsAbstract);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestLogger.Setup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() { }

        #endregion

        #region Test Methods

        [SetUp]
        public void SetUp() { }

        [TearDown]
        public void TearDown() { }

        #endregion

        #region Tests

        [Test]
        public void HelpTextUrls_AreValid()
        {
            foreach (Type t in _allVerbs)
            {
                var verbAttribute = (VerbAttribute)Attribute.GetCustomAttribute(t, typeof(VerbAttribute));
                Assert.NotNull(verbAttribute);

                // Split into two parts at the first instance of ':' and return the second part
                string urlString = verbAttribute.HelpText.Split(new[] { ':' }, 2)[1].Trim();

                // TODO(rkm 2021-03-02) Figure-out a better way to manage projects in nested directories
                if (t == typeof(UpdateValuesVerb))
                {
                    var idx = urlString.LastIndexOf("/") + 1;
                    urlString = urlString.Insert(idx, "Updating/");
                }

                var uri = new Uri(urlString);
                Assert.AreEqual(Uri.UriSchemeHttps, uri.Scheme);

                var req = WebRequest.Create(uri) as HttpWebRequest;
                Assert.NotNull(req);
                req.Method = "HEAD";

                try
                {
                    using var resp = req.GetResponse() as HttpWebResponse;

                    Assert.NotNull(resp);
                    Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
                }
                catch (WebException e)
                {
                    throw new WebException($"Expected {uri} for {t.Name}", e);
                }
            }
        }

        [Test]
        public void VerbName_MatchesClassName()
        {
            foreach (Type t in _allVerbs)
            {
                string nameWithoutVerb = t.Name.Substring(0, t.Name.LastIndexOf("Verb"));
                string[] splitWords = Regex.Split(nameWithoutVerb, @"(?<!^)(?=[A-Z])");
                string expectedVerbName = string.Join('-', splitWords).ToLower();

                // Special-case 'DB'
                expectedVerbName = expectedVerbName.Replace("-db-", "db-");

                var verbAttribute = (VerbAttribute)Attribute.GetCustomAttribute(t, typeof(VerbAttribute));
                Assert.NotNull(verbAttribute);

                Assert.AreEqual(expectedVerbName, verbAttribute.Name);
            }
        }

        #endregion
    }
}

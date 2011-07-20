﻿// ***********************************************************************
// Copyright (c) 2011 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

namespace NUnit.ConsoleRunner
{
    public class NUnit2XmlOutputWriter
    {
        private XmlTextWriter xmlWriter;
        private static Dictionary<string, string> suiteTypes = new Dictionary<string, string>();
        private static Dictionary<string, string> resultStates = new Dictionary<string, string>();

        static NUnit2XmlOutputWriter()
        {
            // TODO: Other types: Namespace, Theory
            suiteTypes["test-project"] = "Project";
            suiteTypes["test-assembly"] = "Assembly";
            suiteTypes["test-suite"] = "test-suite";
            suiteTypes["test-fixture"] = "TestFixture";
            suiteTypes["setup-fixture"] = "SetUpFixture";
            suiteTypes["generic-fixture"] = "GenericFixture";
            suiteTypes["parameterized-fixture"] = "ParameterizedFixture";
            suiteTypes["generic-method"] = "GenericMethod";
            suiteTypes["parameterized-method"] = "ParameterizedMethod";
            suiteTypes["test-case"] = "TestMethod";

            resultStates["Passed"] = "Success";
            resultStates["Failed"] = "Failure";
            resultStates["Failed:Error"] = "Error";
            resultStates["Failed:Cancelled"] = "Cancelled";
            resultStates["Inconclusive"] = "Inconclusive";
            resultStates["Skipped"] = "Skipped";
            resultStates["Skipped:Ignored"] = "Ignored";
            resultStates["Skipped:Invalid"] = "NotRunnable";
        }

        public void WriteXmlOutput(XmlNode result, string outputPath)
        {
            this.xmlWriter = new XmlTextWriter(outputPath, Encoding.Default);

            InitializeXmlFile(result, outputPath);

            foreach (XmlNode child in result.ChildNodes)
                if (child.Name.StartsWith("test-"))
                    WriteResultElement(child);

            TerminateXmlFile();
        }

        private void InitializeXmlFile(XmlNode result, string outputPath)
        {
            ResultSummary summaryResults = new ResultSummary(result);

            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.WriteStartDocument(false);
            xmlWriter.WriteComment("This file represents the results of running a test suite");

            xmlWriter.WriteStartElement("test-results");

            xmlWriter.WriteAttributeString("name", XmlHelper.GetAttribute(result, "fullname"));
            xmlWriter.WriteAttributeString("total", summaryResults.TestsRun.ToString());
            xmlWriter.WriteAttributeString("errors", summaryResults.Errors.ToString());
            xmlWriter.WriteAttributeString("failures", summaryResults.Failures.ToString());
            xmlWriter.WriteAttributeString("not-run", summaryResults.TestsNotRun.ToString());
            xmlWriter.WriteAttributeString("inconclusive", summaryResults.Inconclusive.ToString());
            xmlWriter.WriteAttributeString("ignored", summaryResults.Ignored.ToString());
            xmlWriter.WriteAttributeString("skipped", summaryResults.Skipped.ToString());
            xmlWriter.WriteAttributeString("invalid", summaryResults.NotRunnable.ToString());

            xmlWriter.WriteAttributeString("date", XmlHelper.GetAttribute(result, "run-date"));
            xmlWriter.WriteAttributeString("time", XmlHelper.GetAttribute(result, "start-time"));
            WriteEnvironment();
            WriteCultureInfo();
        }

        private void WriteCultureInfo()
        {
            xmlWriter.WriteStartElement("culture-info");
            xmlWriter.WriteAttributeString("current-culture",
                                           CultureInfo.CurrentCulture.ToString());
            xmlWriter.WriteAttributeString("current-uiculture",
                                           CultureInfo.CurrentUICulture.ToString());
            xmlWriter.WriteEndElement();
        }

        private void WriteEnvironment()
        {
            xmlWriter.WriteStartElement("environment");
            xmlWriter.WriteAttributeString("nunit-version",
                                           Assembly.GetExecutingAssembly().GetName().Version.ToString());
            xmlWriter.WriteAttributeString("clr-version",
                                           Environment.Version.ToString());
            xmlWriter.WriteAttributeString("os-version",
                                           Environment.OSVersion.ToString());
            xmlWriter.WriteAttributeString("platform",
                Environment.OSVersion.Platform.ToString());
            xmlWriter.WriteAttributeString("cwd",
                                           Environment.CurrentDirectory);
            xmlWriter.WriteAttributeString("machine-name",
                                           Environment.MachineName);
            xmlWriter.WriteAttributeString("user",
                                           Environment.UserName);
            xmlWriter.WriteAttributeString("user-domain",
                                           Environment.UserDomainName);
            xmlWriter.WriteEndElement();
        }

        private void WriteResultElement(XmlNode result)
        {
            StartTestElement(result);

            var categories = result.SelectSingleNode("categories");
            if (categories != null)
                WriteCategoriesElement(categories);

            var properties = result.SelectSingleNode("properties");
            if (properties != null)
                WritePropertiesElement(properties);

            var message = result.SelectSingleNode("reason/message");
            if (message != null)
                WriteReasonElement(message.InnerText);

            message = result.SelectSingleNode("failure/message");
            var stackTrace = result.SelectSingleNode("failure/stack-trace");
            if (message != null)
                WriteFailureElement(message.InnerText, stackTrace == null ? null : stackTrace.InnerText);

            if (result.Name != "test-case")
                WriteChildResults(result);

            xmlWriter.WriteEndElement(); // test element
        }

        private void TerminateXmlFile()
        {
            xmlWriter.WriteEndElement(); // test-results
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();
        }


        #region Element Creation Helpers

        private void StartTestElement(XmlNode result)
        {
            if (result.Name == "test-case")
            {
                xmlWriter.WriteStartElement("test-case");
                xmlWriter.WriteAttributeString("name", XmlHelper.GetAttribute(result, "name"));
            }
            else
            {
                xmlWriter.WriteStartElement("test-suite");
                System.Diagnostics.Debug.Assert(suiteTypes.ContainsKey(result.Name));
                if (suiteTypes.ContainsKey(result.Name))
                    xmlWriter.WriteAttributeString("type", suiteTypes[result.Name]);
                string nameAttr = result.Name == "test-assembly" || result.Name == "test-project" ? "fullname" : "name";
                xmlWriter.WriteAttributeString("name", XmlHelper.GetAttribute(result, nameAttr));
            }

            string description = XmlHelper.GetAttribute(result, "description");
            if (description != null)
                xmlWriter.WriteAttributeString("description", description);

            string resultState = XmlHelper.GetAttribute(result, "result");
            string label = XmlHelper.GetAttribute(result, "label");
            string executed = resultState == "Skipped" ? "False" : "True";
            string success = resultState == "Passed" ? "True" : "False";
            string time = XmlHelper.GetAttribute(result, "time");
            string asserts = XmlHelper.GetAttribute(result, "asserts");

            if (label != null && label != string.Empty)
                resultState += ":" + label;


            xmlWriter.WriteAttributeString("executed", executed);
            xmlWriter.WriteAttributeString("result", resultStates[resultState]);

            if (executed == "True")
            {
                xmlWriter.WriteAttributeString("success", success);
                xmlWriter.WriteAttributeString("time", time);
                xmlWriter.WriteAttributeString("asserts", asserts);
            }
        }

        private void WriteCategoriesElement(XmlNode categories)
        {
            xmlWriter.WriteStartElement("categories");
            var items = categories.SelectNodes("category");
            foreach (XmlNode item in items)
            {
                xmlWriter.WriteStartElement("category");
                xmlWriter.WriteAttributeString("name", XmlHelper.GetAttribute(item, "name"));
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
        }

        private void WritePropertiesElement(XmlNode properties)
        {
            int nprops = 0;

            xmlWriter.WriteStartElement("properties");

            var items = properties.SelectNodes("property");
            foreach (XmlNode item in items)
            {
                xmlWriter.WriteStartElement("property");
                xmlWriter.WriteAttributeString("name", XmlHelper.GetAttribute(item, "name"));
                xmlWriter.WriteAttributeString("value", XmlHelper.GetAttribute(item, "value"));
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }

        private void WriteReasonElement(string message)
        {
            xmlWriter.WriteStartElement("reason");
            xmlWriter.WriteStartElement("message");
            xmlWriter.WriteCData(message);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
        }

        private void WriteFailureElement(string message, string stackTrace)
        {
            xmlWriter.WriteStartElement("failure");
            xmlWriter.WriteStartElement("message");
            WriteCData(message);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteStartElement("stack-trace");
            if (stackTrace != null)
                WriteCData(stackTrace);
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
        }

        private void WriteChildResults(XmlNode result)
        {
            xmlWriter.WriteStartElement("results");

            foreach (XmlNode childResult in result.ChildNodes)
                if (childResult.Name.StartsWith("test-"))
                    WriteResultElement(childResult);

            xmlWriter.WriteEndElement();
        }
        #endregion

        #region Output Helpers
        /// <summary>
        /// Makes string safe for xml parsing, replacing control chars with '?'
        /// </summary>
        /// <param name="encodedString">string to make safe</param>
        /// <returns>xml safe string</returns>
        private static string CharacterSafeString(string encodedString)
        {
            /*The default code page for the system will be used.
            Since all code pages use the same lower 128 bytes, this should be sufficient
            for finding uprintable control characters that make the xslt processor error.
            We use characters encoded by the default code page to avoid mistaking bytes as
            individual characters on non-latin code pages.*/
            char[] encodedChars = System.Text.Encoding.Default.GetChars(System.Text.Encoding.Default.GetBytes(encodedString));

            System.Collections.ArrayList pos = new System.Collections.ArrayList();
            for (int x = 0; x < encodedChars.Length; x++)
            {
                char currentChar = encodedChars[x];
                //unprintable characters are below 0x20 in Unicode tables
                //some control characters are acceptable. (carriage return 0x0D, line feed 0x0A, horizontal tab 0x09)
                if (currentChar < 32 && (currentChar != 9 && currentChar != 10 && currentChar != 13))
                {
                    //save the array index for later replacement.
                    pos.Add(x);
                }
            }
            foreach (int index in pos)
            {
                encodedChars[index] = '?';//replace unprintable control characters with ?(3F)
            }
            return System.Text.Encoding.Default.GetString(System.Text.Encoding.Default.GetBytes(encodedChars));
        }

        private void WriteCData(string text)
        {
            int start = 0;
            while (true)
            {
                int illegal = text.IndexOf("]]>", start);
                if (illegal < 0)
                    break;
                xmlWriter.WriteCData(text.Substring(start, illegal - start + 2));
                start = illegal + 2;
                if (start >= text.Length)
                    return;
            }

            if (start > 0)
                xmlWriter.WriteCData(text.Substring(start));
            else
                xmlWriter.WriteCData(text);
        }

        #endregion
    }
}

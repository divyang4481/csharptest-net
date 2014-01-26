#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;
using NUnit.Framework;
using CSharpTest.Net.Html;
using System.IO;
using System.Xml;
using CSharpTest.Net.IO;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    [Category("TestXhtmlValid")]
    public partial class TestXhtmlValid
    {
        [Test]
        public void TestValidStrict()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">
            <html>
                <head><title>required</title></head>
                <body></body>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation(XhtmlDTDSpecification.XhtmlStrict_10);

            using (TempFile temp = new TempFile())
            {
                temp.WriteAllText(doc);
                v.Validate(temp.TempPath);
            }
        }
        
        [Test]
        public void TestValidTransitional()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
            <html>
                <head><title>required</title></head>
                <body><iframe></iframe></body>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation(XhtmlDTDSpecification.XhtmlTransitional_10);

            using (TempFile temp = new TempFile())
            {
                temp.WriteAllText(doc);
                v.Validate(@"C:\transitional.xhtml", new StreamReader(temp.Read()));
            }
        }

        [Test]
        public void TestValidFrameset()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Frameset//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd"">
            <html>
                <head><title>required</title></head>
                <frameset></frameset>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation(XhtmlDTDSpecification.XhtmlFrameset_10);
            v.Validate(new StringReader(doc));
        }

        [Test]
        public void TestValidAnyDTD()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Frameset//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd"">
            <html>
                <head><title>required</title></head>
                <frameset></frameset>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation(XhtmlDTDSpecification.Any);
            v.Validate(new StringReader(doc));
        }

        [Test]
        public void TestValidNoDTD()
        {
            string doc = @"<html>
                <head><title>required</title></head>
                <body></body>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation(XhtmlDTDSpecification.None);
            v.Validate(new StringReader(doc));
        }

        [Test, ExpectedException(typeof(XmlException))]
        public void TestInvalidDTD()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//Some DTD Specification//EN"" ""http://www.w3.org/something.dtd"">
            <html>
                <head><title>required</title></head>
                <body></body>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation(XhtmlDTDSpecification.None);
            v.Validate(new StringReader(doc));
        }

        [Test, ExpectedException(typeof(XmlException))]
        public void TestIncorrectDTD()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Frameset//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd"">
            <html>
                <head><title>required</title></head>
                <frameset></frameset>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation(XhtmlDTDSpecification.XhtmlStrict_10);
            v.Validate(new StringReader(doc));
        }

        [Test, ExpectedException(typeof(XmlException))]
        public void TestInvalidXml()
        {
            string doc = @"<html>
                <head><title>required</title></head>
                <body></other>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation();
            v.Validate(new StringReader(doc));
        }

        [Test, ExpectedException(typeof(XmlException))]
        public void TestInvalidStrictDoc()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">
            <html>
                <head><title>required</title></head>
                <body><iframe></iframe></body>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation();
            v.Validate(new StringReader(doc));
        }

        [Test, ExpectedException(typeof(XmlException))]
        public void TestInvalidAttribute()
        {
            string doc = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">
            <html>
                <head><title>required</title></head>
                <body width=100></body>
            </html>
            ";

            XhtmlValidation v = new XhtmlValidation();
            v.Validate(new StringReader(doc));
        }

        [Test, ExpectedException(typeof(XmlException))]
        public void TestInvalidRoot()
        {
            string doc = @"<!DOCTYPE abc PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">
            <abc>
                <head><title>required</title></head>
                <body><iframe></iframe></body>
            </abc>
            ";

            XhtmlValidation v = new XhtmlValidation();
            v.Validate(new StringReader(doc));
        }
    }
}

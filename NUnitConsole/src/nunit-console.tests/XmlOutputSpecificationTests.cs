﻿using System;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    public class XmlOutputSpecificationTests
    {
        [Test]
        public void SpecMayNotBeNull()
        {
            Assert.That(
                () => new XmlOutputSpecification(null),
                Throws.TypeOf<NullReferenceException>());
        }


        [Test]
        public void SpecOptionMustContainEqualSign()
        {
            Assert.That(
                () => new XmlOutputSpecification("MyFile.xml;transform.xslt"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void SpecOptionMustContainJustOneEqualSign()
        {
            Assert.That(
                () => new XmlOutputSpecification("MyFile.xml;transform=xslt=transform.xslt"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void FileNameOnly()
        {
            var spec = new XmlOutputSpecification("MyFile.xml");
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void FileNamePlusFormat()
        {
            var spec = new XmlOutputSpecification("MyFile.xml;format=nunit2");
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit2"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void FileNamePlusTransform()
        {
            var spec = new XmlOutputSpecification("MyFile.xml;transform=transform.xslt");
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo("transform.xslt"));
        }

        [Test]
        public void UserFormatMayBeIndicatedExplicitlyAfterTransform()
        {
            var spec = new XmlOutputSpecification("MyFile.xml;transform=transform.xslt;format=user");
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo("transform.xslt"));
        }

        [Test]
        public void UserFormatMayBeIndicatedExplicitlyBeforeTransform()
        {
            var spec = new XmlOutputSpecification("MyFile.xml;format=user;transform=transform.xslt");
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo("transform.xslt"));
        }

        [Test]
        public void MultipleFormatSpecifiersNotAllowed()
        {
            Assert.That(
                () => new XmlOutputSpecification("MyFile.xml;format=nunit2;format=nunit3"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void MultipleTransformSpecifiersNotAllowed()
        {
            Assert.That(
                () => new XmlOutputSpecification("MyFile.xml;transform=transform1.xslt;transform=transform2.xslt"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TransformWithNonUserFormatNotAllowed()
        {
            Assert.That(
                () => new XmlOutputSpecification("MyFile.xml;format=nunit2;transform=transform.xslt"),
                Throws.TypeOf<ArgumentException>());
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gallio.Model;
using Gallio.Reflection;
using Machine.Specifications.Example;
using Machine.SpecificationsAdapter;
using NUnit.Framework;

namespace Machine.Specifications.GallioAdapter.Tests
{
  [TestFixture]
  public class FrameworkTests : BaseTestFrameworkTest
  {
    protected override Gallio.Model.ITestFramework CreateFramework()
    {
      return new MachineSpecificationFramework();
    }

    protected override System.Reflection.Assembly GetSampleAssembly()
    {
      return typeof(Account).Assembly;
    }

    [Test]
    public void RootTestShouldBeValid()
    {
      PopulateTestTree();

      RootTest rootTest = testModel.RootTest;
      Assert.IsNull(rootTest.Parent);
      Assert.AreEqual(TestKinds.Root, rootTest.Kind);
      Assert.IsNull(rootTest.CodeElement);
      Assert.IsFalse(rootTest.IsTestCase);
      Assert.AreEqual(1, rootTest.Children.Count);
    }

    [Test]
    public void BaseTestShouldBeValid()
    {
      PopulateTestTree();
      RootTest rootTest = testModel.RootTest;
      Version expectedVersion = typeof(Context).Assembly.GetName().Version;
      BaseTest frameworkTest = (BaseTest)rootTest.Children[0];
      Assert.AreSame(testModel.RootTest, frameworkTest.Parent);
      Assert.AreEqual(TestKinds.Framework, frameworkTest.Kind);
      Assert.IsNull(frameworkTest.CodeElement);
      Assert.AreEqual("Machine.Specifications v" + expectedVersion, frameworkTest.Name);
      Assert.IsFalse(frameworkTest.IsTestCase);
      Assert.AreEqual(1, frameworkTest.Children.Count);
    }

    [Test]
    public void AssemblyTestShouldBeValid()
    {
      PopulateTestTree();
      RootTest rootTest = testModel.RootTest;
      BaseTest frameworkTest = (BaseTest)rootTest.Children[0];
      BaseTest assemblyTest = (BaseTest)frameworkTest.Children[0];
      Assert.AreSame(frameworkTest, assemblyTest.Parent);
      Assert.AreEqual(TestKinds.Assembly, assemblyTest.Kind);
      Assert.AreEqual(CodeReference.CreateFromAssembly(sampleAssembly), assemblyTest.CodeElement.CodeReference);
      Assert.AreEqual(sampleAssembly.GetName().Name, assemblyTest.Name);
      Assert.IsFalse(assemblyTest.IsTestCase);
      Assert.GreaterOrEqual(assemblyTest.Children.Count, 1);
    }
  }
}

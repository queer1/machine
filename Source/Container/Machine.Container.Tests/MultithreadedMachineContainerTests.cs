using System;
using System.Collections.Generic;
using System.Threading;

using Machine.Container.Model;
using Machine.Container.Plugins;
using Machine.Container.Plugins.Disposition;
using Machine.Container.Services;
using Machine.Core.Utility;

using NUnit.Framework;

namespace Machine.Container
{
  public class Creation
  {
    private readonly ResolvedServiceEntry _entry;
    private readonly object _instance;

    public ResolvedServiceEntry Entry
    {
      get { return _entry; }
    }

    public object Instance
    {
      get { return _instance; }
    }

    public Creation(ResolvedServiceEntry entry, object instance)
    {
      _entry = entry;
      _instance = instance;
    }
  }
  public class ServiceCreations
  {
    private readonly List<Creation> _creations = new List<Creation>();

    public void Clear()
    {
      _creations.Clear();
    }

    public void Add(ResolvedServiceEntry entry, object instance)
    {
      _creations.Add(new Creation(entry, instance));
    }

    public IDictionary<Type, List<Creation>> GroupByType()
    {
      Dictionary<Type, List<Creation>> grouped = new Dictionary<Type, List<Creation>>();
      foreach (Creation creation in _creations)
      {
        Type type = creation.Instance.GetType();
        if (!grouped.ContainsKey(type))
        {
          grouped[type] = new List<Creation>();
        }
        grouped[type].Add(creation);
      }
      return grouped;
    }
  }
  [TestFixture]
  public class MultithreadedMachineContainerTests : IServiceContainerListener
  {
    #region Member Data
    private MachineContainer _machineContainer;
    private readonly List<Thread> _threads = new List<Thread>();
    private readonly ServiceCreations _creations = new ServiceCreations();
    #endregion

    #region Test Setup and Teardown Methods
    [SetUp]
    public virtual void Setup()
    {
      _threads.Clear();
      _machineContainer = new MachineContainer();
      _machineContainer.Initialize();
      _machineContainer.AddListener(this);
      _machineContainer.AddPlugin(new DisposablePlugin());
      _machineContainer.PrepareForServices();
      _machineContainer.Start();
    }
    #endregion

    [Test]
    [Ignore]
    public void Multiple_Threads_Resolving()
    {
      _machineContainer.Register.Type<Service1DependsOn2>().AsTransient();
      _machineContainer.Register.Type<SimpleService2>().AsSingleton();
      ThreadStart start = delegate()
      {
        for (int i = 0; i < 20; ++i)
        {
          Service1DependsOn2 service = _machineContainer.Resolve.Object<Service1DependsOn2>();
          _machineContainer.Release(service);
        }
      };
      for (int i = 0; i < 30; ++i)
      {
        AddThread(start);
      }
      JoinAllThreads();

      IDictionary<IReaderWriterLock, List<ReaderWriterUsage>> byLock = ReaderWriterLockStatistics.Singleton.GroupByLock();
      foreach (KeyValuePair<IReaderWriterLock, List<ReaderWriterUsage>> entry in byLock)
      {
        ReaderWriterLockStatistics.OutputSummaryOfUsages(entry.Key.Name, entry.Value);
      }

      IDictionary<Type, List<Creation>> grouped = _creations.GroupByType();
      Assert.AreEqual(1, grouped[typeof(SimpleService2)].Count);
      Assert.AreEqual(30, grouped[typeof(Service1DependsOn2)].Count);
    }

    [TearDown]
    public void Teardown()
    {
      JoinAllThreads();
    }

    private void JoinAllThreads()
    {
      while (_threads.Count > 0)
      {
        _threads[0].Join();
        _threads.RemoveAt(0);
      }
    }

    private void AddThread(ThreadStart start)
    {
      Thread thread = new Thread(start);
      thread.Start();
      _threads.Add(thread);
    }

    #region IServiceContainerListener Members
    public void Initialize(IMachineContainer container)
    {
    }

    public void PreparedForServices()
    {
    }

    public void ServiceRegistered(ServiceEntry entry)
    {
    }

    public void Started()
    {
      _creations.Clear();
    }

    public void InstanceCreated(ResolvedServiceEntry entry, object instance)
    {
      _creations.Add(entry, instance);
    }

    public void InstanceReleased(ResolvedServiceEntry entry, object instance)
    {
    }
    #endregion

    #region IDisposable Members
    public void Dispose()
    {
    }
    #endregion
  }
}
using System;
using BlackMarket.Core;
using NUnit.Framework;

namespace BlackMarket.Tests
{
    public class ServiceRegistryTests
    {
        private class FakeService { }

        [SetUp]
        public void SetUp() => ServiceRegistry.Clear();

        [TearDown]
        public void TearDown() => ServiceRegistry.Clear();

        [Test]
        public void Register_ThenGet_ReturnsSameInstance()
        {
            var service = new FakeService();
            ServiceRegistry.Register(service);

            Assert.AreSame(service, ServiceRegistry.Get<FakeService>());
        }

        [Test]
        public void Get_Unregistered_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => ServiceRegistry.Get<FakeService>());
        }

        [Test]
        public void Register_Twice_Throws()
        {
            ServiceRegistry.Register(new FakeService());
            Assert.Throws<InvalidOperationException>(() => ServiceRegistry.Register(new FakeService()));
        }

        [Test]
        public void Register_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ServiceRegistry.Register<FakeService>(null));
        }

        [Test]
        public void TryGet_Unregistered_ReturnsFalseAndNull()
        {
            bool found = ServiceRegistry.TryGet(out FakeService service);

            Assert.IsFalse(found);
            Assert.IsNull(service);
        }

        [Test]
        public void Unregister_RemovesService()
        {
            ServiceRegistry.Register(new FakeService());

            Assert.IsTrue(ServiceRegistry.Unregister<FakeService>());
            Assert.IsFalse(ServiceRegistry.IsRegistered<FakeService>());
        }
    }
}

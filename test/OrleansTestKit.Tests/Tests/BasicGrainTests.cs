using System;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans.Runtime;
using TestGrains;
using TestInterfaces;
using Xunit;

namespace Orleans.TestKit.Tests
{
    public class BasicGrainTests : TestKitBase
    {
        [Fact]
        public async Task SiloSayHelloTest()
        {
            long id = new Random().Next();
            const string greeting = "Bonjour";

            IHello grain = await Silo.CreateGrainAsync<HelloGrain>(id);

            // This will create and call a Hello grain with specified 'id' in one of the test silos.
            string reply = await grain.SayHello(greeting);

            Assert.NotNull(reply);
            Assert.Equal($"You said: '{greeting}', I say: Hello!", reply);
        }

        [Fact]
        public async Task GrainActivation()
        {
            var grain = await Silo.CreateGrainAsync<LifecycleGrain>(new Random().Next());

            grain.ActivateCount.Should().Be(1);
        }

        [Fact]
        public async Task SecondGrainCreated()
        {
            await Silo.CreateGrainAsync<LifecycleGrain>(new Random().Next());

            Func<Task> creatingSecondGrainAsync = async () => await Silo.CreateGrainAsync<LifecycleGrain>(new Random().Next());
            creatingSecondGrainAsync.Should().Throw<Exception>();
        }

        [Fact]
        public async Task GrainDeactivation()
        {
            var grain = await Silo.CreateGrainAsync<LifecycleGrain>(new Random().Next());

            grain.DeactivateCount.Should().Be(0);

            await Silo.DeactivateAsync(grain);

            grain.DeactivateCount.Should().Be(1);
        }

        [Fact]
        public async Task IntegerKeyGrain()
        {
            const int id = int.MaxValue;

            var grain = await Silo.CreateGrainAsync<IntegerKeyGrain>(id);

            var key = await grain.GetKey();
            var keyLong = grain.GetPrimaryKeyLong();
            var referenceKey = grain.GrainReference.GetPrimaryKeyLong();
            var addressableKey = ((IAddressable)grain).GetPrimaryKeyLong();

            key.Should().Be(id);
            keyLong.Should().Be(id);
            referenceKey.Should().Be(id);
            addressableKey.Should().Be(id);
        }

        [Fact]
        public async Task IntegerCompoundKeyGrain()
        {
            const int id = int.MaxValue;
            var ext = "Thing";

            var grain = await Silo.CreateGrainAsync<IntegerCompoundKeyGrain>(id, ext);

            var key = await grain.GetKey();
            var keyLong = grain.GetPrimaryKeyLong(out var keyExt);
            var referenceKey = grain.GrainReference.GetPrimaryKeyLong(out var referenceKeyExt);
            var addressableKey = ((IAddressable)grain).GetPrimaryKeyLong(out var addressableKeyExt);

            key.Item1.Should().Be(id);
            key.Item2.Should().Be(ext);

            keyLong.Should().Be(id);
            keyExt.Should().Be(ext);

            referenceKey.Should().Be(id);
            referenceKeyExt.Should().Be(ext);

            addressableKey.Should().Be(id);
            addressableKeyExt.Should().Be(ext);
        }

        [Fact]
        public async Task GuidKeyGrain()
        {
            var id = Guid.NewGuid();

            var grain = await Silo.CreateGrainAsync<GuidKeyGrain>(id);

            var key = await grain.GetKey();

            var keyGuid = grain.GetPrimaryKey();
            var referenceKey = grain.GrainReference.GetPrimaryKey();
            var addressableKey = ((IAddressable)grain).GetPrimaryKey();

            key.Should().Be(id);
            keyGuid.Should().Be(id);
            referenceKey.Should().Be(id);
            addressableKey.Should().Be(id);
        }

        [Fact]
        public async Task GuidCompoundKeyGrain()
        {
            var id = Guid.NewGuid();
            var ext = "Thing";

            var grain = await Silo.CreateGrainAsync<GuidCompoundKeyGrain>(id, ext);

            var key = await grain.GetKey();

            var keyGuid = grain.GetPrimaryKey(out var keyExt);
            var referenceKey = grain.GrainReference.GetPrimaryKey(out var referenceKeyExt);
            var addressableKey = ((IAddressable)grain).GetPrimaryKey(out var addressableKeyExt);

            key.Item1.Should().Be(id);
            key.Item2.Should().Be(ext);

            keyGuid.Should().Be(id);
            keyExt.Should().Be(ext);

            referenceKey.Should().Be(id);
            referenceKeyExt.Should().Be(ext);

            addressableKey.Should().Be(id);
            addressableKeyExt.Should().Be(ext);

        }

        [Fact]
        public async Task StringKeyGrain()
        {
            const string id = "TestId";

            var grain = await Silo.CreateGrainAsync<StringKeyGrain>(id);

            var key = await grain.GetKey();

            var keyString = grain.GetPrimaryKeyString();
            var referenceKey = grain.GrainReference.GetPrimaryKeyString();
            var addressableKey = ((IAddressable)grain).GetPrimaryKeyString();

            key.Should().Be(id);
            keyString.Should().Be(id);
            referenceKey.Should().Be(id);
            addressableKey.Should().Be(id);
        }

        [Fact]
        public async Task StatefulIntegerKeyGrain()
        {
            const int id = int.MaxValue;

            var grain = await Silo.CreateGrainAsync<StatefulIntegerKeyGrain>(id);

            var key = await grain.GetKey();

            var keyLong = grain.GetPrimaryKeyLong();
            var referenceKey = grain.GrainReference.GetPrimaryKeyLong();
            var addressableKey = ((IAddressable)grain).GetPrimaryKeyLong();

            key.Should().Be(id);
            keyLong.Should().Be(id);
            referenceKey.Should().Be(id);
            addressableKey.Should().Be(id);
        }

        [Fact]
        public async Task StatefulGuidKeyGrain()
        {
            var id = Guid.NewGuid();

            var grain = await Silo.CreateGrainAsync<StatefulGuidKeyGrain>(id);

            var key = await grain.GetKey();

            var keyGuid = grain.GetPrimaryKey();
            var referenceKey = grain.GrainReference.GetPrimaryKey();
            var addressableKey = ((IAddressable)grain).GetPrimaryKey();

            key.Should().Be(id);
            keyGuid.Should().Be(id);
            referenceKey.Should().Be(id);
            addressableKey.Should().Be(id);
        }

        [Fact]
        public async Task StatefulStringKeyGrain()
        {
            const string id = "TestId";

            var grain = await Silo.CreateGrainAsync<StatefulStringKeyGrain>(id);

            var key = await grain.GetKey();
            var keyString = grain.GetPrimaryKeyString();
            var referenceKey = grain.GrainReference.GetPrimaryKeyString();
            var addressableKey = ((IAddressable)grain).GetPrimaryKeyString();

            key.Should().Be(id);
            keyString.Should().Be(id);
            referenceKey.Should().Be(id);
            addressableKey.Should().Be(id);
        }
    }
}

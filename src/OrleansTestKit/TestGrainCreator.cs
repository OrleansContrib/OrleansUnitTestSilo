using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Orleans.Core;
using Orleans.Runtime;

namespace Orleans.TestKit
{
    public class TestGrainCreator
    {
        private readonly PropertyInfo _runtimeProperty;
        private readonly FieldInfo _identityField;
        private readonly FieldInfo _activationDataField;
        private readonly MethodInfo _grainReferenceFromGrainMethod;
        private readonly Func<long, long, string, object> _createGrainIdLong;
        private readonly Func<long, Guid, string, object> _createGrainIdGuid;
        private readonly Func<long, string, object> _createGrainIdString;
        private readonly FieldInfo _grainReferenceField;
        private readonly Type _activationDataType;

        private readonly IGrainActivator _activator;
        private readonly IGrainRuntime _runtime;
        private readonly MethodInfo _newActivationAddressMethod;
        private readonly FieldInfo _addressField;
        private FieldInfo _itemsField;


        public TestGrainCreator(IGrainRuntime runtime, IServiceProvider serviceProvider)
        {
            _runtime = runtime;
            _identityField = typeof(Grain).GetField("Identity", BindingFlags.Instance | BindingFlags.NonPublic);
            _runtimeProperty = typeof(Grain).GetProperty("Runtime", BindingFlags.Instance | BindingFlags.NonPublic);

            _grainReferenceFromGrainMethod = typeof(GrainReference).GetMethod("FromGrainId", BindingFlags.Static | BindingFlags.NonPublic);
            _activationDataField = typeof(Grain).GetField("Data", BindingFlags.Instance | BindingFlags.NonPublic);

            var grainIdType = Type.GetType("Orleans.Runtime.GrainId, Orleans.Core.Abstractions");
            if (grainIdType == null)
                throw new InvalidOperationException();

            var getGrainIdMethods = grainIdType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(p => p.Name == "GetGrainId").ToArray();
            foreach (var method in getGrainIdMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 2 && parameters[1].ParameterType == typeof(string))
                {
                    _createGrainIdString = (Func<long, string, object>)method.CreateDelegate(typeof(Func<long, string, object>));
                }
                else if (parameters.Length == 3 && parameters[1].ParameterType == typeof(Guid))
                {
                    _createGrainIdGuid = (Func<long, Guid, string, object>)method.CreateDelegate(typeof(Func<long, Guid, string, object>));
                }
                else if (parameters.Length == 3 && parameters[1].ParameterType == typeof(long))
                {
                    _createGrainIdLong = (Func<long, long, string, object>)method.CreateDelegate(typeof(Func<long, long, string, object>));
                }
            }

            _activationDataType = Type.GetType("Orleans.Runtime.ActivationData, Orleans.Runtime");
            if (_activationDataType == null)
                throw new InvalidOperationException();

            _grainReferenceField = _activationDataType.GetField("GrainReference", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_grainReferenceField == null)
                throw new InvalidOperationException();

            _addressField = _activationDataType.GetField("<Address>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_addressField == null)
                throw new InvalidOperationException();

            _itemsField = _activationDataType.GetField("<Items>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_itemsField == null)
                throw new InvalidOperationException();

            var activationAddressType = Type.GetType("Orleans.Runtime.ActivationAddress, Orleans.Core.Abstractions");
            if (activationAddressType == null)
                throw new InvalidOperationException();

            _newActivationAddressMethod = activationAddressType.GetMethod("NewActivationAddress");
            if (_newActivationAddressMethod == null)
                throw new InvalidOperationException();

            _activator = new DefaultGrainActivator(serviceProvider);
        }

        public Grain CreateGrainInstance(IGrainActivationContext context)
        {
            var grain = (Grain)_activator.Create(context);

            var participant = grain as ILifecycleParticipant<IGrainLifecycle>;
            participant?.Participate(context.ObservableLifecycle);

            //Set the runtime and identity. This is equivalent to what Orleans' GrainCreator does
            //when creating new grains. It is messier but easier than trying to wrangle the values
            //in via a constructor which may or may exist on types inheriting from Grain.
            _runtimeProperty.SetValue(grain, _runtime);
            _identityField.SetValue(grain, context.GrainIdentity);

            _activationDataField.SetValue(grain, CreateActivationData(context));

            return grain;
        }

        private object CreateActivationData(IGrainActivationContext context)
        {
            var typeCode = context.GrainType.GUID.GetHashCode();
            var grainId = CreateGrainId((TestGrainIdentity)context.GrainIdentity, typeCode);

            var activationAddress = _newActivationAddressMethod.Invoke(null, new[] { SiloAddress.Zero, grainId });

            var grainReference = _grainReferenceFromGrainMethod.Invoke(null, new[] { grainId, null, null, null });
            var activationData = FormatterServices.GetUninitializedObject(_activationDataType);
            _grainReferenceField.SetValue(activationData, grainReference);
            _addressField.SetValue(activationData, activationAddress);
            _itemsField.SetValue(activationData, new Dictionary<object, object>());
            return activationData;
        }

        private object CreateGrainId(TestGrainIdentity grainIdentity, long typeCode)
        {
            switch (grainIdentity.KeyType)
            {
                case TestGrainIdentity.KeyTypes.String:
                    return _createGrainIdString(typeCode, grainIdentity.PrimaryKeyString);
                case TestGrainIdentity.KeyTypes.GuidCompound:
                case TestGrainIdentity.KeyTypes.Guid:
                    return _createGrainIdGuid(typeCode, grainIdentity.PrimaryKey, grainIdentity.KeyExtension);
                case TestGrainIdentity.KeyTypes.LongCompound:
                case TestGrainIdentity.KeyTypes.Long:
                    return _createGrainIdLong(typeCode, grainIdentity.PrimaryKeyLong, grainIdentity.KeyExtension);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

using UnityEngine;

namespace IndieGabo.HandyTools.HandyServiceLocator
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ServiceLocator))]
    public abstract class ServiceBootstrapper : MonoBehaviour
    {
        protected ServiceLocator _container;
        internal ServiceLocator Container => _container ??= GetComponent<ServiceLocator>();

        protected bool _hasBeenBootstrapped;

        protected void Awake() => BootstrapOnDemand();

        public void BootstrapOnDemand()
        {
            if (_hasBeenBootstrapped) return;

            _hasBeenBootstrapped = true;
            Bootstrap();
        }

        protected abstract void Bootstrap();
    }
}
using Castle.Windsor;

namespace InterShareMobile.Core
{
    public class Ioc
    {
        private static Ioc? _instance;
        private static readonly object _syncRoot = new object();

        private readonly WindsorContainer _iocContainer;

        public static Ioc Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (_syncRoot)
                {
                    _instance ??= new Ioc();
                }

                return _instance;
            }
        }

        private Ioc()
        {
            _iocContainer = new WindsorContainer();
        }

        public static void Register<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : TInterface
        {
            Instance.RegisterImplementation<TInterface, TImplementation>();
        }

        private void RegisterImplementation<TInterface, TImplementation>()
            where TInterface: class
            where TImplementation: TInterface
        {
            _iocContainer.Register(Castle.MicroKernel.Registration.Component.For<TInterface>().ImplementedBy<TImplementation>());
        }

        public static TInterface Resolve<TInterface>()
            where TInterface : class
        {
            return Instance.ResolveImplementation<TInterface>();
        }

        private TInterface ResolveImplementation<TInterface>()
            where TInterface : class
        {
            return _iocContainer.Resolve<TInterface>();
        }

        public static void Close()
        {
            Instance._iocContainer.Dispose();
        }
    }
}
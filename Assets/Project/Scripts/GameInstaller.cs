using Zenject;

namespace Project.Scripts
{
    public class GameInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MirrorNetworkMessagingService>()
                .AsSingle()
                .NonLazy();
            
            Container.Bind<NetworkTestController>()
                .FromComponentInHierarchy()
                .AsSingle();
        }
    }
}
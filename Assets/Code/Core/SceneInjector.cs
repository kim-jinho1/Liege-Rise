using System.Collections.Generic;
using Reflex.Core;
using UnityEngine;

namespace Code.Core
{
    public class SceneInjector : MonoBehaviour, IInstaller
    {
        [SerializeField] private List<MonoBehaviour> dependencies;

        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            foreach (MonoBehaviour dependency in dependencies)
                containerBuilder.RegisterValue(dependency);
        }
    }
}

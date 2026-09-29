using UnityEngine;

namespace Code.Core
{
    /// <summary>
    /// 씬 단위 MonoBehaviour 싱글톤 베이스입니다.
    /// 인스턴스가 비어 있으면 씬에서 자동 탐색하므로 씬 리로드 후에도 자가 복구됩니다.
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        /// <summary>
        /// 현재 씬의 싱글톤 인스턴스를 반환합니다. 없으면 씬에서 탐색하며, 그래도 없으면 null을 반환합니다.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindAnyObjectByType<T>();
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
                Debug.LogWarning(
                    $"[{typeof(T).Name}] 씬에 둘 이상 있습니다: '{_instance.name}' → '{name}'로 교체됩니다.", this);

            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}

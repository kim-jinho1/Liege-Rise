namespace Code.Core
{
    /// <summary>
    /// 인터페이스 뒤에 숨은 Unity 오브젝트가 살아 있는지 봅니다.
    /// 인터페이스 변수에 ?.이나 is null을 쓰면 참조만 보게 되어,
    /// Unity가 "파괴됨"을 null로 보여 주는 == 오버로드를 그냥 지나칩니다.
    /// </summary>
    public static class UnityRef
    {
        /// <summary>
        /// 참조가 있고, Unity 오브젝트라면 파괴되지도 않았는지입니다.
        /// 순수 C# 객체는 참조만 봅니다.
        /// </summary>
        public static bool IsAlive(object candidate)
            => candidate is UnityEngine.Object unityObject ? unityObject != null : candidate != null;

        /// <summary>
        /// 파괴된 Unity 오브젝트를 진짜 null로 바꿔 돌려줍니다.
        /// 뒤에 ?.을 붙여도 안전해집니다 — UnityRef.Alive(Foo.Instance)?.Bar().
        /// MonoSingleton은 씬을 내릴 때 파괴된 인스턴스를 그대로 들고 있습니다.
        /// </summary>
        public static T Alive<T>(T candidate) where T : UnityEngine.Object
            => candidate != null ? candidate : null;
    }
}

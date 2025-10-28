using UnityEngine;

namespace UnityUtils {
    public static class GameObjectExtensions {
        public static T GetOrAdd<T>(this GameObject go) where T : Component {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }

    public static class Logwin {
        public static void Log(string channel, object message) {
            Debug.Log($"[{channel}] {message}");
        }
    }
}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace TMRI.Core
{
    public static class QuaternionExtensions
    {
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 ToAngularVelocity(this Quaternion q)
        {
            if (Mathf.Abs(q.w) > 1023.5f / 1024.0f)
                return new Vector3();
            var angle = Mathf.Acos(Mathf.Abs(q.w));
            var gain = Mathf.Sign(q.w) * 2.0f * angle / Mathf.Sin(angle);
            return new Vector3(q.x * gain, q.y * gain, q.z * gain);
        }
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Quaternion FromAngularVelocity(this Vector3 w)
        {
            var mag = w.magnitude;
            if (mag <= 0)
                return Quaternion.identity;
            var cs = Mathf.Cos(mag * 0.5f);
            var siGain = Mathf.Sin(mag * 0.5f) / mag;
            return new Quaternion(w.x * siGain, w.y * siGain, w.z * siGain, cs);
        }
        public static Quaternion Average(Quaternion[] source)
        {
            Assert.IsFalse(source == null || !source.Any());
            Vector3 result = new Vector3();
            foreach (var q in source)
            {
                result += q.ToAngularVelocity();
            }
            return (result / source.Length).FromAngularVelocity();
        }
    }

    public static class TMRIListenerUtility
    {
        public static void ExecuteOnListeners<T>(this MonoBehaviour context, Action<T> action, FindObjectsInactive includeInactive = FindObjectsInactive.Exclude) where T : class
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var listeners = MonoBehaviour.FindObjectsByType<MonoBehaviour>(
                includeInactive,
                FindObjectsSortMode.None
            ).Where(mb => mb.enabled).OfType<T>();

            foreach (var listener in listeners)
            {
                action(listener);
            }
        }
    }
}
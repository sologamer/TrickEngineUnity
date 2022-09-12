using System;
using System.Collections.Generic;

namespace TrickCore
{
    /// <summary>
    /// Basically a KeyValuePair, but this one works for Odin
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T2"></typeparam>
    [Preserve, Serializable]
    public sealed class TrickPair<T, T2> : ITrickPair
    {
        public T Key;
        public T2 Value;

        public TrickPair()
        {
        
        }

        public TrickPair(T a, T2 b)
        {
            Key = a;
            Value = b;
        }

        public string ListElementKey => $"{Key}";
        public string ListElementValue => $"{Value}";

        public KeyValuePair<T, T2> ToPair() => new KeyValuePair<T, T2>(Key, Value);

        protected bool Equals(TrickPair<T, T2> other)
        {
            return EqualityComparer<T>.Default.Equals(Key, other.Key) && EqualityComparer<T2>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TrickPair<T, T2>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(Key) * 397) ^ EqualityComparer<T2>.Default.GetHashCode(Value);
            }
        }

        public static bool operator ==(TrickPair<T, T2> left, TrickPair<T, T2> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TrickPair<T, T2> left, TrickPair<T, T2> right)
        {
            return !Equals(left, right);
        }

        public object GetKey()
        {
            return Key;
        }

        public object GetValue()
        {
            return Value;
        }

        public override string ToString()
        {
            return $"[{Key}, {Value}]";
        }
    }

    public interface ITrickPair
    {
        object GetKey();
        object GetValue();
    }
}
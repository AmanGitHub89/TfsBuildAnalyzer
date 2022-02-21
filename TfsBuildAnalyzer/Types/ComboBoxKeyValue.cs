using System;


namespace TfsBuildAnalyzer.Types
{
    internal class ComboBoxKeyValue : IEquatable<ComboBoxKeyValue>
    {
        public string Key { get; }
        public string Value { get; }

        public ComboBoxKeyValue(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public bool Equals(ComboBoxKeyValue other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Key.Equals(other.Key) && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ComboBoxKeyValue)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key != null ? Key.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return Value;
        }
    }
}

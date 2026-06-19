using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Synapse.Reflection.Models;

public class SettableProperty : INotifyPropertyChanged
{
    public required string Name { get; set; }
    public required Type Type { get; set; }
    public virtual bool UserSettable => false;
    
    
    private object? _value = null;
    public object? Value
    {
        get => _value;
        set
        {
            if (value is null)
            {
                if (Type.IsValueType && Nullable.GetUnderlyingType(Type) is null)
                    throw new ArgumentNullException(Name, $"Non-nullable parameter '{Name}' requires a value of type {Type.Name}.");
                _value = null;
                SetField(ref _value, null);
                return;
            }

            if (Type.IsInstanceOfType(value))
            {
                SetField(ref _value, value);
                OnPropertyChanged();
                return;
            }

            throw new ArgumentException(
                $"Parameter '{Name}' expects {Type.FullName}, but got {value.GetType().FullName}.", Name);
        }
    }
    
    public override string ToString()
    {
        if (Value is null) return $"{Type.Name} {Name}";
        if (Value is IEnumerable e && Value is not string)
        {
            var items = new List<string>();
            foreach (var item in e) items.Add(item is string s ? $"\"{s}\"" : item?.ToString() ?? "null");
            return $"{Type.Name} {Name} = [{string.Join(", ", items)}]";
        }
        var valStr = Value is string vs ? $"\"{vs}\"" : Value.ToString();
        return $"{Type.Name} {Name} = {valStr}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

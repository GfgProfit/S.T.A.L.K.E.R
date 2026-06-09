using System;
using System.Linq;
using System.Reflection;

internal static class DiConstructorSelector
{
    public static ConstructorInfo Select(Type type)
    {
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Concat(type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance))
            .ToArray();

        ConstructorInfo[] markedConstructors = constructors
            .Where(constructor => constructor.GetCustomAttributes(typeof(InjectAttribute), true).Any())
            .ToArray();

        if (markedConstructors.Length > 1)
        {
            throw new InvalidOperationException($"{type.Name} has multiple constructors marked with [Inject].");
        }

        if (markedConstructors.Length == 1)
        {
            return markedConstructors[0];
        }

        return constructors.OrderByDescending(constructor => constructor.GetParameters().Length).FirstOrDefault()
            ?? throw new InvalidOperationException($"{type.Name} has no constructor.");
    }
}

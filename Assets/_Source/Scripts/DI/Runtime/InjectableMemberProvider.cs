using System;
using System.Collections.Generic;
using System.Reflection;

internal sealed class InjectableMemberProvider
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly Dictionary<Type, InjectableMember[]> _membersByType = new();

    public InjectableMember[] GetMembers(Type type)
    {
        if (type == null)
        {
            return Array.Empty<InjectableMember>();
        }

        if (!_membersByType.TryGetValue(type, out InjectableMember[] members))
        {
            members = BuildMembers(type);
            _membersByType[type] = members;
        }

        return members;
    }

    private static InjectableMember[] BuildMembers(Type type)
    {
        List<InjectableMember> members = new();

        AddFields(type, members);
        AddProperties(type, members);

        return members.ToArray();
    }

    private static void AddFields(Type type, List<InjectableMember> members)
    {
        foreach (FieldInfo fieldInfo in type.GetFields(Flags))
        {
            InjectAttribute injectAttribute = fieldInfo.GetCustomAttribute<InjectAttribute>();

            if (injectAttribute == null)
            {
                continue;
            }

            members.Add(new InjectableMember(
                fieldInfo.FieldType,
                "field",
                fieldInfo.Name,
                injectAttribute.Optional,
                (target, value) => fieldInfo.SetValue(target, value)));
        }
    }

    private static void AddProperties(Type type, List<InjectableMember> members)
    {
        foreach (PropertyInfo propertyInfo in type.GetProperties(Flags))
        {
            if (!propertyInfo.CanWrite)
            {
                continue;
            }

            InjectAttribute injectAttribute = propertyInfo.GetCustomAttribute<InjectAttribute>();

            if (injectAttribute == null)
            {
                continue;
            }

            members.Add(new InjectableMember(
                propertyInfo.PropertyType,
                "property",
                propertyInfo.Name,
                injectAttribute.Optional,
                (target, value) => propertyInfo.SetValue(target, value)));
        }
    }
}

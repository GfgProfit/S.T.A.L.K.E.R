using System;

internal sealed class DiInjector
{
    private readonly InjectableMemberProvider _memberProvider = new();

    public void Inject(object target, Func<Type, object> resolver)
    {
        if (target == null)
        {
            return;
        }

        if (resolver == null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        Type ownerType = target.GetType();
        InjectableMember[] members = _memberProvider.GetMembers(ownerType);

        for (int i = 0; i < members.Length; i++)
        {
            TryAssign(target, ownerType, members[i], resolver);
        }
    }

    private static void TryAssign(object target, Type ownerType, InjectableMember member, Func<Type, object> resolver)
    {
        try
        {
            object value = resolver(member.DependencyType);

            if (value == null && !member.Optional)
            {
                throw new InvalidOperationException($"{ownerType.Name}.{member.Name} ({member.Kind}) requires {member.DependencyType.Name}, but it resolved to null.");
            }

            member.SetValue(target, value);
        }
        catch (Exception exception)
        {
            if (!member.Optional)
            {
                throw new InvalidOperationException($"{ownerType.Name}.{member.Name} ({member.Kind}) requires {member.DependencyType.Name}, but it couldn't be resolved.", exception);
            }
        }
    }
}

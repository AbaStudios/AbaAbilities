using System;

namespace AbaAbilities.Common.Attachments
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AllowMultipleInstancesAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class NoAutoActivateAttribute : Attribute { }
}
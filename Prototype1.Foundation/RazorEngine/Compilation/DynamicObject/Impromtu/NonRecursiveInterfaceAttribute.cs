using System;

namespace RazorEngine.Compilation.ImpromptuInterface
{
    /// <summary>
    /// Attribute on Inteface to stop proxy from recursively
    /// proxying other interfaces
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Method |
                          System.AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Property)]
    public class NonRecursiveInterfaceAttribute : Attribute
    {
    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace HelpLine.Tests;

internal static class ApiContract
{
    public static string GenerateContractForAssembly(Assembly assembly)
    {
        StringBuilder output = new();
        var types = assembly.GetExportedTypes().OrderBy(t => t.FullName).ToArray();
        var namespaces = types.Select(t => t.Namespace).Distinct().OrderBy(n => n).ToArray();

        HashSet<MethodInfo> printedMethods = [];

        foreach (var ns in namespaces)
        {
            output.AppendLine(ns);

            foreach (var type in types.Where(t => t.Namespace == ns))
            {
                var isDelegate = typeof(Delegate).IsAssignableFrom(type);

                var typeKind = type.IsValueType
                    ? type.IsEnum
                        ? "enum"
                        : "struct"
                    : isDelegate
                        ? "delegate"
                        : type.IsInterface
                            ? "interface"
                            : "class";

                output.Append($"  {type.GetAccessModifiers()} {typeKind} {type.GetReadableTypeName(ns)}");

                if (type.BaseType is { } baseType && baseType != typeof(object))
                {
                    output.Append($" : {baseType.GetReadableTypeName(ns)}");
                }

                if (type.GetInterfaces().OrderBy(i => i.FullName).ToArray() is { Length: > 0 } interfaces)
                {
                    for (var i = 0; i < interfaces.Length; i++)
                    {
                        var @interface = interfaces[i];
                        var delimiter = i == 0 && type.IsInterface ? " : " : ", ";
                        output.Append($"{delimiter}{@interface.GetReadableTypeName(ns)}");
                    }
                }

                output.AppendLine();

                if (type.IsEnum)
                {
                    WriteContractForEnum(type, output);
                }
                else
                {
                    WriteContractForClassOrStruct(type, printedMethods, output);
                }
            }
        }

        return output.ToString();
    }

    private static void WriteContractForEnum(Type type, StringBuilder output)
    {
        var names = Enum.GetNames(type);
        var values = Enum.GetValues(type).Cast<int>().ToArray();

        foreach (var (name, value) in names.Zip(values.Select(v => v.ToString())))
        {
            output.AppendLine($"    {name}={value}");
        }
    }

    private static void WriteContractForClassOrStruct(Type type, HashSet<MethodInfo> printedMethods, StringBuilder output)
    {
        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                   .Where(m => m.DeclaringType == type && !m.IsAssembly && !m.IsFamilyAndAssembly && !m.IsPrivate)
                                   .OrderBy(m => m.Name)
                                   .ThenBy(m => m.GetParameters().Length))
        {
            if (printedMethods.Add(method))
            {
                output.AppendLine($"    {GetMethodSignature(method, type.Namespace!)}");
            }
        }

        foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 .Where(m => m.DeclaringType == type)
                                 .Where(m => !m.IsAssembly && !m.IsFamilyAndAssembly && !m.IsPrivate)
                                 .OrderBy(m => m.Name))
        {
            output.AppendLine($"    .ctor({GetParameterSignatures(ctor.GetParameters(), false, type.Namespace!)})");
        }

        foreach (var prop in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 .Where(m => m.DeclaringType == type)
                                 .OrderBy(c => c.Name))
        {
            if (prop.GetMethod?.IsPublic == true && printedMethods.Add(prop.GetMethod))
            {
                var setter = prop.GetSetMethod();
                if (setter is not null)
                {
                    printedMethods.Add(setter);
                }

                output.AppendLine($"    {GetPropertySignature(prop, type.Namespace!)}");
            }
        }

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                 .Where(m => m.DeclaringType == type)
                                 .OrderBy(c => c.Name))
        {
            if (prop.GetMethod is { IsPublic: true, IsAssembly: false } && printedMethods.Add(prop.GetMethod))
            {
                var setter = prop.GetSetMethod();
                if (setter is not null)
                {
                    printedMethods.Add(setter);
                }

                output.AppendLine($"    {GetPropertySignature(prop, type.Namespace!)}");
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                   .Where(m => m.DeclaringType == type && !m.IsAssembly && !m.IsFamilyAndAssembly && !m.IsPrivate && !m.IsPropertyAccessor())
                                   .OrderBy(m => m.Name)
                                   .ThenBy(m => m.GetParameters().Length))
        {
            if (printedMethods.Add(method))
            {
                output.AppendLine($"    {GetMethodSignature(method, type.Namespace!)}");
            }
        }
    }

    public static string GetPropertySignature(this PropertyInfo property, string omitNamespace)
    {
        var getter = property.GetGetMethod();
        var setter = property.GetSetMethod();

        var (getterVisibility, getterScope) = GetAccessModifiers(getter);
        var (setterVisibility, _) = GetAccessModifiers(setter);

        string? overallVisibility = null;

        switch (getterVisibility, setterVisibility)
        {
            case (string g, string s) when g == s:
                overallVisibility = getterVisibility;
                getterVisibility = null;
                setterVisibility = null;
                break;

            case ({ } g, null):
                overallVisibility = g;
                getterVisibility = null;
                break;

            case (null, { } s):
                overallVisibility = s;
                setterVisibility = null;
                break;
        }

        var getterSignature = getter is { } ? $"{getterVisibility} get; " : string.Empty;
        var setterSignature = setter is { } ? $"{setterVisibility} set; " : string.Empty;

        return $"{overallVisibility} {getterScope} {GetReadableTypeName(property.PropertyType, omitNamespace)} {property.Name} {{ {getterSignature}{setterSignature}}}"
            .Replace("  ", " ");
    }

    public static string GetMethodSignature(this MethodInfo method, string omitNamespace)
    {
        var (methodVisibility, methodScope) = GetAccessModifiers(method);
        var genericArgs = method.IsGenericMethod
            ? $"<{string.Join(", ", method.GetGenericArguments().Select(a => GetReadableTypeName(a, omitNamespace)))}>"
            : string.Empty;

        var isExtensionMethod = method.IsDefined(typeof(ExtensionAttribute), false);
        var parameters = GetParameterSignatures(method.GetParameters(), isExtensionMethod, omitNamespace);

        return $"{methodVisibility} {methodScope} {GetReadableTypeName(method.ReturnType, omitNamespace)} {method.Name}{genericArgs}({parameters})"
            .Replace("  ", " ");
    }

    public static string GetParameterSignatures(this IEnumerable<ParameterInfo> parameters, bool isExtensionMethod, string omitNamespace)
    {
        var signature = parameters.Select(param =>
        {
            var prefix = string.Empty;

            if (param.ParameterType.IsByRef)
            {
                prefix = "ref ";
            }
            else if (param.IsOut)
            {
                prefix = "out ";
            }
            else if (isExtensionMethod && param.Position == 0)
            {
                prefix = "this ";
            }

            var result = prefix + $"{GetReadableTypeName(param.ParameterType, omitNamespace)} {param.Name}";

            if (param.HasDefaultValue)
            {
                result += $" = {param.DefaultValue ?? "null"}";
            }

            return result;
        });

        return string.Join(", ", signature);
    }

    private static (string? Visibility, string? Scope) GetAccessModifiers(this MethodBase? method)
    {
        if (method is null)
        {
            return (null, null);
        }

        string? visibility = null;
        string? scope = null;

        if (method.IsAssembly)
        {
            visibility = "internal";
            if (method.IsFamily)
            {
                visibility += " protected";
            }
        }
        else if (method.IsPublic)
        {
            visibility = "public";
        }
        else if (method.IsPrivate)
        {
            visibility = "private";
        }
        else if (method.IsFamily)
        {
            visibility = "protected";
        }

        if (method.IsStatic)
        {
            scope = "static";
        }

        return (visibility, scope);
    }

    private static string GetAccessModifiers(this Type type)
    {
        var modifier = string.Empty;

        if (type.IsPublic)
        {
            modifier = "public";
        }

        if (type.IsAbstract && !type.IsInterface)
        {
            modifier += type.IsSealed ? " static" : " abstract";
        }

        return modifier;
    }

    public static bool IsPropertyAccessor(this MethodInfo methodInfo) =>
        methodInfo.DeclaringType!.GetProperties().Any(prop => prop.GetSetMethod() == methodInfo || prop.GetGetMethod() == methodInfo);

    public static string GetReadableTypeName(this Type type, string omitNamespace)
    {
        StringBuilder builder = new();
        using StringWriter writer = new(builder);
        WriteCSharpDeclarationTo(type, writer, omitNamespace);
        writer.Flush();
        return builder.ToString();
    }

    private static void WriteCSharpDeclarationTo(this Type type, TextWriter writer, string omitNamespace)
    {
        var typeName = type.Namespace == omitNamespace
            ? type.Name
            : type.FullName ?? type.Name;

        if (type.IsByRef)
        {
            WriteCSharpDeclarationTo(type.GetElementType()!, writer, omitNamespace);
            return;
        }

        if (typeName.Contains('`', StringComparison.Ordinal))
        {
            writer.Write(typeName[..typeName.IndexOf('`')]);
            writer.Write('<');

            var genericArguments = type.GetGenericArguments();
            for (var i = 0; i < genericArguments.Length; i++)
            {
                var genericArgument = genericArguments[i];

                if (genericArgument.IsGenericParameter)
                {
                    if (genericArgument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant))
                    {
                        writer.Write("out ");
                    }
                    else if (genericArgument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.Contravariant))
                    {
                        writer.Write("in ");
                    }
                }

                WriteCSharpDeclarationTo(genericArgument, writer, omitNamespace);

                if (i < genericArguments.Length - 1)
                {
                    writer.Write(", ");
                }
            }

            writer.Write('>');
        }
        else
        {
            writer.Write(typeName);
        }
    }
}

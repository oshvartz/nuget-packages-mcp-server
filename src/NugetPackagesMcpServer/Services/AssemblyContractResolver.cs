using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackagesMcpServer.Services
{
    public class AssemblyContractResolver : IAssemblyContractResolver
    {
        private static readonly HashSet<string> _methodsToExclude = new HashSet<string>
        {
            "Equals",
            "GetHashCode",
            "GetType",
            "ToString",
            "Clone",
            "<Clone>"
        };

        public string ResolveAssemblyContractToMarkdown(Assembly assembly)
        {
            var markdownBuilder = new StringBuilder();
            markdownBuilder.AppendLine($"# Assembly: {assembly.GetName().Name}");
            markdownBuilder.AppendLine();

            Type?[] publicTypes;

            try
            {
                publicTypes = assembly.GetTypes();

            }
            catch (ReflectionTypeLoadException ex)
            {
                // Handle types that could not be loaded
                publicTypes = ex.Types.Where(t => t != null).ToArray();
                markdownBuilder.AppendLine("Some types could not be loaded:");
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    markdownBuilder.AppendLine($"- {loaderException!.Message}");
                }
            }

            var interfaces = publicTypes.Where(t => t!.IsInterface).OrderBy(t => t!.Name);
            if (interfaces.Any())
            {
                markdownBuilder.AppendLine("## Interfaces");
                foreach (var i in interfaces)
                {
                    AppendTypeInfo(markdownBuilder, i!);
                }
            }

            var classes = publicTypes.Where(t => t!.IsClass && !t.IsInterface && !IsStatic(t)).OrderBy(t => t!.Name);
            if (classes.Any())
            {
                markdownBuilder.AppendLine("## Classes");
                foreach (var c in classes)
                {
                    AppendTypeInfo(markdownBuilder, c!);
                }
            }

            var structs = publicTypes.Where(t => t!.IsValueType && !t.IsEnum).OrderBy(t => t!.Name);
            if (structs.Any())
            {
                markdownBuilder.AppendLine("## Structs");
                foreach (var s in structs)
                {
                    AppendTypeInfo(markdownBuilder, s!);
                }
            }

            var enums = publicTypes.Where(t => t!.IsEnum).OrderBy(t => t!.Name);
            if (enums.Any())
            {
                markdownBuilder.AppendLine("## Enums");
                foreach (var e in enums)
                {
                    AppendEnumInfo(markdownBuilder, e!);
                }
            }

            var extensionMethodClasses = publicTypes.Where(t => IsStatic(t!) && t!.GetMethods(BindingFlags.Static | BindingFlags.Public).Any(m => m.IsDefined(typeof(ExtensionAttribute), false)));
            if (extensionMethodClasses.Any())
            {
                markdownBuilder.AppendLine("## Extension Methods");
                foreach (var type in extensionMethodClasses)
                {
                    var extensionMethods = type!.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                               .Where(m => m.IsDefined(typeof(ExtensionAttribute), false));

                    foreach (var method in extensionMethods)
                    {
                        AppendMethodInfo(markdownBuilder, method, isExtension: true);
                    }
                }
            }

            return markdownBuilder.ToString();
        }

        private static bool IsStatic(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        private static void AppendTypeInfo(StringBuilder markdownBuilder, Type type)
        {
            markdownBuilder.AppendLine($"### {FormatTypeName(type)}");
            markdownBuilder.AppendLine("```csharp");

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                 .OrderBy(p => p.Name);
            if (properties.Any())
            {
                markdownBuilder.AppendLine("// Properties");
                foreach (var prop in properties)
                {
                    var getter = prop.GetGetMethod(true);
                    var setter = prop.GetSetMethod(true);
                    if ((getter != null && getter.IsPublic) || (setter != null && setter.IsPublic))
                    {
                        try
                        {
                            markdownBuilder.Append($"public {FormatTypeName(prop.PropertyType)} {prop.Name} {{ ");
                            if (prop.CanRead && getter!.IsPublic) markdownBuilder.Append("get; ");
                            if (prop.CanWrite && setter!.IsPublic) markdownBuilder.Append("set; ");
                            markdownBuilder.AppendLine("}");
                        }
                        catch (FileNotFoundException)
                        {
                            //fail to load dependency, skip this property
                            continue;
                        }
                    }
                }
                markdownBuilder.AppendLine();
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                              .Where(m => !m.IsSpecialName && !_methodsToExclude.Contains(m.Name))
                              .OrderBy(m => m.Name);
            if (methods.Any())
            {
                markdownBuilder.AppendLine("// Methods");
                foreach (var method in methods)
                {
                    AppendMethodInfo(markdownBuilder, method);
                }
            }

            markdownBuilder.AppendLine("```");
            markdownBuilder.AppendLine();
        }

        private static void AppendEnumInfo(StringBuilder markdownBuilder, Type type)
        {
            markdownBuilder.AppendLine($"### {FormatTypeName(type)}");
            markdownBuilder.AppendLine("```csharp");
            foreach (var name in Enum.GetNames(type))
            {
                markdownBuilder.AppendLine($"{name},");
            }
            markdownBuilder.AppendLine("```");
            markdownBuilder.AppendLine();
        }

        private static void AppendMethodInfo(StringBuilder markdownBuilder, MethodInfo method, bool isExtension = false)
        {
            try
            {
                var returnType = method.ReturnType?.ToString();
                var parameters = method.GetParameters().Select(p =>
                {
                    var thisMod = isExtension && p.Position == 0 ? "this " : "";
                    return $"{thisMod}{FormatTypeName(p.ParameterType)} {p.Name}";
                });
                var staticMod = method.IsStatic && !isExtension ? "static " : "";
                markdownBuilder.AppendLine($"public {staticMod}{returnType} {method.Name}({string.Join(", ", parameters)});");
            }
            catch (Exception)
            {
                //fail to load dependency, skip this method
                return;
            }
        }

        private static string FormatTypeName(Type type)
        {
            if (type == null) return "null";
            if (type == typeof(void)) return "void";
            if (type.IsGenericType)
            {
                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
                return $"{type.Name.Split('`')[0]}<{genericArgs}>";
            }

            var typeName = type.Name switch
            {
                "String" => "string",
                "Int32" => "int",
                "Boolean" => "bool",
                "Double" => "double",
                "Object" => "object",
                "Void" => "void",
                _ => type.Name
            };
            return typeName;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace DotNetAspects.Fody
{
    /// <summary>
    /// Fody weaver that implements method interception for DotNetAspects.
    /// Supports MethodInterceptionAspect, OnMethodBoundaryAspect, and LocationInterceptionAspect.
    /// </summary>
    public class ModuleWeaver : BaseModuleWeaver
    {
        // MethodInterceptionAspect types
        private TypeReference? _methodInterceptionAspectType;
        private TypeReference? _methodInterceptionArgsType;
        private TypeReference? _argumentsType;
        private TypeReference? _methodInfoType;
        private MethodReference? _argumentsCtor;
        private MethodReference? _methodInterceptionArgsCtorWithMethodInfo;
        private MethodReference? _getTypeFromHandle;
        private MethodReference? _getMethodFromHandle;

        // OnMethodBoundaryAspect types
        private TypeReference? _onMethodBoundaryAspectType;
        private TypeReference? _methodExecutionArgsType;
        private MethodReference? _methodExecutionArgsCtor;

        // LocationInterceptionAspect types
        private TypeReference? _locationInterceptionAspectType;
        private TypeReference? _locationInterceptionArgsType;
        private MethodReference? _locationInterceptionArgsCtor;
        private TypeReference? _locationInfoType;
        private MethodReference? _locationInfoCtor;
        private MethodReference? _getPropertyFromHandle;

        public override void Execute()
        {
            if (!InitializeTypeReferences())
            {
                WriteWarning("DotNetAspects types not found. Skipping weaving.");
                return;
            }

            foreach (var type in ModuleDefinition.Types.ToList())
            {
                ProcessType(type);
            }
        }

        private bool InitializeTypeReferences()
        {
            try
            {
                // MethodInterceptionAspect
                _methodInterceptionAspectType = ResolveType("DotNetAspects.Interception.MethodInterceptionAspect");
                _methodInterceptionArgsType = ResolveType("DotNetAspects.Args.MethodInterceptionArgs");
                _argumentsType = ResolveType("DotNetAspects.Args.Arguments");

                // OnMethodBoundaryAspect
                _onMethodBoundaryAspectType = ResolveType("DotNetAspects.Interception.OnMethodBoundaryAspect");
                _methodExecutionArgsType = ResolveType("DotNetAspects.Args.MethodExecutionArgs");

                // LocationInterceptionAspect
                _locationInterceptionAspectType = ResolveType("DotNetAspects.Interception.LocationInterceptionAspect");
                _locationInterceptionArgsType = ResolveType("DotNetAspects.Args.LocationInterceptionArgs");

                if (_methodInterceptionAspectType == null || _methodInterceptionArgsType == null || _argumentsType == null)
                {
                    return false;
                }

                // Get Arguments constructor that takes object[]
                var argsTypeDef = _argumentsType.Resolve();
                var argsCtor = argsTypeDef?.Methods.FirstOrDefault(m =>
                    m.IsConstructor && m.Parameters.Count == 1 &&
                    m.Parameters[0].ParameterType.IsArray);
                if (argsCtor != null)
                {
                    _argumentsCtor = ModuleDefinition.ImportReference(argsCtor);
                }
                else
                {
                    WriteError("Could not find Arguments(object[]) constructor");
                    return false;
                }

                // Get MethodInterceptionArgs constructor that takes (object, MethodBase, IArguments, MethodInfo)
                var miaTypeDef = _methodInterceptionArgsType.Resolve();
                var miaCtor = miaTypeDef?.Methods.FirstOrDefault(m =>
                    m.IsConstructor && m.Parameters.Count == 4 &&
                    m.Parameters[3].ParameterType.FullName == "System.Reflection.MethodInfo");
                if (miaCtor != null)
                {
                    _methodInterceptionArgsCtorWithMethodInfo = ModuleDefinition.ImportReference(miaCtor);
                }
                else
                {
                    WriteError("Could not find MethodInterceptionArgs constructor with MethodInfo parameter");
                    return false;
                }

                // Get MethodExecutionArgs constructor
                if (_methodExecutionArgsType != null)
                {
                    var meaTypeDef = _methodExecutionArgsType.Resolve();
                    var meaCtor = meaTypeDef?.Methods.FirstOrDefault(m => m.IsConstructor && !m.HasParameters);
                    if (meaCtor != null)
                    {
                        _methodExecutionArgsCtor = ModuleDefinition.ImportReference(meaCtor);
                    }
                }

                // Get LocationInterceptionArgs constructor
                if (_locationInterceptionArgsType != null)
                {
                    var liaTypeDef = _locationInterceptionArgsType.Resolve();
                    var liaCtor = liaTypeDef?.Methods.FirstOrDefault(m => m.IsConstructor && !m.HasParameters);
                    if (liaCtor != null)
                    {
                        _locationInterceptionArgsCtor = ModuleDefinition.ImportReference(liaCtor);
                    }
                }

                // Get LocationInfo type and constructor
                _locationInfoType = ResolveType("DotNetAspects.Args.LocationInfo");
                if (_locationInfoType != null)
                {
                    var locInfoTypeDef = _locationInfoType.Resolve();
                    var locInfoCtor = locInfoTypeDef?.Methods.FirstOrDefault(m => m.IsConstructor && !m.HasParameters);
                    if (locInfoCtor != null)
                    {
                        _locationInfoCtor = ModuleDefinition.ImportReference(locInfoCtor);
                    }
                }

                // Get PropertyInfo.GetPropertyFromHandle
                var propertyInfoType = FindTypeDefinition("System.Reflection.PropertyInfo");
                if (propertyInfoType != null)
                {
                    var getPropMethod = propertyInfoType.Methods.FirstOrDefault(m =>
                        m.Name == "GetPropertyFromHandle" && m.Parameters.Count == 2);
                    if (getPropMethod != null)
                    {
                        _getPropertyFromHandle = ModuleDefinition.ImportReference(getPropMethod);
                    }
                }

                // Get system type references
                var typeType = FindTypeDefinition("System.Type");
                if (typeType == null)
                {
                    WriteError("Could not find System.Type");
                    return false;
                }
                _getTypeFromHandle = ModuleDefinition.ImportReference(
                    typeType.Methods.First(m => m.Name == "GetTypeFromHandle"));

                var methodBaseType = FindTypeDefinition("System.Reflection.MethodBase");
                if (methodBaseType == null)
                {
                    WriteError("Could not find System.Reflection.MethodBase");
                    return false;
                }
                _getMethodFromHandle = ModuleDefinition.ImportReference(
                    methodBaseType.Methods.First(m =>
                        m.Name == "GetMethodFromHandle" && m.Parameters.Count == 2));

                var methodInfoType = FindTypeDefinition("System.Reflection.MethodInfo");
                if (methodInfoType == null)
                {
                    WriteError("Could not find System.Reflection.MethodInfo");
                    return false;
                }
                _methodInfoType = ModuleDefinition.ImportReference(methodInfoType);

                return true;
            }
            catch (Exception ex)
            {
                WriteError($"Error initializing type references: {ex.Message}");
                return false;
            }
        }

        private TypeReference? ResolveType(string fullName)
        {
            try
            {
                foreach (var assemblyRef in ModuleDefinition.AssemblyReferences)
                {
                    var assembly = ModuleDefinition.AssemblyResolver.Resolve(assemblyRef);
                    if (assembly != null)
                    {
                        var type = assembly.MainModule.Types.FirstOrDefault(t => t.FullName == fullName);
                        if (type != null)
                        {
                            return ModuleDefinition.ImportReference(type);
                        }

                        // Check nested types
                        foreach (var parentType in assembly.MainModule.Types)
                        {
                            var nested = parentType.NestedTypes.FirstOrDefault(t => t.FullName == fullName);
                            if (nested != null)
                            {
                                return ModuleDefinition.ImportReference(nested);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore resolution errors
            }
            return null;
        }

        private void ProcessType(TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes.ToList())
            {
                ProcessType(nestedType);
            }

            // Process properties for LocationInterceptionAspect
            foreach (var property in type.Properties.ToList())
            {
                ProcessProperty(property);
            }

            foreach (var method in type.Methods.ToList())
            {
                ProcessMethod(method);
            }
        }

        private void ProcessProperty(PropertyDefinition property)
        {
            var aspects = GetLocationInterceptionAspects(property);
            if (!aspects.Any())
                return;

            WriteInfo($"Weaving property: {property.DeclaringType.FullName}.{property.Name}");

            try
            {
                WeaveLocationInterception(property, aspects.First());
            }
            catch (Exception ex)
            {
                WriteError($"Error weaving property {property.Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private List<CustomAttribute> GetLocationInterceptionAspects(PropertyDefinition property)
        {
            var aspects = new List<CustomAttribute>();

            if (_locationInterceptionAspectType == null)
                return aspects;

            var aspectTypeDef = _locationInterceptionAspectType.Resolve();
            if (aspectTypeDef == null)
                return aspects;

            // Check property attributes
            foreach (var attr in property.CustomAttributes)
            {
                if (InheritsFrom(attr.AttributeType.Resolve(), aspectTypeDef))
                {
                    aspects.Add(attr);
                }
            }

            // Check class-level attributes
            foreach (var attr in property.DeclaringType.CustomAttributes)
            {
                if (InheritsFrom(attr.AttributeType.Resolve(), aspectTypeDef))
                {
                    aspects.Add(attr);
                }
            }

            return aspects.OrderBy(GetAspectPriority).ToList();
        }

        private void ProcessMethod(MethodDefinition method)
        {
            if (method.IsAbstract || method.IsConstructor || !method.HasBody)
                return;

            if (method.Name.EndsWith("$Original") || method.Name.EndsWith("$Boundary"))
                return;

            // Check for MethodInterceptionAspect first
            var interceptionAspects = GetMethodInterceptionAspects(method);
            if (interceptionAspects.Any())
            {
                WriteInfo($"Weaving method (interception): {method.DeclaringType.FullName}.{method.Name}");
                try
                {
                    WeaveMethodInterception(method, interceptionAspects);
                }
                catch (Exception ex)
                {
                    WriteError($"Error weaving method {method.Name}: {ex.Message}\n{ex.StackTrace}");
                }
                return;
            }

            // Check for OnMethodBoundaryAspect
            var boundaryAspects = GetOnMethodBoundaryAspects(method);
            if (boundaryAspects.Any())
            {
                WriteInfo($"Weaving method (boundary): {method.DeclaringType.FullName}.{method.Name}");
                try
                {
                    WeaveOnMethodBoundary(method, boundaryAspects.First());
                }
                catch (Exception ex)
                {
                    WriteError($"Error weaving method {method.Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private List<CustomAttribute> GetMethodInterceptionAspects(MethodDefinition method)
        {
            var aspects = new List<CustomAttribute>();

            if (_methodInterceptionAspectType == null)
                return aspects;

            var aspectTypeDef = _methodInterceptionAspectType.Resolve();
            if (aspectTypeDef == null)
                return aspects;

            // Check method attributes
            foreach (var attr in method.CustomAttributes)
            {
                if (InheritsFrom(attr.AttributeType.Resolve(), aspectTypeDef))
                {
                    aspects.Add(attr);
                }
            }

            // Check class-level attributes
            foreach (var attr in method.DeclaringType.CustomAttributes)
            {
                if (InheritsFrom(attr.AttributeType.Resolve(), aspectTypeDef))
                {
                    aspects.Add(attr);
                }
            }

            return aspects.OrderBy(GetAspectPriority).ToList();
        }

        private List<CustomAttribute> GetOnMethodBoundaryAspects(MethodDefinition method)
        {
            var aspects = new List<CustomAttribute>();

            if (_onMethodBoundaryAspectType == null)
                return aspects;

            var aspectTypeDef = _onMethodBoundaryAspectType.Resolve();
            if (aspectTypeDef == null)
                return aspects;

            // Check method attributes
            foreach (var attr in method.CustomAttributes)
            {
                if (InheritsFrom(attr.AttributeType.Resolve(), aspectTypeDef))
                {
                    aspects.Add(attr);
                }
            }

            // Check class-level attributes
            foreach (var attr in method.DeclaringType.CustomAttributes)
            {
                if (InheritsFrom(attr.AttributeType.Resolve(), aspectTypeDef))
                {
                    aspects.Add(attr);
                }
            }

            return aspects.OrderBy(GetAspectPriority).ToList();
        }

        private int GetAspectPriority(CustomAttribute attr)
        {
            var prop = attr.Properties.FirstOrDefault(p => p.Name == "AspectPriority");
            if (prop.Argument.Value != null)
            {
                return (int)prop.Argument.Value;
            }
            return 0;
        }

        private bool InheritsFrom(TypeDefinition? type, TypeDefinition baseType)
        {
            if (type == null) return false;
            if (type.FullName == baseType.FullName) return true;

            var current = type;
            while (current?.BaseType != null)
            {
                var baseDef = current.BaseType.Resolve();
                if (baseDef == null) break;
                if (baseDef.FullName == baseType.FullName) return true;
                current = baseDef;
            }
            return false;
        }

        #region MethodInterceptionAspect Weaving

        private void WeaveMethodInterception(MethodDefinition method, List<CustomAttribute> aspects)
        {
            // 1. Clone original method body to new private method
            var originalMethod = CloneMethodBody(method, "$Original");
            method.DeclaringType.Methods.Add(originalMethod);

            // 2. Clear the original method and build interception logic
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.InitLocals = true;

            var il = method.Body.GetILProcessor();

            // For simplicity, only handle the first aspect for now
            var aspect = aspects.First();

            BuildInterceptionBody(method, originalMethod, aspect, il);
        }

        private void BuildInterceptionBody(MethodDefinition method, MethodDefinition originalMethod,
            CustomAttribute aspect, ILProcessor il)
        {
            // Local variables
            var aspectVar = new VariableDefinition(ModuleDefinition.ImportReference(aspect.AttributeType));
            var argsVar = new VariableDefinition(_methodInterceptionArgsType);
            var argumentsVar = new VariableDefinition(_argumentsType);
            var originalMethodVar = new VariableDefinition(_methodInfoType);

            method.Body.Variables.Add(aspectVar);      // 0
            method.Body.Variables.Add(argsVar);        // 1
            method.Body.Variables.Add(argumentsVar);   // 2
            method.Body.Variables.Add(originalMethodVar); // 3

            // === Create aspect instance ===
            var aspectCtor = aspect.AttributeType.Resolve().Methods
                .FirstOrDefault(m => m.IsConstructor && !m.HasParameters);

            if (aspectCtor == null)
            {
                WriteError($"Aspect {aspect.AttributeType.Name} has no parameterless constructor");
                il.Emit(OpCodes.Ret);
                return;
            }

            il.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(aspectCtor));

            // Set properties from attribute
            foreach (var prop in aspect.Properties)
            {
                il.Emit(OpCodes.Dup);
                EmitLdcValue(il, prop.Argument.Value, prop.Argument.Type);

                var setter = aspect.AttributeType.Resolve().Properties
                    .FirstOrDefault(p => p.Name == prop.Name)?.SetMethod;
                if (setter != null)
                {
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(setter));
                }
                else
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Pop);
                }
            }

            il.Emit(OpCodes.Stloc, aspectVar);

            // === Get MethodInfo for original method ===
            il.Emit(OpCodes.Ldtoken, method.DeclaringType);
            il.Emit(OpCodes.Call, _getTypeFromHandle!);
            il.Emit(OpCodes.Ldstr, originalMethod.Name);
            var bindingFlags = method.IsStatic ? 40 : 36;
            il.Emit(OpCodes.Ldc_I4, bindingFlags);

            var systemTypeType = FindTypeDefinition("System.Type");
            var getMethodMethod = systemTypeType?.Methods
                .First(m => m.Name == "GetMethod" && m.Parameters.Count == 2 &&
                           m.Parameters[0].ParameterType.FullName == "System.String" &&
                           m.Parameters[1].ParameterType.FullName == "System.Reflection.BindingFlags");
            il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(getMethodMethod!));
            il.Emit(OpCodes.Stloc, originalMethodVar);

            // === Create Arguments ===
            il.Emit(OpCodes.Ldc_I4, method.Parameters.Count);
            il.Emit(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object);

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg, method.IsStatic ? i : i + 1);

                if (method.Parameters[i].ParameterType.IsValueType ||
                    method.Parameters[i].ParameterType.IsGenericParameter)
                {
                    il.Emit(OpCodes.Box, method.Parameters[i].ParameterType);
                }

                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Newobj, _argumentsCtor);
            il.Emit(OpCodes.Stloc, argumentsVar);

            // === Create MethodInterceptionArgs ===
            if (method.IsStatic)
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ldtoken, method);
            il.Emit(OpCodes.Ldtoken, method.DeclaringType);
            il.Emit(OpCodes.Call, _getMethodFromHandle);

            il.Emit(OpCodes.Ldloc, argumentsVar);
            il.Emit(OpCodes.Ldloc, originalMethodVar);

            il.Emit(OpCodes.Newobj, _methodInterceptionArgsCtorWithMethodInfo);
            il.Emit(OpCodes.Stloc, argsVar);

            // === Call aspect.OnInvoke(args) ===
            il.Emit(OpCodes.Ldloc, aspectVar);
            il.Emit(OpCodes.Ldloc, argsVar);

            var onInvokeMethod = aspect.AttributeType.Resolve().Methods
                .FirstOrDefault(m => m.Name == "OnInvoke" && m.Parameters.Count == 1);

            if (onInvokeMethod == null)
            {
                onInvokeMethod = _methodInterceptionAspectType!.Resolve().Methods
                    .FirstOrDefault(m => m.Name == "OnInvoke" && m.Parameters.Count == 1);
            }

            if (onInvokeMethod != null)
            {
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(onInvokeMethod));
            }

            // === Return args.ReturnValue ===
            if (method.ReturnType.FullName != "System.Void")
            {
                il.Emit(OpCodes.Ldloc, argsVar);
                var returnValueProp = _methodInterceptionArgsType!.Resolve().Properties
                    .FirstOrDefault(p => p.Name == "ReturnValue");

                if (returnValueProp?.GetMethod != null)
                {
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(returnValueProp.GetMethod));

                    if (method.ReturnType.IsValueType || method.ReturnType.IsGenericParameter)
                    {
                        il.Emit(OpCodes.Unbox_Any, method.ReturnType);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, method.ReturnType);
                    }
                }
            }

            il.Emit(OpCodes.Ret);
        }

        #endregion

        #region OnMethodBoundaryAspect Weaving

        private void WeaveOnMethodBoundary(MethodDefinition method, CustomAttribute aspect)
        {
            if (_methodExecutionArgsType == null || _methodExecutionArgsCtor == null)
            {
                WriteWarning("MethodExecutionArgs type not available, skipping OnMethodBoundaryAspect weaving");
                return;
            }

            // Clone original method body
            var originalMethod = CloneMethodBody(method, "$Boundary");
            method.DeclaringType.Methods.Add(originalMethod);

            // Clear the original method
            method.Body.Instructions.Clear();
            method.Body.Variables.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.InitLocals = true;

            var il = method.Body.GetILProcessor();

            // Local variables
            var aspectVar = new VariableDefinition(ModuleDefinition.ImportReference(aspect.AttributeType));
            var argsVar = new VariableDefinition(_methodExecutionArgsType);
            var returnVar = method.ReturnType.FullName != "System.Void"
                ? new VariableDefinition(method.ReturnType)
                : null;
            var exceptionVar = new VariableDefinition(ModuleDefinition.ImportReference(typeof(Exception)));

            method.Body.Variables.Add(aspectVar);  // 0
            method.Body.Variables.Add(argsVar);    // 1
            if (returnVar != null)
                method.Body.Variables.Add(returnVar);  // 2
            method.Body.Variables.Add(exceptionVar);   // 2 or 3

            // Create aspect instance
            var aspectCtor = aspect.AttributeType.Resolve().Methods
                .FirstOrDefault(m => m.IsConstructor && !m.HasParameters);
            if (aspectCtor == null)
            {
                WriteError($"Aspect {aspect.AttributeType.Name} has no parameterless constructor");
                il.Emit(OpCodes.Ret);
                return;
            }

            il.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(aspectCtor));

            // Set properties
            foreach (var prop in aspect.Properties)
            {
                il.Emit(OpCodes.Dup);
                EmitLdcValue(il, prop.Argument.Value, prop.Argument.Type);
                var setter = aspect.AttributeType.Resolve().Properties
                    .FirstOrDefault(p => p.Name == prop.Name)?.SetMethod;
                if (setter != null)
                {
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(setter));
                }
                else
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Pop);
                }
            }
            il.Emit(OpCodes.Stloc, aspectVar);

            // Create MethodExecutionArgs
            il.Emit(OpCodes.Newobj, _methodExecutionArgsCtor);

            // Set Instance property
            var instanceProp = _methodExecutionArgsType.Resolve().Properties.FirstOrDefault(p => p.Name == "Instance");
            if (instanceProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                if (method.IsStatic)
                    il.Emit(OpCodes.Ldnull);
                else
                    il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(instanceProp.SetMethod));
            }

            // Set Method property
            var methodProp = _methodExecutionArgsType.Resolve().Properties.FirstOrDefault(p => p.Name == "Method");
            if (methodProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldtoken, method);
                il.Emit(OpCodes.Ldtoken, method.DeclaringType);
                il.Emit(OpCodes.Call, _getMethodFromHandle);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(methodProp.SetMethod));
            }

            // Set Arguments property
            var argsProp = _methodExecutionArgsType.Resolve().Properties.FirstOrDefault(p => p.Name == "Arguments");
            if (argsProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                // Create Arguments object
                il.Emit(OpCodes.Ldc_I4, method.Parameters.Count);
                il.Emit(OpCodes.Newarr, ModuleDefinition.TypeSystem.Object);
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldarg, method.IsStatic ? i : i + 1);
                    if (method.Parameters[i].ParameterType.IsValueType || method.Parameters[i].ParameterType.IsGenericParameter)
                        il.Emit(OpCodes.Box, method.Parameters[i].ParameterType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
                il.Emit(OpCodes.Newobj, _argumentsCtor);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(argsProp.SetMethod));
            }

            il.Emit(OpCodes.Stloc, argsVar);

            // Call OnEntry
            var onEntryMethod = GetAspectMethod(aspect.AttributeType, "OnEntry");
            if (onEntryMethod != null)
            {
                il.Emit(OpCodes.Ldloc, aspectVar);
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(onEntryMethod));
            }

            // Try block start
            var tryStart = il.Create(OpCodes.Nop);
            il.Append(tryStart);

            // Call original method
            if (!method.IsStatic)
                il.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < method.Parameters.Count; i++)
                il.Emit(OpCodes.Ldarg, method.IsStatic ? i : i + 1);
            il.Emit(OpCodes.Call, originalMethod);

            // Store return value
            if (returnVar != null)
            {
                il.Emit(OpCodes.Stloc, returnVar);

                // Set ReturnValue on args
                var returnValueProp = _methodExecutionArgsType.Resolve().Properties.FirstOrDefault(p => p.Name == "ReturnValue");
                if (returnValueProp?.SetMethod != null)
                {
                    il.Emit(OpCodes.Ldloc, argsVar);
                    il.Emit(OpCodes.Ldloc, returnVar);
                    if (method.ReturnType.IsValueType || method.ReturnType.IsGenericParameter)
                        il.Emit(OpCodes.Box, method.ReturnType);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(returnValueProp.SetMethod));
                }
            }

            // Call OnSuccess
            var onSuccessMethod = GetAspectMethod(aspect.AttributeType, "OnSuccess");
            if (onSuccessMethod != null)
            {
                il.Emit(OpCodes.Ldloc, aspectVar);
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(onSuccessMethod));
            }

            // Leave try block
            var endFinally = il.Create(OpCodes.Nop);
            var leaveTarget = il.Create(OpCodes.Nop);
            il.Emit(OpCodes.Leave, leaveTarget);

            // Catch block
            var catchStart = il.Create(OpCodes.Stloc, exceptionVar);
            il.Append(catchStart);

            // Set Exception on args
            var exceptionProp = _methodExecutionArgsType.Resolve().Properties.FirstOrDefault(p => p.Name == "Exception");
            if (exceptionProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Ldloc, exceptionVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(exceptionProp.SetMethod));
            }

            // Call OnException
            var onExceptionMethod = GetAspectMethod(aspect.AttributeType, "OnException");
            if (onExceptionMethod != null)
            {
                il.Emit(OpCodes.Ldloc, aspectVar);
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(onExceptionMethod));
            }

            // Rethrow
            il.Emit(OpCodes.Rethrow);

            // End catch, start finally
            var finallyStart = il.Create(OpCodes.Nop);
            il.Append(finallyStart);

            // Call OnExit
            var onExitMethod = GetAspectMethod(aspect.AttributeType, "OnExit");
            if (onExitMethod != null)
            {
                il.Emit(OpCodes.Ldloc, aspectVar);
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(onExitMethod));
            }

            il.Append(endFinally);
            il.Emit(OpCodes.Endfinally);

            // After finally
            il.Append(leaveTarget);

            // Return
            if (returnVar != null)
                il.Emit(OpCodes.Ldloc, returnVar);
            il.Emit(OpCodes.Ret);

            // Set up exception handlers
            var tryEnd = catchStart;
            var catchEnd = finallyStart;
            var finallyEnd = leaveTarget;

            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                HandlerStart = catchStart,
                HandlerEnd = catchEnd,
                CatchType = ModuleDefinition.ImportReference(typeof(Exception))
            });

            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = tryStart,
                TryEnd = finallyStart,
                HandlerStart = finallyStart,
                HandlerEnd = finallyEnd
            });
        }

        private MethodDefinition? GetAspectMethod(TypeReference aspectType, string methodName)
        {
            var typeDef = aspectType.Resolve();
            while (typeDef != null)
            {
                var method = typeDef.Methods.FirstOrDefault(m => m.Name == methodName && m.Parameters.Count == 1);
                if (method != null && !method.IsAbstract)
                    return method;
                typeDef = typeDef.BaseType?.Resolve();
            }
            return null;
        }

        #endregion

        #region LocationInterceptionAspect Weaving

        private void WeaveLocationInterception(PropertyDefinition property, CustomAttribute aspect)
        {
            if (_locationInterceptionArgsType == null || _locationInterceptionArgsCtor == null)
            {
                WriteWarning("LocationInterceptionArgs type not available, skipping LocationInterceptionAspect weaving");
                return;
            }

            // Weave getter if exists
            if (property.GetMethod != null && property.GetMethod.HasBody)
            {
                WeavePropertyGetter(property, aspect);
            }

            // Weave setter if exists
            if (property.SetMethod != null && property.SetMethod.HasBody)
            {
                WeavePropertySetter(property, aspect);
            }
        }

        private void WeavePropertyGetter(PropertyDefinition property, CustomAttribute aspect)
        {
            var getter = property.GetMethod;

            // Clone original getter
            var originalGetter = CloneMethodBody(getter, "$OriginalGet");
            property.DeclaringType.Methods.Add(originalGetter);

            // Clear and rebuild getter
            getter.Body.Instructions.Clear();
            getter.Body.Variables.Clear();
            getter.Body.ExceptionHandlers.Clear();
            getter.Body.InitLocals = true;

            var il = getter.Body.GetILProcessor();

            // Local variables
            var aspectVar = new VariableDefinition(ModuleDefinition.ImportReference(aspect.AttributeType));
            var argsVar = new VariableDefinition(_locationInterceptionArgsType);

            getter.Body.Variables.Add(aspectVar);
            getter.Body.Variables.Add(argsVar);

            // Create aspect instance
            var aspectCtor = aspect.AttributeType.Resolve().Methods
                .FirstOrDefault(m => m.IsConstructor && !m.HasParameters);
            if (aspectCtor == null)
            {
                il.Emit(OpCodes.Ret);
                return;
            }

            il.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(aspectCtor));

            // Set properties
            foreach (var prop in aspect.Properties)
            {
                il.Emit(OpCodes.Dup);
                EmitLdcValue(il, prop.Argument.Value, prop.Argument.Type);
                var setter = aspect.AttributeType.Resolve().Properties
                    .FirstOrDefault(p => p.Name == prop.Name)?.SetMethod;
                if (setter != null)
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(setter));
                else
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Pop);
                }
            }
            il.Emit(OpCodes.Stloc, aspectVar);

            // Create LocationInterceptionArgs
            il.Emit(OpCodes.Newobj, _locationInterceptionArgsCtor);

            // Set Instance
            var locationArgsTypeDef = _locationInterceptionArgsType?.Resolve();
            var instanceProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "Instance");
            if (instanceProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                if (getter.IsStatic)
                    il.Emit(OpCodes.Ldnull);
                else
                    il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(instanceProp.SetMethod));
            }

            // Set LocationName
            var locationNameProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "LocationName");
            if (locationNameProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldstr, property.Name);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(locationNameProp.SetMethod));
            }

            // Set LocationType
            var locationTypeProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "LocationType");
            if (locationTypeProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldtoken, property.PropertyType);
                il.Emit(OpCodes.Call, _getTypeFromHandle);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(locationTypeProp.SetMethod));
            }

            // Set Location (LocationInfo object with PropertyInfo)
            var locationProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "Location");
            if (locationProp?.SetMethod != null && _locationInfoType != null && _locationInfoCtor != null)
            {
                var locationInfoTypeDef = _locationInfoType.Resolve();

                il.Emit(OpCodes.Dup); // dup args

                // Create new LocationInfo()
                il.Emit(OpCodes.Newobj, _locationInfoCtor);

                // Set LocationInfo.Name
                var nameProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "Name");
                if (nameProp?.SetMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldstr, property.Name);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(nameProp.SetMethod));
                }

                // Set LocationInfo.LocationType
                var locTypeProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "LocationType");
                if (locTypeProp?.SetMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldtoken, property.PropertyType);
                    il.Emit(OpCodes.Call, _getTypeFromHandle);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(locTypeProp.SetMethod));
                }

                // Set LocationInfo.DeclaringType
                var declTypeProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "DeclaringType");
                if (declTypeProp?.SetMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldtoken, property.DeclaringType);
                    il.Emit(OpCodes.Call, _getTypeFromHandle);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(declTypeProp.SetMethod));
                }

                // Set LocationInfo.PropertyInfo using Type.GetProperty(name)
                var propInfoProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "PropertyInfo");
                var typeType = FindTypeDefinition("System.Type");
                var getPropertyMethod = typeType?.Methods.FirstOrDefault(m =>
                    m.Name == "GetProperty" && m.Parameters.Count == 1 &&
                    m.Parameters[0].ParameterType.FullName == "System.String");
                if (propInfoProp?.SetMethod != null && getPropertyMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    // declaringType.GetProperty("PropertyName")
                    il.Emit(OpCodes.Ldtoken, property.DeclaringType);
                    il.Emit(OpCodes.Call, _getTypeFromHandle);
                    il.Emit(OpCodes.Ldstr, property.Name);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(getPropertyMethod));
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(propInfoProp.SetMethod));
                }

                // Set args.Location = locationInfo
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(locationProp.SetMethod));
            }

            // Set up getter delegate
            var getterDelegateProp = locationArgsTypeDef?.Fields.FirstOrDefault(f => f.Name == "_getter");
            if (getterDelegateProp != null)
            {
                il.Emit(OpCodes.Dup);
                // Create Func<object> that calls the original getter
                if (!getter.IsStatic)
                    il.Emit(OpCodes.Ldarg_0);
                else
                    il.Emit(OpCodes.Ldnull);

                il.Emit(OpCodes.Ldftn, originalGetter);

                var funcType = ModuleDefinition.ImportReference(typeof(Func<object>));
                var funcCtor = ModuleDefinition.ImportReference(
                    typeof(Func<object>).GetConstructors()[0]);
                il.Emit(OpCodes.Newobj, funcCtor);
                il.Emit(OpCodes.Stfld, ModuleDefinition.ImportReference(getterDelegateProp));
            }

            il.Emit(OpCodes.Stloc, argsVar);

            // Call OnGetValue
            var onGetValueMethod = GetAspectMethod(aspect.AttributeType, "OnGetValue");
            if (onGetValueMethod != null)
            {
                il.Emit(OpCodes.Ldloc, aspectVar);
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(onGetValueMethod));
            }

            // Return args.Value
            var valueProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "Value");
            if (valueProp?.GetMethod != null)
            {
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(valueProp.GetMethod));

                if (property.PropertyType.IsValueType || property.PropertyType.IsGenericParameter)
                    il.Emit(OpCodes.Unbox_Any, property.PropertyType);
                else
                    il.Emit(OpCodes.Castclass, property.PropertyType);
            }

            il.Emit(OpCodes.Ret);
        }

        private void WeavePropertySetter(PropertyDefinition property, CustomAttribute aspect)
        {
            var setter = property.SetMethod;

            // Clone original setter
            var originalSetter = CloneMethodBody(setter, "$OriginalSet");
            property.DeclaringType.Methods.Add(originalSetter);

            // Clear and rebuild setter
            setter.Body.Instructions.Clear();
            setter.Body.Variables.Clear();
            setter.Body.ExceptionHandlers.Clear();
            setter.Body.InitLocals = true;

            var il = setter.Body.GetILProcessor();

            // Local variables
            var aspectVar = new VariableDefinition(ModuleDefinition.ImportReference(aspect.AttributeType));
            var argsVar = new VariableDefinition(_locationInterceptionArgsType);

            setter.Body.Variables.Add(aspectVar);
            setter.Body.Variables.Add(argsVar);

            // Create aspect instance
            var aspectCtor = aspect.AttributeType.Resolve().Methods
                .FirstOrDefault(m => m.IsConstructor && !m.HasParameters);
            if (aspectCtor == null)
            {
                il.Emit(OpCodes.Ret);
                return;
            }

            il.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(aspectCtor));

            foreach (var prop in aspect.Properties)
            {
                il.Emit(OpCodes.Dup);
                EmitLdcValue(il, prop.Argument.Value, prop.Argument.Type);
                var propSetter = aspect.AttributeType.Resolve().Properties
                    .FirstOrDefault(p => p.Name == prop.Name)?.SetMethod;
                if (propSetter != null)
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(propSetter));
                else
                {
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Pop);
                }
            }
            il.Emit(OpCodes.Stloc, aspectVar);

            // Create LocationInterceptionArgs
            il.Emit(OpCodes.Newobj, _locationInterceptionArgsCtor);

            // Set Instance
            var locationArgsTypeDef = _locationInterceptionArgsType?.Resolve();
            var instanceProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "Instance");
            if (instanceProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                if (setter.IsStatic)
                    il.Emit(OpCodes.Ldnull);
                else
                    il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(instanceProp.SetMethod));
            }

            // Set LocationName
            var locationNameProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "LocationName");
            if (locationNameProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldstr, property.Name);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(locationNameProp.SetMethod));
            }

            // Set Value (the value being set)
            var valueProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "Value");
            if (valueProp?.SetMethod != null)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldarg, setter.IsStatic ? 0 : 1); // value parameter
                if (property.PropertyType.IsValueType || property.PropertyType.IsGenericParameter)
                    il.Emit(OpCodes.Box, property.PropertyType);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(valueProp.SetMethod));
            }

            // Set Location (LocationInfo object with PropertyInfo)
            var locationProp = locationArgsTypeDef?.Properties.FirstOrDefault(p => p.Name == "Location");
            if (locationProp?.SetMethod != null && _locationInfoType != null && _locationInfoCtor != null)
            {
                var locationInfoTypeDef = _locationInfoType.Resolve();

                il.Emit(OpCodes.Dup); // dup args

                // Create new LocationInfo()
                il.Emit(OpCodes.Newobj, _locationInfoCtor);

                // Set LocationInfo.Name
                var nameProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "Name");
                if (nameProp?.SetMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldstr, property.Name);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(nameProp.SetMethod));
                }

                // Set LocationInfo.LocationType
                var locTypeProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "LocationType");
                if (locTypeProp?.SetMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldtoken, property.PropertyType);
                    il.Emit(OpCodes.Call, _getTypeFromHandle);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(locTypeProp.SetMethod));
                }

                // Set LocationInfo.DeclaringType
                var declTypeProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "DeclaringType");
                if (declTypeProp?.SetMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldtoken, property.DeclaringType);
                    il.Emit(OpCodes.Call, _getTypeFromHandle);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(declTypeProp.SetMethod));
                }

                // Set LocationInfo.PropertyInfo using Type.GetProperty(name)
                var propInfoProp = locationInfoTypeDef?.Properties.FirstOrDefault(p => p.Name == "PropertyInfo");
                var typeType = FindTypeDefinition("System.Type");
                var getPropertyMethod = typeType?.Methods.FirstOrDefault(m =>
                    m.Name == "GetProperty" && m.Parameters.Count == 1 &&
                    m.Parameters[0].ParameterType.FullName == "System.String");
                if (propInfoProp?.SetMethod != null && getPropertyMethod != null)
                {
                    il.Emit(OpCodes.Dup);
                    // declaringType.GetProperty("PropertyName")
                    il.Emit(OpCodes.Ldtoken, property.DeclaringType);
                    il.Emit(OpCodes.Call, _getTypeFromHandle);
                    il.Emit(OpCodes.Ldstr, property.Name);
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(getPropertyMethod));
                    il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(propInfoProp.SetMethod));
                }

                // Set args.Location = locationInfo
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(locationProp.SetMethod));
            }

            // Set up setter delegate
            var setterDelegateProp = locationArgsTypeDef?.Fields.FirstOrDefault(f => f.Name == "_setter");
            if (setterDelegateProp != null)
            {
                il.Emit(OpCodes.Dup);
                if (!setter.IsStatic)
                    il.Emit(OpCodes.Ldarg_0);
                else
                    il.Emit(OpCodes.Ldnull);

                il.Emit(OpCodes.Ldftn, originalSetter);

                var actionType = ModuleDefinition.ImportReference(typeof(Action<object>));
                var actionCtor = ModuleDefinition.ImportReference(
                    typeof(Action<object>).GetConstructors()[0]);
                il.Emit(OpCodes.Newobj, actionCtor);
                il.Emit(OpCodes.Stfld, ModuleDefinition.ImportReference(setterDelegateProp));
            }

            il.Emit(OpCodes.Stloc, argsVar);

            // Call OnSetValue
            var onSetValueMethod = GetAspectMethod(aspect.AttributeType, "OnSetValue");
            if (onSetValueMethod != null)
            {
                il.Emit(OpCodes.Ldloc, aspectVar);
                il.Emit(OpCodes.Ldloc, argsVar);
                il.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(onSetValueMethod));
            }

            il.Emit(OpCodes.Ret);
        }

        #endregion

        #region Helper Methods

        private MethodDefinition CloneMethodBody(MethodDefinition source, string suffix)
        {
            var attributes = MethodAttributes.Private | MethodAttributes.HideBySig;
            if (source.IsStatic)
            {
                attributes |= MethodAttributes.Static;
            }

            var clone = new MethodDefinition(
                source.Name + suffix,
                attributes,
                source.ReturnType);

            if (!source.IsStatic)
            {
                clone.HasThis = true;
            }

            foreach (var param in source.Parameters)
            {
                clone.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
            }

            foreach (var gp in source.GenericParameters)
            {
                var newGp = new GenericParameter(gp.Name, clone);
                clone.GenericParameters.Add(newGp);
            }

            clone.Body.InitLocals = source.Body.InitLocals;

            foreach (var variable in source.Body.Variables)
            {
                clone.Body.Variables.Add(new VariableDefinition(variable.VariableType));
            }

            var il = clone.Body.GetILProcessor();
            var instructionMap = new Dictionary<Instruction, Instruction>();

            foreach (var instr in source.Body.Instructions)
            {
                var newInstr = CopyInstruction(instr);
                instructionMap[instr] = newInstr;
                il.Append(newInstr);
            }

            // Fix branch targets
            foreach (var instr in clone.Body.Instructions)
            {
                if (instr.Operand is Instruction target)
                {
                    if (instructionMap.TryGetValue(target, out var newTarget))
                        instr.Operand = newTarget;
                }
                else if (instr.Operand is Instruction[] targets)
                {
                    instr.Operand = targets
                        .Select(t => instructionMap.TryGetValue(t, out var nt) ? nt : t)
                        .ToArray();
                }
            }

            // Copy exception handlers
            foreach (var handler in source.Body.ExceptionHandlers)
            {
                clone.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType)
                {
                    CatchType = handler.CatchType,
                    TryStart = handler.TryStart != null && instructionMap.ContainsKey(handler.TryStart)
                        ? instructionMap[handler.TryStart] : null,
                    TryEnd = handler.TryEnd != null && instructionMap.ContainsKey(handler.TryEnd)
                        ? instructionMap[handler.TryEnd] : null,
                    HandlerStart = handler.HandlerStart != null && instructionMap.ContainsKey(handler.HandlerStart)
                        ? instructionMap[handler.HandlerStart] : null,
                    HandlerEnd = handler.HandlerEnd != null && instructionMap.ContainsKey(handler.HandlerEnd)
                        ? instructionMap[handler.HandlerEnd] : null,
                    FilterStart = handler.FilterStart != null && instructionMap.ContainsKey(handler.FilterStart)
                        ? instructionMap[handler.FilterStart] : null
                });
            }

            return clone;
        }

        private Instruction CopyInstruction(Instruction instr)
        {
            if (instr.Operand == null)
                return Instruction.Create(instr.OpCode);

            return instr.Operand switch
            {
                TypeReference tr => Instruction.Create(instr.OpCode, tr),
                MethodReference mr => Instruction.Create(instr.OpCode, mr),
                FieldReference fr => Instruction.Create(instr.OpCode, fr),
                string s => Instruction.Create(instr.OpCode, s),
                int i => Instruction.Create(instr.OpCode, i),
                long l => Instruction.Create(instr.OpCode, l),
                float f => Instruction.Create(instr.OpCode, f),
                double d => Instruction.Create(instr.OpCode, d),
                sbyte sb => Instruction.Create(instr.OpCode, sb),
                byte b => Instruction.Create(instr.OpCode, b),
                VariableDefinition v => Instruction.Create(instr.OpCode, v),
                ParameterDefinition p => Instruction.Create(instr.OpCode, p),
                Instruction target => Instruction.Create(instr.OpCode, target),
                Instruction[] targets => Instruction.Create(instr.OpCode, targets),
                _ => Instruction.Create(instr.OpCode)
            };
        }

        private void EmitLdcValue(ILProcessor il, object? value, TypeReference type)
        {
            switch (value)
            {
                case null:
                    il.Emit(OpCodes.Ldnull);
                    break;
                case string s:
                    il.Emit(OpCodes.Ldstr, s);
                    break;
                case int i:
                    il.Emit(OpCodes.Ldc_I4, i);
                    break;
                case bool b:
                    il.Emit(OpCodes.Ldc_I4, b ? 1 : 0);
                    break;
                case long l:
                    il.Emit(OpCodes.Ldc_I8, l);
                    break;
                case float f:
                    il.Emit(OpCodes.Ldc_R4, f);
                    break;
                case double d:
                    il.Emit(OpCodes.Ldc_R8, d);
                    break;
                case byte bv:
                    il.Emit(OpCodes.Ldc_I4, (int)bv);
                    break;
                case sbyte sbv:
                    il.Emit(OpCodes.Ldc_I4, (int)sbv);
                    break;
                case short sv:
                    il.Emit(OpCodes.Ldc_I4, (int)sv);
                    break;
                case ushort usv:
                    il.Emit(OpCodes.Ldc_I4, (int)usv);
                    break;
                case uint uiv:
                    il.Emit(OpCodes.Ldc_I4, (int)uiv);
                    break;
                case ulong ulv:
                    il.Emit(OpCodes.Ldc_I8, (long)ulv);
                    break;
                default:
                    il.Emit(OpCodes.Ldnull);
                    break;
            }
        }

        #endregion

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "DotNetAspects";
            yield return "mscorlib";
            yield return "System";
            yield return "System.Runtime";
            yield return "System.Reflection";
            yield return "netstandard";
        }
    }
}

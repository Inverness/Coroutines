﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Coroutines.Serialization
{
    /// <summary>
    /// Converts a compiler-generated iterator state machine to and from an IteratorState object.
    /// </summary>
    public class IteratorStateConverter
    {
        private const char StateTypeChar = '1';
        private const char CurrentTypeChar = '2';
        private const char ArgumentTypeChar = '3';
        private const char ThisTypeChar = '4';
        private const char HoistedVariableTypeChar = '5';
        private const char StateMachineTypeChar = 'd';
        private const string StateSuffix = "state";

        private static readonly TypeInfo s_enumeratorType = typeof (IEnumerator).GetTypeInfo();
        private static readonly TypeInfo s_enumerableType = typeof (IEnumerable).GetTypeInfo();

        private readonly Dictionary<Type, IteratorTypeInfo> _fieldInfoCache = new Dictionary<Type, IteratorTypeInfo>();
        private readonly Dictionary<Tuple<string, string>, Type> _iteratorTypes = new Dictionary<Tuple<string, string>, Type>(); 

        /// <summary>
        /// Convert an iterator to an IteratorState.
        /// </summary>
        /// <param name="iterator">The iterator instance.</param>
        /// <returns>An equivalent IteratorState instance that can be serialized.</returns>
        /// <exception cref="ArgumentNullException">iterator is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">iterator is not a compiler-generated iterator state machine.</exception>
        public IteratorState ToState(IEnumerator iterator)
        {
            if (iterator == null)
                throw new ArgumentNullException(nameof(iterator));

            IteratorTypeInfo ti = GetIteratorTypeInfo(iterator.GetType());

            if (ti == null)
                throw new ArgumentOutOfRangeException(nameof(iterator), "not an iterator state machine type");

            var state = new IteratorState
            {
                DeclaringTypeName = NameUtility.GetSimpleAssemblyQualifiedName(ti.Type.DeclaringType),
                MethodName = ti.MethodName,
                State = (int) ti.State.GetValue(iterator),
                Current = ti.Current.GetValue(iterator)
            };

            if (ti.This != null)
                state.This = ti.This.GetValue(iterator);

            foreach (KeyValuePair<string, FieldInfo> a in ti.Arguments)
                state.Arguments.Add(a.Key, a.Value.GetValue(iterator));

            foreach (KeyValuePair<string, FieldInfo> v in ti.Variables)
                state.Variables.Add(v.Key, v.Value.GetValue(iterator));

            return state;
        }

        /// <summary>
        /// Convert an IteratorState to an instance of the iterator it represents.
        /// </summary>
        /// <param name="state">An IteratorState instance.</param>
        /// <returns>An equivalent iterator instance.</returns>
        public IEnumerator FromState(IteratorState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Type iteratorType = GetIteratorTypeChecked(state.DeclaringTypeName, state.MethodName);

            IteratorTypeInfo ti = GetIteratorTypeInfo(iteratorType);
            if (ti == null)
                throw new ArgumentOutOfRangeException(nameof(state), "not an iterator state machine type");

            var iterator = (IEnumerator) ti.Constructor.Invoke(new object[] {state.State});
            ti.Current.SetValue(iterator, state.Current);
            ti.This?.SetValue(iterator, state.This);

            foreach (KeyValuePair<string, object> a in state.Arguments)
            {
                if (ti.Arguments.TryGetValue(a.Key, out FieldInfo info))
                    info.SetValue(iterator, a.Value);
            }

            foreach (KeyValuePair<string, object> v in state.Variables)
            {
                if (ti.Variables.TryGetValue(v.Key, out FieldInfo info))
                    info.SetValue(iterator, v.Value);
            }

            return iterator;
        }

        private IteratorTypeInfo GetIteratorTypeInfo(Type type)
        {
            if (_fieldInfoCache.TryGetValue(type, out IteratorTypeInfo fi))
                return fi;

            fi = CreateIteratorTypeInfo(type);

            if (fi != null)
                _fieldInfoCache.Add(type, fi);

            return fi;
        }

        private Type GetIteratorTypeChecked(string declaringTypeName, string methodName)
        {
            Type declaringType = Type.GetType(declaringTypeName, true);

            Tuple<string, string> key = Tuple.Create(declaringType.AssemblyQualifiedName, methodName);

            if (_iteratorTypes.TryGetValue(key, out Type iteratorType))
                return iteratorType;

            TypeInfo declaringTypeInfo = declaringType.GetTypeInfo();

            MethodInfo method = declaringTypeInfo.GetDeclaredMethod(methodName);
            if (method == null || !(s_enumeratorType.IsAssignableFrom(method.ReturnType.GetTypeInfo()) || s_enumerableType.IsAssignableFrom(method.ReturnType.GetTypeInfo())))
                throw new ArgumentException("invalid iterator method name: " + methodName, nameof(methodName));

            foreach (TypeInfo nt in declaringTypeInfo.DeclaredNestedTypes)
            {
                if (NameUtility.TryParseGeneratedName(nt.Name, out char typeChar, out string _, out string original) && typeChar == StateMachineTypeChar && original == methodName)
                {
                    iteratorType = nt.AsType();
                    break;
                }
            }

            if (iteratorType == null)
                throw new ArgumentException($"no iterator state machine type found for method {methodName} and type {declaringType.AssemblyQualifiedName}");

            _iteratorTypes.Add(key, iteratorType);

            return iteratorType;
        }

        private IteratorTypeInfo CreateIteratorTypeInfo(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();

            var fi = new IteratorTypeInfo
            {
                Type = type,
                MethodName = NameUtility.ParseOriginalName(type.Name),
                Constructor = typeInfo.DeclaredConstructors.FirstOrDefault(c => !c.IsStatic)
            };

            foreach (FieldInfo field in typeInfo.DeclaredFields)
            {
                if (NameUtility.TryParseGeneratedName(field.Name, out char typeChar, out string suffix, out string original))
                {
                    if (typeChar == StateTypeChar)
                    {
                        // Must check suffix too since <>1__managedThreadId is also used
                        if (suffix == StateSuffix)
                        {
                            fi.State = field;
                        }
                    }
                    else if (typeChar == CurrentTypeChar)
                    {
                        fi.Current = field;
                    }
                    else if (typeChar == ThisTypeChar)
                    {
                        fi.This = field;
                    }
                    else if (typeChar == ArgumentTypeChar)
                    {
                        fi.Arguments.Add(original ?? suffix, field);
                    }
                    else if (typeChar == HoistedVariableTypeChar)
                    {
                        Debug.Assert(original != null);
                        fi.Variables.Add(original, field);
                    }
                }
                else
                {
                    Debug.Assert(!field.IsPublic);
                    fi.Variables.Add(field.Name, field);
                }
            }

            if (fi.Current == null || fi.State == null || fi.Constructor == null)
                return null;

            return fi;
        }

        private class IteratorTypeInfo
        {
            public string MethodName;

            public Type Type;

            public ConstructorInfo Constructor;

            public FieldInfo State;

            public FieldInfo Current;

            public FieldInfo This;

            public readonly Dictionary<string, FieldInfo> Arguments = new Dictionary<string, FieldInfo>();

            public readonly Dictionary<string, FieldInfo> Variables = new Dictionary<string, FieldInfo>();
        }
    }
}

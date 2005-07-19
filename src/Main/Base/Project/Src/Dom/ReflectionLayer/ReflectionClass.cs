﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	[Serializable]
	public class ReflectionClass : DefaultClass
	{
		Type type;
		
		BindingFlags flags = BindingFlags.Instance  |
			BindingFlags.Static    |
			BindingFlags.NonPublic |
			BindingFlags.DeclaredOnly |
			BindingFlags.Public;
		
		public override List<IClass> InnerClasses {
			get {
				List<IClass> innerClasses = new List<IClass>();
				foreach (Type nestedType in type.GetNestedTypes(flags)) {
					innerClasses.Add(new ReflectionClass(CompilationUnit, nestedType, this));
				}
				return innerClasses;
			}
		}
		
		public override List<IField> Fields {
			get {
				List<IField> fields = new List<IField>();
				foreach (FieldInfo field in type.GetFields(flags)) {
					if (!field.IsPublic && !field.IsFamily) continue;
					if (!field.IsSpecialName) {
						fields.Add(new ReflectionField(field, this));
					}
				}
				return fields;
			}
		}
		
		public override List<IProperty> Properties {
			get {
				List<IProperty> properties = new List<IProperty>();
				foreach (PropertyInfo propertyInfo in type.GetProperties(flags)) {
					ParameterInfo[] p = null;
					
					// we may not get the permission to access the index parameters
					try {
						p = propertyInfo.GetIndexParameters();
					} catch (Exception) {}
					if (p == null || p.Length == 0) {
						ReflectionProperty prop = new ReflectionProperty(propertyInfo, this);
						if (prop.IsPublic || prop.IsProtected)
							properties.Add(prop);
					}
				}
				
				return properties;
			}
		}
		
		public override List<IIndexer> Indexer {
			get {
				List<IIndexer> indexer = new List<IIndexer>();
				foreach (PropertyInfo propertyInfo in type.GetProperties(flags)) {
					ParameterInfo[] p = null;
					
					// we may not get the permission to access the index parameters
					try {
						p = propertyInfo.GetIndexParameters();
					} catch (Exception) {}
					if (p != null && p.Length != 0) {
						indexer.Add(new ReflectionIndexer(propertyInfo, this));
					}
				}
				
				return indexer;
			}
		}
		
		public override List<IMethod> Methods {
			get {
				List<IMethod> methods = new List<IMethod>();
				
				foreach (ConstructorInfo constructorInfo in type.GetConstructors(flags)) {
					if (!constructorInfo.IsPublic && !constructorInfo.IsFamily) continue;
					IMethod newMethod = new ReflectionMethod(constructorInfo, this);
					methods.Add(newMethod);
				}
				
				foreach (MethodInfo methodInfo in type.GetMethods(flags)) {
					if (!methodInfo.IsPublic && !methodInfo.IsFamily) continue;
					if (!methodInfo.IsSpecialName) {
						IMethod newMethod = new ReflectionMethod(methodInfo, this);
						methods.Add(newMethod);
					}
				}
				return methods;
			}
		}
		
		public override List<IEvent> Events {
			get {
				List<IEvent> events = new List<IEvent>();
				
				foreach (EventInfo eventInfo in type.GetEvents(flags)) {
					events.Add(new ReflectionEvent(eventInfo, this));
				}
				return events;
			}
		}
		
		public static bool IsDelegate(Type type)
		{
			return type.IsSubclassOf(typeof(Delegate)) && type != typeof(MulticastDelegate);
		}
		
		#region VoidClass / VoidReturnType
		public class VoidClass : ReflectionClass
		{
			public VoidClass(ICompilationUnit compilationUnit) : base(compilationUnit, typeof(void), null) {}
			
			protected override IReturnType CreateDefaultReturnType() {
				return new VoidReturnType(this);
			}
		}
		
		private class VoidReturnType : DefaultReturnType
		{
			public VoidReturnType(IClass c) : base(c) {}
			public override List<IMethod> GetMethods() {
				return new List<IMethod>(1);
			}
			public override List<IProperty> GetProperties() {
				return new List<IProperty>(1);
			}
			public override List<IField> GetFields() {
				return new List<IField>(1);
			}
			public override List<IEvent> GetEvents() {
				return new List<IEvent>(1);
			}
			public override List<IIndexer> GetIndexers() {
				return new List<IIndexer>(1);
			}
		}
		#endregion
		
		public ReflectionClass(ICompilationUnit compilationUnit, Type type, IClass declaringType) : base(compilationUnit, declaringType)
		{
			this.type = type;
			string name = type.FullName.Replace('+', '.');
			if (name.Length > 2 && name[name.Length - 2] == '`') {
				FullyQualifiedName = name.Substring(0, name.Length - 2);
			} else {
				FullyQualifiedName = name;
			}
			
			// set classtype
			if (type.IsInterface) {
				this.ClassType = ClassType.Interface;
			} else if (type.IsEnum) {
				this.ClassType = ClassType.Enum;
			} else if (type.IsValueType) {
				this.ClassType = ClassType.Struct;
			} else if (IsDelegate(type)) {
				this.ClassType = ClassType.Delegate;
			} else {
				// TODO: Check if class is a module.
				this.ClassType = ClassType.Class;
			}
			if (type.IsGenericTypeDefinition) {
				foreach (Type g in type.GetGenericArguments()) {
					this.TypeParameters.Add(new DefaultTypeParameter(this, g));
				}
			}
			
			ModifierEnum modifiers  = ModifierEnum.None;
			
			if (type.IsNestedAssembly) {
				modifiers |= ModifierEnum.Internal;
			}
			if (type.IsSealed) {
				modifiers |= ModifierEnum.Sealed;
			}
			if (type.IsAbstract) {
				modifiers |= ModifierEnum.Abstract;
			}
			
			if (type.IsNestedPrivate ) { // I assume that private is used most and public last (at least should be)
				modifiers |= ModifierEnum.Private;
			} else if (type.IsNestedFamily ) {
				modifiers |= ModifierEnum.Protected;
			} else if (type.IsNestedPublic || type.IsPublic) {
				modifiers |= ModifierEnum.Public;
			} else if (type.IsNotPublic) {
				modifiers |= ModifierEnum.Internal;
			} else if (type.IsNestedFamORAssem) {
				modifiers |= ModifierEnum.ProtectedOrInternal;
			} else if (type.IsNestedFamANDAssem) {
				modifiers |= ModifierEnum.Protected;
				modifiers |= ModifierEnum.Internal;
			}
			this.Modifiers = modifiers;
			
			// set base classes
			if (type.BaseType != null) { // it's null for System.Object ONLY !!!
				BaseTypes.Add(type.BaseType.FullName);
			}
			
			foreach (Type iface in type.GetInterfaces()) {
				BaseTypes.Add(iface.FullName);
			}
		}
	}
	
}

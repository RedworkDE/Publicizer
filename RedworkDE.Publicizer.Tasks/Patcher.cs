using System;
using System.Linq;
using dnlib.DotNet;

namespace RedworkDE.Publicizer
{
    public static class Patcher
    {
        public static void Patch(ModuleDef module, bool publicizeInternal, bool publicizePrivate, bool publicizeReadonly, bool publicizeEventBackingField, bool unityKeepSerializable)
        {
            try
            {

	            foreach (var type in module.GetTypes())
	            {
					// update type visibility
		            if (type.IsNested)
		            {
			            if (publicizePrivate)
			            {
				            type.Visibility = TypeAttributes.NestedPublic;
						}
			            else if (publicizeInternal)
			            {
				            if (type.IsNestedAssembly ||
				                type.IsNestedFamilyOrAssembly)
				            {
					            type.Visibility = TypeAttributes.NestedPublic;
				            }
						}
		            }
		            else if (publicizePrivate || publicizeInternal)
		            {
			            type.Visibility = TypeAttributes.Public;
		            }

					// update method visibility
					// this also affects properties and events
		            foreach (var method in type.Methods)
		            {
			            if (publicizePrivate)
			            {
				            method.Access = MethodAttributes.Public;
			            }
						else if (publicizeInternal)
			            {
				            if (method.IsAssembly || method.IsFamilyOrAssembly)
				            {
					            method.Access = MethodAttributes.Public;
							}
			            }
		            }

                    foreach (var field in type.Fields)
                    {
                        if (publicizePrivate)
                        {
	                        // if an event with same name exists
	                        // remove it, users can directly access the underlying action
	                        if (publicizeEventBackingField)
	                        {
		                        var sameEvent = type.Events.FirstOrDefault(e => e.Name == field.Name);
		                        type.Events.Remove(sameEvent);
	                        }
	                        else
	                        {
								// skip making the backing field public or it will be impossible to refer to the event
								if (type.Events.Any(e => e.Name == field.Name)) continue;
							}
                        }


                        if (unityKeepSerializable)
                        {
	                        // if the field was not serialized before making it public mark it as such, or unity will crash deserializing this type
	                        if (!field.IsPublic && !field.IsStatic && !field.IsInitOnly && !field.HasConstant && !field.CustomAttributes.Any(c =>
		                        c.AttributeType.Namespace == "UnityEngine" && c.AttributeType.Name == "SerializeField" ||
		                        c.AttributeType.Namespace == "System.Runtime.CompilerServices" && c.AttributeType.Name == "CompilerGeneratedAttribute")) field.IsNotSerialized = true;
                        }

						if (publicizePrivate)
						{
							field.Access = FieldAttributes.Public;
						}
						else if (publicizeInternal)
						{
							if (field.IsAssembly || field.IsFamilyOrAssembly)
							{
								field.Access = FieldAttributes.Public;
							}
						}

						if (publicizeReadonly)
                        {
                            // ignore constants
	                        if (!field.IsLiteral)
	                        {
		                        field.IsInitOnly = false;
	                        }
                        }
                    }
		            
	            }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

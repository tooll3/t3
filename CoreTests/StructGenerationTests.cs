using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OperatorTests
{
    [TestClass]
    public class StructGenerationTests
    {
        [TestMethod]
        public void CreateAndInstantiateStruct_typeAndInstanceCanBeCreatedAndHasCorrectSize()
        {
            // Create assembly and module
            var assemblyName = new AssemblyName("MyStructAssembly");
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
            var attr = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.ExplicitLayout | TypeAttributes.Serializable | TypeAttributes.AnsiClass;
            var typeBuilder = moduleBuilder.DefineType("MyStruct", attr, typeof(ValueType));

            const int explicitSize = 32;
            var ctorParameters = new[] { typeof(LayoutKind) };
            var ctorInfo = typeof(StructLayoutAttribute).GetConstructor(ctorParameters);
            var fields = typeof(StructLayoutAttribute).GetFields(BindingFlags.Public | BindingFlags.Instance);
            var structLayoutSizeField = (from f in fields where f.Name == "Size" select f).ToArray();
            var structLayoutAttr = new CustomAttributeBuilder(ctorInfo, new object[] { LayoutKind.Explicit },
                                                              structLayoutSizeField, new object[] { explicitSize }); 
            typeBuilder.SetCustomAttribute(structLayoutAttr);
            
            var nameFieldBuilder = typeBuilder.DefineField( "Name", typeof(string), FieldAttributes.Public);
            nameFieldBuilder.SetOffset(0);

            var sizeFieldBuilder = typeBuilder.DefineField( "size", typeof(float), FieldAttributes.Public);
            sizeFieldBuilder.SetOffset(8);

            sizeFieldBuilder = typeBuilder.DefineField( "size2", typeof(float), FieldAttributes.Public);
            sizeFieldBuilder.SetOffset(12);
            
            sizeFieldBuilder = typeBuilder.DefineField( "size3", typeof(float), FieldAttributes.Public);
            sizeFieldBuilder.SetOffset(16);
            
            var type = typeBuilder.CreateType();
            Assert.AreEqual(true, type.IsValueType);

            var instance = Activator.CreateInstance(type);
            int size = Marshal.SizeOf(instance);
            Assert.AreEqual(explicitSize, size);
        }
    }
}
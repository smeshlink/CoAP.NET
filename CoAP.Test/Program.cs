using System;
using System.Collections.Generic;
using System.Reflection;

namespace CoAP.Test
{
    class Program
    {
        static Type[] testTypes = new Type[] { 
            //typeof(DatagramReadWriteTest),
            typeof(MessageTest),
            typeof(OptionTest),
            typeof(ResourceTest)
        };

        static void Main()
        {
            RunTests();
            Console.WriteLine("(end of tests; press any key)");
            Console.ReadKey();
        }

        private static void RunTests()
        {
            int total = 0;
            int fail = 0;
            Dictionary<Object, MethodInfo[]> tests = new Dictionary<Object, MethodInfo[]>();
            foreach (Type type in testTypes)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                MethodInfo[] activeTests = Array.FindAll(methods, delegate(MethodInfo m) { return Attribute.IsDefined(m, typeof(ActiveTestAttribute)); });
                if (activeTests.Length != 0)
                    methods = activeTests;
                tests.Add(Activator.CreateInstance(type), methods);
                total += methods.Length;
            }

            foreach (KeyValuePair<Object, MethodInfo[]> pair in tests)
            {
                foreach (MethodInfo method in pair.Value)
                {
                    Console.Write("Running " + method.Name);
                    try
                    {
                        method.Invoke(pair.Key, null);
                        Console.WriteLine(" - OK!");
                    }
                    catch (Exception ex)
                    {
                        fail++;
                        if (ex.InnerException == null)
                            Console.WriteLine(" - " + ex.Message);
                        else if (ex.InnerException.InnerException == null)
                            Console.WriteLine(" - " + ex.InnerException.Message);
                        else
                            Console.WriteLine(" - " + ex.InnerException.InnerException.Message);
                    }
                }
            }
            Console.WriteLine();
            if (fail == 0)
            {
                Console.WriteLine("(all tests successful)");
            }
            else
            {
                Console.WriteLine("#### FAILED: {0} / {1}", fail, total);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    sealed class ActiveTestAttribute : Attribute { }
}

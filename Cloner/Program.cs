using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Cloner
{
    internal class Program
    {
        private static Dictionary<Type, ClonerElement> cloners = new Dictionary<Type, ClonerElement>();

        private static ClonerElement GenerateCloner(Type type)
        {
            if (type == typeof(decimal) || type == typeof(string) || type == typeof(long) || type == typeof(int))
            {
                return new PrimitiveClonerElement();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (List<>))
            {
                var elementCloner = GenerateCloner(type.GenericTypeArguments[0]);
                return new ListClonerElement(elementCloner, type);
            }
            else
            {
                // Assume nested object
                ClonerElement resultElement;
                if (!cloners.TryGetValue(type, out resultElement))
                {
                    var objectCloner = new ObjectClonerElement(type);
                    cloners.Add(type, objectCloner);
                    foreach (var property in type.GetProperties())
                    {
                        objectCloner.AddPropertyCloner(property, GenerateCloner(property.PropertyType));
                    }
                    objectCloner.FinishExpressions();
                    resultElement = objectCloner;
                }
                return resultElement;
            }
        }

        private static int counter = 0;

        private static TestClass GenerateTestData(int level)
        {
            TestClass result = null;
            if (level <= 5)
            {
                result = new TestClass
                {
                    Value1 = "" + level + ": " + counter++,
                    Value2 = counter,
                    Value3 = counter,
                    Value4 = counter,
                    NestedClasses = new List<TestClass>(),
                    NestedValue = GenerateTestData(level + 1)
                };
                for (var i = 0; i < 5; i++)
                {
                    result.NestedClasses.Add(GenerateTestData(level + 1));
                }
            }
            return result;
        }

        private static void Main(string[] args)
        {
            var input = new TestClass[100];
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = GenerateTestData(0);
            }


            var output = new TestClass[input.Length];
            var outputNative = new TestClass[input.Length];
            var cloner = GenerateCloner(input[0].GetType());

            var totalInstances1 = TestClass.totalInstances;
            
            // Test run
            for (var i = 0; i < input.Length; i++)
            {
                outputNative[i] = input[i].Clone();
            }

            var stopWatchNative = Stopwatch.StartNew();
            for (var i = 0; i < input.Length; i++)
            {
                outputNative[i] = input[i].Clone();
            }
            stopWatchNative.Stop();

            var totalInstances2 = TestClass.totalInstances;

            // Test run
            for (var i = 0; i < input.Length; i++)
            {
                output[i] = (TestClass)cloner.Clone(input[i]);
            }
            var stopWatch = Stopwatch.StartNew();
            for (var i = 0; i < input.Length; i++)
            {
                output[i] = (TestClass)cloner.Clone(input[i]);
            }
            stopWatch.Stop();

            var totalInstances3 = TestClass.totalInstances;

            for (var i = 0; i < input.Length; i++)
            {
                var inputString = input[i].ToString();
                var outputString = output[i].ToString();
                Console.WriteLine("Strings match: " + (inputString == outputString));
                Console.WriteLine("Total length: " + outputString.Length);
                Console.WriteLine("Total properties: " + input[i].GetTotalAttributes());
            }
            Console.WriteLine("Total time: " + stopWatch.Elapsed);
            Console.WriteLine("Total time native: " + stopWatchNative.Elapsed);
            Console.WriteLine("Time per instance: " + stopWatch.ElapsedMilliseconds / input.Length);
            Console.WriteLine("Time per instance native: " + stopWatchNative.ElapsedMilliseconds / input.Length);
            Console.WriteLine("Total instances: " + totalInstances1 + ", " + (totalInstances2 - totalInstances1) + ", " + (totalInstances3 - totalInstances2));
        }
    }



    public class TestClass
    {
        private static int propertiesCount = typeof (TestClass).GetProperties().Length;
        public string Value1 { get; set; }
        public decimal Value2 { get; set; }
        public long Value3 { get; set; }
        public int Value4 { get; set; }

        public TestClass NestedValue { get; set; }
        public List<TestClass> NestedClasses { get; set; }

        public static int totalInstances = 0;
        public TestClass()
        {
            totalInstances++;
        }

        public override string ToString()
        {
            var nestedClassesString = new StringBuilder();
            if (NestedClasses != null)
            {
                foreach (var item in NestedClasses)
                {
                    if (item != null)
                    {
                        nestedClassesString.Append(item);
                    }
                }
            }
            return $"Value1: {Value1}, Value2: {Value2}, Value3: {Value3}, Value4: {Value4}, NestedValue: {NestedValue}, NestedClasses: {nestedClassesString}";
        }

        public int GetTotalAttributes()
        {
            var result = propertiesCount;
            if (NestedValue != null)
            {
                result += NestedValue.GetTotalAttributes();
            }
            if (NestedClasses != null)
            {
                foreach (var item in NestedClasses)
                {
                    if (item != null)
                    {
                        result += item.GetTotalAttributes();
                    }
                }
            }
            return result;
        }

        public TestClass Clone()
        {
            var result = new TestClass
            {
                Value1 = Value1,
                Value2 = Value2,
                Value3 = Value3,
                Value4 = Value4,
                NestedValue = NestedValue?.Clone(),
                NestedClasses = NestedClasses == null ? null : new List<TestClass>(NestedClasses.Count)
            };
            if (NestedClasses != null)
            {
                foreach (var item in NestedClasses)
                {
                    result.NestedClasses.Add(item?.Clone());
                }
            }
            return result;
        }
    }
}
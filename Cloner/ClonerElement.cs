using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Cloner
{
    public abstract class ClonerElement
    {
        public abstract object Clone(object input);
    }

    public class PrimitiveClonerElement : ClonerElement
    {
        public override object Clone(object input)
        {
            return input;
        }
    }

    public class ListClonerElement : ClonerElement
    {
        private readonly ConstructorInfo constructor;
        private readonly ClonerElement elementCloner;

        public ListClonerElement(ClonerElement elementCloner, Type type) 
        {
            this.elementCloner = elementCloner;
            constructor = type.GetConstructor(new[] {typeof (int)});
        }

        public override object Clone(object input)
        {
            if (input == null)
            {
                return null;
            }
            var inputList = (IList) input;
            var resultList = (IList) constructor.Invoke(new object[] {inputList.Count});
            foreach (var item in inputList)
            {
                resultList.Add(elementCloner.Clone(item));
            }
            return resultList;
        }
    }

    public class ObjectClonerElement : ClonerElement
    {
        private readonly ConstructorInfo constructor;
        private readonly List<Tuple<PropertyInfo, ClonerElement>> propertyCloners;
        private readonly Type type;
        private readonly List<Expression> expressions = new List<Expression>();
        private readonly ParameterExpression input;
        private readonly ParameterExpression inputCorrectType;
        private readonly ParameterExpression outputCorrectType;
        private Func<object, object> cloneAction;

        public ObjectClonerElement(Type type) 
        {
            constructor = type.GetConstructor(new Type[0]);
            propertyCloners = new List<Tuple<PropertyInfo, ClonerElement>>();
            this.type = type;
            input = Expression.Parameter(typeof(object), "input");
            inputCorrectType = Expression.Parameter(type, "inputCorrectType");
            outputCorrectType = Expression.Parameter(type, "outputCorrectType");
            expressions.Add(Expression.Assign(inputCorrectType, Expression.Convert(input, type)));
            expressions.Add(Expression.Assign(outputCorrectType, Expression.New(constructor)));
        }

        public void AddPropertyCloner(PropertyInfo property, ClonerElement cloner)
        {
            propertyCloners.Add(new Tuple<PropertyInfo, ClonerElement>(property, cloner));
            
            if (property.PropertyType == typeof(string) || property.PropertyType == typeof(int) || property.PropertyType == typeof(long) || property.PropertyType == typeof(decimal))
            {
                // Assign primitive values directly
                expressions.Add(
                    Expression.Assign(
                        Expression.Property(outputCorrectType, property),
                        Expression.Property(inputCorrectType, property)
                    )
                );
            }
            else
            {
                // Everything else must be properly cloned
                expressions.Add(
                    Expression.Assign(
                        Expression.Property(outputCorrectType, property),
                        Expression.Convert(
                            Expression.Call(
                                Expression.Constant(cloner),
                                cloner.GetType().GetMethod("Clone"),
                                Expression.Convert(
                                    Expression.Property(inputCorrectType, property),
                                    typeof (object)
                                )
                            ),
                            property.PropertyType
                        )
                    )
                );
            }
        }

        public void FinishExpressions()
        {
            expressions.Add(Expression.Label(Expression.Label(type), outputCorrectType));
            var block = Expression.Block(new [] {inputCorrectType, outputCorrectType}, expressions);
            cloneAction = Expression.Lambda<Func<object, object>>(block, input).Compile();
        }

        public override object Clone(object input)
        {
            if (input == null)
            {
                return null;
            }
            // Clone using pure reflection
            /*var result = constructor.Invoke(new object[0]);
            foreach (var property in propertyCloners)
            {
                var clonedValue = property.Item2.Clone(property.Item1.GetValue(input));
                property.Item1.SetValue(result, clonedValue);
            }
            return result;*/


            // Clone based on compiled expressions
            return cloneAction(input);
        }
    }

}
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Prototype1.Foundation
{
    public class TemporaryOverride<TObject, TProperty> : IDisposable
    {
        private readonly object _originalValue = default(TProperty);
        private readonly object _propertyOwner;
        private readonly PropertyInfo _property;

        public TemporaryOverride(TObject obj, Expression<Func<TObject, TProperty>> expression, TProperty temporaryValue)
        {
            var param = expression.Parameters[0];
            var body = ((LambdaExpression)expression).Body as MemberExpression;
            _property = body.Member as PropertyInfo;

            if (body.Expression is ParameterExpression)
            {
                _propertyOwner = obj;
            }
            else if (body.Expression is MemberExpression || body.Expression.NodeType == ExpressionType.Convert)
            {
                //MemberExpression ownerBody = body.Expression as MemberExpression;
                _propertyOwner = Expression.Lambda(body.Expression, param).Compile().DynamicInvoke(obj);
            }
            else
            {
                throw new ArgumentException("Expression should be a ParameterExpression, MemberExpresion, or have a NodeType of Convert");
            }

            _originalValue = expression.Compile().DynamicInvoke(obj);
            _property.SetValue(_propertyOwner, temporaryValue, null);
        }

        public void Dispose()
        {
            _property.SetValue(_propertyOwner, _originalValue, null);
        }
    }

    public static class TemporaryOverrideExtension
    {
        /// <summary>
        /// Use this extension method inside a using block to temporarily override the value of any property on any object.
        /// Assert.AreEqual("Ryan", ryan.Name);
        ///    using (ryan.Override(p => p.Name, "Joe"))
        ///    {
        ///        Assert.AreEqual("Joe", ryan.Name);
        ///    }
        ///    Assert.AreEqual("Ryan", ryan.Name);
        /// </summary>
        /// <typeparam name="TObject">Type of the object we're modifying (inferred from the type we're using)</typeparam>
        /// <typeparam name="TProperty">Type of the property of the object we're modifying (inferred from the lambda expression)</typeparam>
        /// <param name="obj">The actual object we're modifying</param>
        /// <param name="expression">Lambda expression with the property accessor</param>
        /// <param name="temporaryValue">The value you wish to override the property with</param>
        /// <returns>TemporaryOverride that when disposed will automatically revert the property back to its original state</returns>
        public static TemporaryOverride<TObject, TProperty> Override<TObject, TProperty>(this TObject obj, Expression<Func<TObject, TProperty>> expression, TProperty temporaryValue)
        {
            return new TemporaryOverride<TObject, TProperty>(obj, expression, temporaryValue);
        }
    }
}

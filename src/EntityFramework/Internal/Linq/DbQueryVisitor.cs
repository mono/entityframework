// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.Linq
{
    using System.Collections.Concurrent;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    ///     A LINQ expression visitor that finds <see cref="DbQuery" /> uses with equivalent
    ///     <see cref="ObjectQuery" /> instances.
    /// </summary>
    internal class DbQueryVisitor : ExpressionVisitor
    {
        #region Fields and constructors

        private const BindingFlags SetAccessBindingFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly ConcurrentDictionary<Type, Func<ObjectQuery, object>> _wrapperFactories =
            new ConcurrentDictionary<Type, Func<ObjectQuery, object>>();

        #endregion

        #region Overriden visitors

        /// <summary>
        ///     Replaces calls to DbContext.Set() with an expression for the equivalent <see cref="ObjectQuery" />.
        /// </summary>
        /// <param name="node"> The node to replace. </param>
        /// <returns> A new node, which may have had the replacement made. </returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Check.NotNull(node, "node");

            // We are looking for either the generic or non-generic Set method on DbContext.
            // However, we don't constrain to this so if you write your own parameterless method on
            // a derived DbContext, then we will work with this as well.
            if (typeof(DbContext).IsAssignableFrom(node.Method.DeclaringType))
            {
                var memberExpression = node.Object as MemberExpression;
                if (memberExpression != null)
                {
                    // Only try to invoke the method if it is on the context, is not parameterless, and is not attributed
                    // as a function.
                    var context = GetContextFromConstantExpression(memberExpression.Expression, memberExpression.Member);
                    if (context != null
                        && !node.Method.GetCustomAttributes(typeof(DbFunctionAttribute), false).Any()
                        && node.Method.GetParameters().Length == 0)
                    {
                        var expression =
                            CreateObjectQueryConstant(
                                node.Method.Invoke(context, SetAccessBindingFlags, null, null, null));
                        if (expression != null)
                        {
                            return expression;
                        }
                    }
                }
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>
        ///     Replaces a <see cref="DbQuery" /> or <see cref="DbQuery{T}" /> property with a constant expression
        ///     for the underlying <see cref="ObjectQuery" />.
        /// </summary>
        /// <param name="node"> The node to replace. </param>
        /// <returns> A new node, which may have had the replacement made. </returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            Check.NotNull(node, "node");

            var propInfo = node.Member as PropertyInfo;
            var memberExpression = node.Expression as MemberExpression;

            if (propInfo != null
                && memberExpression != null
                && typeof(IQueryable).IsAssignableFrom(propInfo.PropertyType)
                && typeof(DbContext).IsAssignableFrom(node.Member.DeclaringType))
            {
                var context = GetContextFromConstantExpression(memberExpression.Expression, memberExpression.Member);
                if (context != null)
                {
                    var expression =
                        CreateObjectQueryConstant(propInfo.GetValue(context, SetAccessBindingFlags, null, null, null));
                    if (expression != null)
                    {
                        return expression;
                    }
                }
            }

            return base.VisitMember(node);
        }

        /// <summary>
        ///     Processes the fields in each constant expression and replaces <see cref="DbQuery" /> instances with
        ///     the underlying ObjectQuery instance.  This handles cases where the query has a closure
        ///     containing <see cref="DbQuery" /> values.
        /// </summary>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            Check.NotNull(node, "node");

            var value = node.Value;
            if (value != null)
            {
                var fields = value.GetType().GetFields();
                foreach (var field in fields.Where(f => typeof(IQueryable).IsAssignableFrom(f.FieldType)))
                {
                    var objectQuery = ExtractObjectQuery(field.GetValue(value));
                    if (objectQuery != null)
                    {
                        field.SetValue(value, objectQuery);
                    }
                }
            }

            return base.VisitConstant(node);
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Gets a <see cref="DbContext" /> value from the given member, or returns null
        ///     if the member doesn't contain a DbContext instance.
        /// </summary>
        /// <param name="expression"> The expression for the object for the member, which may be null for a static member. </param>
        /// <param name="member"> The member. </param>
        /// <returns> The context or null. </returns>
        private static DbContext GetContextFromConstantExpression(Expression expression, MemberInfo member)
        {
            DebugCheck.NotNull(member);

            if (expression == null)
            {
                // Static field/property access
                return GetContextFromMember(member, null);
            }

            var constant = expression as ConstantExpression;
            if (constant != null)
            {
                var value = constant.Value;
                if (value != null)
                {
                    return GetContextFromMember(member, value);
                }
            }
            return null;
        }

        /// <summary>
        ///     Gets the <see cref="DbContext" /> instance from the given instance or static member, returning null
        ///     if the member does not contain a DbContext instance.
        /// </summary>
        /// <param name="member"> The member. </param>
        /// <param name="value"> The value of the object to get the instance from, or null if the member is static. </param>
        /// <returns> The context instance or null. </returns>
        private static DbContext GetContextFromMember(MemberInfo member, object value)
        {
            DebugCheck.NotNull(member);

            var asField = member as FieldInfo;
            if (asField != null)
            {
                return asField.GetValue(value) as DbContext;
            }
            var asProperty = member as PropertyInfo;
            if (asProperty != null)
            {
                return asProperty.GetValue(value, null) as DbContext;
            }
            return null;
        }

        /// <summary>
        ///     Takes a <see cref="DbQuery{T}" /> or <see cref="DbQuery" /> and creates an expression
        ///     for the underlying <see cref="ObjectQuery{T}" />.
        /// </summary>
        private static Expression CreateObjectQueryConstant(object dbQuery)
        {
            var objectQuery = ExtractObjectQuery(dbQuery);

            if (objectQuery != null)
            {
                var elementType = objectQuery.GetType().GetGenericArguments().Single();

                Func<ObjectQuery, object> factory;
                if (!_wrapperFactories.TryGetValue(elementType, out factory))
                {
                    var genericType = typeof(ReplacementDbQueryWrapper<>).MakeGenericType(elementType);
                    var factoryMethod = genericType.GetMethod(
                        "Create", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(ObjectQuery) },
                        null);
                    factory =
                        (Func<ObjectQuery, object>)
                        Delegate.CreateDelegate(typeof(Func<ObjectQuery, object>), factoryMethod);
                    _wrapperFactories.TryAdd(elementType, factory);
                }

                var replacement = factory(objectQuery);

                var newConstant = Expression.Constant(replacement, replacement.GetType());
                return Expression.Property(newConstant, "Query");
            }

            return null;
        }

        /// <summary>
        ///     Takes a <see cref="DbQuery{T}" /> or <see cref="DbQuery" /> and extracts the underlying <see cref="ObjectQuery{T}" />.
        /// </summary>
        private static ObjectQuery ExtractObjectQuery(object dbQuery)
        {
            var adapted = dbQuery as IInternalQueryAdapter;
            return adapted == null ? null : adapted.InternalQuery.ObjectQuery;
        }

        #endregion
    }
}

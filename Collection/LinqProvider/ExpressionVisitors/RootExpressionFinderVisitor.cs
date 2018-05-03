using CosmosDbContext.Collection;
using System.Linq.Expressions;

namespace CosmosDbContext.Collection.LinqProvider.ExpressionVisitors
{
    internal class RootExpressionFinderVisitor : ExpressionVisitor
    {
        private Expression rootExpression;

        public Expression GetRoot(Expression expression)
        {
            Visit(expression);
            return rootExpression;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsGenericType && (node.Type.GetGenericTypeDefinition() == typeof(CosmosDbCollection<>)))
            {
                this.rootExpression = node;
            }
            return node;
        }
    }
}

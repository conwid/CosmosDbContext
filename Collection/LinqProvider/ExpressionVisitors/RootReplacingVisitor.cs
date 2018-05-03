using CosmosDbContext.Collection;
using System.Linq;
using System.Linq.Expressions;

namespace CosmosDbContext.Collection.LinqProvider.ExpressionVisitors
{
    internal class RootReplacingVisitor : ExpressionVisitor
    {
        private readonly IQueryable newRoot;
        public RootReplacingVisitor(IQueryable newRoot)
        {
            this.newRoot = newRoot;
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsGenericType && (node.Type.GetGenericTypeDefinition() == typeof(CosmosDbCollection<>)))
            {
                return Expression.Constant(newRoot);
            }
            else
            {
                return node;
            }
        }
    }
}

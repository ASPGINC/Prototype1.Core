using FluentNHibernate.Mapping;
using NHibernate;

namespace Prototype1.Foundation.Data.Filters
{
    public class PermanentRecordFilter : FilterDefinition
    {
        public PermanentRecordFilter()
        {
            WithName("PermanentRecordFilter")
                .WithCondition("Deleted = :deleted")
                .AddParameter("deleted", NHibernateUtil.Boolean);
        }
    }
}

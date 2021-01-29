using NHibernate.Event;
using System.Threading;
using System.Threading.Tasks;

namespace Prototype1.Foundation.Data.Listener
{
    public class NullAuditListener : IAuditListener
    {
        public void OnPostUpdate(PostUpdateEvent @event)
        {
            //no-op
        }

        public Task OnPostUpdateAsync(PostUpdateEvent @event, CancellationToken cancellationToken) {
            //no-op
            return Task.CompletedTask;
        }

        public void OnPostInsert(PostInsertEvent @event)
        {
            //no-op
        }

        public Task OnPostInsertAsync(PostInsertEvent @event, CancellationToken cancellationToken) {
            //no-op
            return Task.CompletedTask;
        }


        public void OnPostDelete(PostDeleteEvent @event)
        {
            //no-op
        }
        public Task OnPostDeleteAsync(PostDeleteEvent @event, CancellationToken cancellationToken) {
            //no-op
            return Task.CompletedTask;
        }


        public void OnPostUpdateCollection(PostCollectionUpdateEvent @event)
        {
            //no-op
        }
        public Task OnPostUpdateCollectionAsync(PostCollectionUpdateEvent @event, CancellationToken cancellationToken) {
            //no-op
            return Task.CompletedTask;
        }

    }
}
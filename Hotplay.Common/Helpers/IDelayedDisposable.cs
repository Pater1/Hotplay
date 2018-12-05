using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.Helpers {
    public interface IDelayedDisposable : IDisposable {
        ICollection<Action> OnDispose { get; }
        ICollection<Func<Task>> OnDisposeAsync { get; }
        void DisposeInternal();
    }
    public static class IDelayedDisposableExtentions{
        public static void DefaultDelayedDispose(this IDelayedDisposable del) {
            ThreadPool.QueueUserWorkItem(async (x) => {
                foreach(Action a in del.OnDispose) {
                    a();
                }
                foreach(Func<Task> ft in del.OnDisposeAsync) {
                    await ft();
                }
                del.DisposeInternal();
            });
        }
    }
}

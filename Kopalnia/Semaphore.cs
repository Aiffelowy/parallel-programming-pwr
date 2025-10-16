using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kopalnia
{
    class Semaphore<T>(T value, int max)
    {
        private readonly SemaphoreSlim locker = new(max);

        public class LockedSemaphore : IDisposable
        {
            private readonly SemaphoreSlim locker;
            private readonly T value;

            internal LockedSemaphore(T value, SemaphoreSlim locker) { this.locker = locker; this.value = value; this.locker.Wait(); }
            public static T operator ~(LockedSemaphore l) => l.value;
            void IDisposable.Dispose()
            {
                this.locker.Release();
                GC.SuppressFinalize(this);
            }
        }

        public LockedSemaphore lock_()
        {
            return new LockedSemaphore(value, this.locker);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kopalnia
{
    class Mutex<T>(T value)
    {
        private readonly object locker = new();

        public class LockedMutex : IDisposable
        {
            private readonly object locker;
            private bool taken;
            private readonly T value;

            internal LockedMutex(T value, object locker) { this.locker = locker; this.value = value; Monitor.Enter(this.locker, ref this.taken); }
            public static T operator ~(LockedMutex l) => l.value;
            void IDisposable.Dispose()
            {
                if (this.taken) { Monitor.Exit(this.locker); this.taken = false; }
                GC.SuppressFinalize(this);
            }
        }

        public LockedMutex lock_() { return new LockedMutex(value, this.locker); }
    }
}

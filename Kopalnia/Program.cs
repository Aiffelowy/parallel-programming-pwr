class Mutex<T>(T value)
{
    private readonly object locker = new();

    public class LockedMutex : IDisposable {
        private readonly object locker;
        private bool taken;
        private readonly T value;

        internal LockedMutex(T value, object locker) { this.locker = locker; this.value = value; Monitor.Enter(this.locker, ref this.taken); }
        public static T operator ~(LockedMutex l) => l.value;
        void IDisposable.Dispose()
        {
            if(this.taken) { Monitor.Exit(this.locker); this.taken = false; }
            GC.SuppressFinalize(this);
        }
    }

    public LockedMutex lock_() { return new LockedMutex(value, this.locker);  }
}


class Semaphore<T>(T value, int max)
{
    private readonly SemaphoreSlim locker = new(max);

    public class LockedSemaphore : IDisposable {
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

    public LockedSemaphore lock_() {
        return new LockedSemaphore(value, this.locker);
    }
}



class Cursor
{
    static private readonly Mutex<object> cursor_lock = new(new object());
    public static void print(string text, int position) {
        using var lock_ = Cursor.cursor_lock.lock_();
        Console.CursorVisible = false;
        Console.SetCursorPosition(0, position);
        Console.WriteLine(text);
    }
}



class WareHouse {
    public int coal_count = 0;

    public void deposit(int number) { this.coal_count += number; }
}

class Mine(int coal)
{
    public int coal_count = coal;

    public int mine(int amount) {
        this.coal_count -= amount;
        return amount;
    }
}

class Worker(Mutex<WareHouse> warehouse_access, Semaphore<Mine> mine_access, int number)
{
    static readonly int TIME_TO_MINE = 3000;
    static readonly int TIME_TO_UNLOAD = 1000;
    static readonly int TRAVEL_TIME = 10000;
    public static readonly int CAPACITY = 200;

    private void print(string text) {
        Cursor.print($"Worker {number}: {text}                                       ", number+3);
    }
    public void work()
    {
        {
            while (true)
            {
                print("Traveling to the mine...");
                Thread.Sleep(Worker.TRAVEL_TIME);
                print("Waiting to access the mine...");
                using (var lock_ = mine_access.lock_())
                {
                    if((~lock_).coal_count < Worker.CAPACITY)
                    {
                        return;
                    }
                    print("Mining...");
                    Thread.Sleep(Worker.TIME_TO_MINE);
                    (~lock_).mine(Worker.CAPACITY);
                }
                print("Traveling to the warehouse....");
                Thread.Sleep(Worker.TRAVEL_TIME);
                print("Waiting to access the warehouse...");
                using (var lock_ = warehouse_access.lock_())
                {
                    print("Unloading...");
                    Thread.Sleep(Worker.TIME_TO_UNLOAD);
                    (~lock_).deposit(Worker.CAPACITY);
                }
            }
        }
    }
}

class Program
{
    static void Main(string[] args) {
        Mutex<WareHouse> warehouse = new(new WareHouse());
        Semaphore<Mine> mine = new(new Mine(20000), 2);
        Task[] tasks = new Task[5];
        for (int i = 0; i < 5; i++) {
            var local_i = i;
            tasks[i] = Task.Run(() => { var w = new Worker(warehouse, mine, local_i); w.work(); });
        }

        while(true)
        {
            using(var lock_ = warehouse.lock_())
            {
                Cursor.print($"Coal in the warehouse: {(~lock_).coal_count}             ", 0);
            }

            using(var lock_ =  mine.lock_())
            {
                Cursor.print($"Coal left in the mine: {(~lock_).coal_count }            ", 1);
                if((~lock_).coal_count < Worker.CAPACITY) { break; }
            }
        }

        Task.WaitAll(tasks);
    }
}

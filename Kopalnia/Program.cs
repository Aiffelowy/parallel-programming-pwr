using System.Diagnostics;

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
    public SemaphoreSlim getLocker() { return locker; }


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

    public bool isEmpty = false;

    public int mine(int amount) {
        this.coal_count -= amount;
        return amount;
    }
}

class Worker(Mutex<WareHouse> warehouse_access, Semaphore<Mine>[] mine_access, int number)
{
    static readonly int TIME_TO_MINE = 10;
    static readonly int TIME_TO_UNLOAD = 10;
    static readonly int [] TRAVEL_TIME = { 10000, 15000 };
    public static readonly int CAPACITY = 200;

    private void print(string text) {
        Cursor.print($"Worker {number}: {text}                                       ", number+6);
    }
    public void work(bool isRandom)
    {
        {
            int chosenMine;
            int[] mineCount = new int[2]{2000, 2000};
            bool[] isClosed = new bool[2] { false, false };
            while (true)
            {
                if (isClosed[0] && isClosed[1]) return;
                else if (isClosed[0]) chosenMine = 1;
                else if (isClosed[1]) chosenMine = 0;
                else
                {
                    if (isRandom)
                    {
                        chosenMine = new Random().Next() % 2;
                    }
                    else
                    {
                        if (mine_access[0].getLocker().CurrentCount > mine_access[1].getLocker().CurrentCount)
                        {
                            chosenMine = 0;
                        }
                        else
                        {
                            chosenMine = 1;
                        }
                    }
                }

                print($"Traveling to the mine nr {chosenMine +1}...");
                Thread.Sleep(Worker.TRAVEL_TIME[chosenMine]);
                print($"Waiting to access the mine nr {chosenMine + 1}...");
                using (var lock_ = mine_access[chosenMine].lock_())
                {
                    if((~lock_).coal_count < Worker.CAPACITY)
                    {
                        isClosed[chosenMine] = true;
                        print($"Mine {chosenMine + 1} is empty, going back to the warehouse...");
                    }
                    else
                    {
                        print($"Mining in mine {chosenMine + 1}...");
                        Thread.Sleep(Worker.TIME_TO_MINE);
                        (~lock_).mine(Worker.CAPACITY);
                        mineCount[chosenMine] = (~lock_).coal_count;
                    }
                }
                print("Traveling to the warehouse....");
                Thread.Sleep(Worker.TRAVEL_TIME[0]);
                if (isClosed[chosenMine]) continue;
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

class Solutions
{
    public static void thirdTask(int maxWorkers)
    {
        int[] time = new int[maxWorkers];
        float[] acceleration = new float[maxWorkers];
        float[] efficiency = new float[maxWorkers];


        for (int i = 1; i <= maxWorkers; i++)
        {
            Mutex<WareHouse> warehouse = new(new WareHouse());
            var mine = new[]
            {
                new Semaphore<Mine>(new Mine(2000), 2),
                new Semaphore<Mine>(new Mine(2000), 2)
            };
            Stopwatch timeOperation = Stopwatch.StartNew();
            Task[] tasks = new Task[i];
            for (int j = 0; j < i; j++)
            {
                var local_j = j;
                tasks[j] = Task.Run(() => { var w = new Worker(warehouse, mine, local_j); w.work(true); });
            }
            Cursor.print($"Simulation with {i} workers:", 0);
            while (true)
            {
                using (var lock_ = warehouse.lock_())
                {
                    Cursor.print($"Coal in the warehouse: {(~lock_).coal_count}             ", 1);
                }

                using (var lock_ = mine[0].lock_())
                {
                    Cursor.print($"Coal left in the first mine: {(~lock_).coal_count}            ", 2);
                    if ((~lock_).coal_count < Worker.CAPACITY) { break; }
                }
                using (var lock_ = mine[1].lock_())
                {
                    Cursor.print($"Coal left in the second mine: {(~lock_).coal_count}            ", 3);
                    if ((~lock_).coal_count < Worker.CAPACITY) { break; }
                }
            }

            Task.WaitAll(tasks);
            timeOperation.Stop();
            time[i - 1] = (int)timeOperation.ElapsedMilliseconds;
            acceleration[i - 1] = (float)time[0] / (float)time[i - 1];
            efficiency[i - 1] = acceleration[i - 1] / i * 100;
        }
        Console.Clear();
        for (int j = 0; j < maxWorkers; j++)
        {
            Console.WriteLine($"Workers: {j + 1}, Time: {time[j]} ms, Acceleration: {acceleration[j]:F2}, Efficiency: {efficiency[j]:F2} %\n");
        }
    }
}

class Program
{
    static void Main(string[] args) {
        Solutions.thirdTask(4);
        //Mutex<WareHouse> warehouse = new(new WareHouse());
        //Semaphore<Mine> mine = new(new Mine(20000), 2);
        //Semaphore<Mine> secondMine = new(new Mine(20000), 2);
        //Task[] tasks = new Task[5];
        //for (int i = 0; i < 5; i++)
        //{
        //    var local_i = i;
        //    tasks[i] = Task.Run(() => { var w = new Worker(warehouse, mine, local_i); w.work(); });
        //}

        //while (true)
        //{
        //    using (var lock_ = warehouse.lock_())
        //    {
        //        Cursor.print($"Coal in the warehouse: {(~lock_).coal_count}             ", 0);
        //    }

        //    using (var lock_ = mine.lock_())
        //    {
        //        Cursor.print($"Coal left in the mine: {(~lock_).coal_count}            ", 1);
        //        if ((~lock_).coal_count < Worker.CAPACITY) { break; }
        //    }
        //}

        //Task.WaitAll(tasks);
    }
}

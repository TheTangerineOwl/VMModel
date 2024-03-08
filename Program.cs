namespace VirtualMemory
{
    class Program
    {

        public static MemoryModel? memory;

        public static void PrintValue(int index)
        {
            Console.Write(index + ": ");
            if (memory[index] != null)
                Console.Write(memory[index]);
            else Console.Write("не записано");
            Console.Write(", страница в памяти " + (index / MemoryPage.byteNum + 1));
            Console.WriteLine(", страница в буфере " + memory.FindPage(index) + "\n");
        }

        static void Main()
        {

            memory = new MemoryModel();

            Console.WriteLine("Вывод значений: \n");

            PrintValue(0);
            PrintValue(200);
            PrintValue(128 * 5);

            Console.WriteLine("\nЗапись значений...");

            memory[0] = 0b0101010101010101;

            memory[200] = int.MinValue;

            memory[128 * 5] = int.MaxValue;


            Console.WriteLine("\nВывод новых значений");

            PrintValue(0);
            PrintValue(200);
            PrintValue(128 * 5);


            //Test();



            memory.SavePage(0);
            memory.SavePage(1);
            memory.SavePage(2);

        }

        // Функция для теста заполнения всего массива.
        public static void Test()
        {
            for (int i = 0; i < 10000; i++)
            {
                memory[i] = i;
                PrintValue(i);
            }
        }
    }
}
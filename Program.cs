// Моделирование системы управления виртуальной памятью на языке C#.
namespace VirtualMemory
{
    class Program
    {
        // Используемая модель виртуальной памяти.
        public static MemoryModel? memory;

        /// <summary>
        /// Вывод в консоль информации о значении по данному индексу, лежащем в памяти.
        /// </summary>
        /// <param name="index">Индекс значения в моделируемом массиве. </param>
        public static void PrintValue(int index)
        {
            Console.Write(index + ": ");
            if (memory[index] != null)
                Console.Write(memory[index]);
            else
                Console.Write("не записано");
            Console.Write(", страница в памяти " + (int)Math.Ceiling((double)(index / (MemoryPage.byteNum * 8))));
            Console.WriteLine(", страница в буфере " + memory.FindPage(index) + "\n");
        }

        /// <summary>
        /// Демонстрация работы программы.
        /// </summary>
        static void Main()
        {
            memory = new MemoryModel(3, 10112);

            Console.WriteLine("Вывод значений: \n");

            PrintValue(0);
            PrintValue(255);
            PrintValue(639);

            Console.WriteLine("\nЗапись значений...");

            memory[0] = 3;
            memory[255] = 5;
            memory[639] = 10;

            Console.WriteLine("\nВывод новых значений: ");

            PrintValue(0);
            PrintValue(255);
            PrintValue(639);
        }
    }
}
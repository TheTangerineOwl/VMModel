using System;
using System.Text;

namespace VirtualMemory
{
    /// <summary>
    /// Класс, определяющий модель виртуальной памяти.
    /// </summary>
    public class MemoryModel : IDisposable
    {
        int _bufferSize;
        int _arraySize;
        // Файловый указатель виртуального массива.
        Stream array;
        // Буфер страниц.
        MemoryPage[] pages;

        /// <summary>
        /// Страничная модель виртуальной памяти, в которой хранится массив указанной длины, хранящаяся в файле дампа.
        /// </summary>
        /// <param name="bufferSize">Количество страниц в буфере.</param>
        /// <param name="arraySize">Размер виртуального массива.</param>
        /// <param name="filename">Имя файла дампа.</param>
        public MemoryModel(int bufferSize = 3, int arraySize = 10000, string filename = "vm.bin")
        {
            _bufferSize = bufferSize;
            _arraySize = arraySize;
            // В случае, если отсутствует файл дампа, создает новый файл и заполняет нулями нужное количество страниц.
            if (!File.Exists(filename))
            {
                array = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
                byte[] buffer = Encoding.Default.GetBytes("VM");
                array.Write(buffer, 0, buffer.Length);
                int bytesCount = (int)Math.Ceiling((double)(arraySize / (MemoryPage.byteNum * 8))) * MemoryPage.pageSize;
                for (int i = 0; i < bytesCount; i++)
                    array.WriteByte(0);
            }
            else
                array = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
            // Пропускает сигнатуру в начале файла.
            array.Seek(2, SeekOrigin.Begin);
            pages = new MemoryPage[bufferSize];
            // Создание массива для промежуточного считывания необходимого количества страниц из файла дампа и их чтение.
            byte[] data = new byte[bufferSize * MemoryPage.pageSize];
            array.Read(data, 0, data.Length);
            for (int i = 0; i < bufferSize; i++)
            {
                MemoryPage newPage = new MemoryPage(i);
                // Копирование битовой карты из памяти.
                Buffer.BlockCopy(data, i * MemoryPage.pageSize, newPage.Bitmap, 0, MemoryPage.byteNum);
                Buffer.BlockCopy(data, i * MemoryPage.pageSize + MemoryPage.byteNum, newPage.Data, 0, MemoryPage.byteNum * sizeof(int) * 8);
                pages[i] = newPage;
            }

        }

        /// <summary>
        /// Получение информации о бите с заданным индексом в битовой карте.
        /// </summary>
        /// <param name="_bitmap">Битовая карта страницы.</param>
        /// <param name="index">Индекс бита в битовой карте.</param>
        /// <returns>true, если бит равен 1; false, если бит равен 0.</returns>
        public static bool GetBit(byte[] _bitmap, int index)
        {
            byte areaByte = _bitmap[index / 8];
            if (areaByte == 0) return false;
            int mask = 0b1 << (index % 8);
            return (areaByte & mask) != 0;
        }

        /// <summary>
        /// Установка бита с заданным индексом в битовой карте в указанное значение.
        /// </summary>
        /// <param name="_bitmap">Битовая карта страницы.</param>
        /// <param name="index">Индекс бита в битовой карте.</param>
        /// <param name="value">Значение для установки: true, чтобы установить бит 1; false, чтобы установить бит 0.</param>
        public static void SetBit(ref byte[] _bitmap, int index, bool value)
        {
            byte mask;
            if (value)
            {
                mask = (byte)(0b1 << (index % 8));
                _bitmap[index / 8] = (byte)(_bitmap[index / 8] | mask);
            }
            else
            {
                mask = (byte)(~(0b1 << (index % 8)));
                _bitmap[index / 8] = (byte)(_bitmap[index / 8] & mask);
            }
        }

        /// <summary>
        /// Определение и возврат номера страницы в буфере по индексу значения в массиве.
        /// </summary>
        /// <param name="index">Индекс значения в массиве.</param>
        /// <returns>Номер страницы в буфере; null, если такой страницы не существует.</returns>
        public int? FindPage(int index)
        {
            // Определение абсолютного номера страницы.
            int pageIndex = index / (MemoryPage.byteNum * 8);
            if (pageIndex > _arraySize / (MemoryPage.byteNum * 8))
                return null;

            // Поиск страницы с указанным номером в буфере.
            MemoryPage? page = pages.FirstOrDefault(page => page.Index == pageIndex);

            // Если отсутствует в буфере, то самая старая страница заменяется нужной.
            if (page == null)
            {
                // Поиск самой старой страницы в буфере.
                MemoryPage toRemove = pages.Aggregate((x, y) => x.ModTime < y.ModTime ? x : y);
                int toRemoveIndex = Array.IndexOf(pages, toRemove);

                if (toRemove.IsModified == true)
                    SavePage(toRemoveIndex);

                MemoryPage newPage = new MemoryPage(pageIndex);

                // Чтение страницы для замены старой из памяти.
                array.Seek(2 + MemoryPage.pageSize * pageIndex, SeekOrigin.Begin);
                array.Read(newPage.Bitmap, 0, newPage.Bitmap.Length);
                array.Read(newPage.Data, 0, newPage.Data.Length);
                pages[toRemoveIndex] = newPage;
                return toRemoveIndex;
            }

            return Array.IndexOf(pages, page);
        }

        /// <summary>
        /// Чтение значения из буфера страниц.
        /// </summary>
        /// <param name="value">Переменная, куда будет записано значение.</param>
        /// <param name="elementIndex">Индекс значения в массиве.</param>
        /// <returns>true, если операция была завершена успешно.</returns>
        public bool ReadValue(out int value, int elementIndex)
        {
            int pageIndex = FindPage(elementIndex) ?? -1;
            value = 0;

            if (pageIndex == -1)
                return false;

            int onPageIndex = elementIndex % (MemoryPage.byteNum * 8);
            // Если значение не было задано.
            if (!GetBit(pages[pageIndex].Bitmap, onPageIndex))
                return false;

            byte[] bytes = new byte[sizeof(int)];
            Buffer.BlockCopy(pages[pageIndex].Data, onPageIndex * sizeof(int), bytes, 0, sizeof(int));
            value = BitConverter.ToInt32(bytes, 0);
            return true;
        }

        /// <summary>
        /// Запись значения в буфер страниц.
        /// </summary>
        /// <param name="value">Значение для записи.</param>
        /// <param name="elementIndex">Индекс для записи значения в массив.</param>
        /// <returns></returns>
        public bool WriteValue(int value, int elementIndex)
        {
            int pageIndex = FindPage(elementIndex) ?? -1;
            if (pageIndex == -1)
                return false;

            int onPageIndex = elementIndex % (MemoryPage.byteNum * 8);
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, pages[pageIndex].Data, onPageIndex * sizeof(int), sizeof(int));
            SetBit(ref pages[pageIndex].Bitmap, onPageIndex, true);
            pages[pageIndex].IsModified = true;
            return true;
        }

        /// <summary>
        /// Сохранение страницы из буфера в память.
        /// </summary>
        /// <param name="index">Индекс страницы в буфере.</param>
        public void SavePage(int index)
        {
            array.Seek(2 + MemoryPage.pageSize * pages[index].Index, SeekOrigin.Begin);
            array.Write(pages[index].Bitmap, 0, pages[index].Bitmap.Length);
            array.Write(pages[index].Data, 0, pages[index].Data.Length);
        }

        /// <summary>
        /// Перегрузка индексатора.
        /// </summary>
        /// <param name="index">Индекс значения в памяти.</param>
        /// <returns>Значение из памяти.</returns>
        /// <exception cref="ArgumentNullException">Исключение при попытке записать null в память.</exception>
        public int? this[int index]
        {
            get
            {
                if (ReadValue(out int value, index))
                    return value;
                else
                    return null;
            }

            set
            {
                if (value != null)
                    WriteValue(value.Value, index);
                else
                    throw new ArgumentNullException("Значение не может быть null!", nameof(value));
            }
        }

        /// <summary>
        /// Освобождение ресурсов при окончании работы.
        /// </summary>
        public void Dispose()
        {
            array.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Деструктор класса при завершении работы.
        /// </summary>
        ~MemoryModel()
        {
            for (int i = 0; i < pages.Length; i++)
                SavePage(i);
            array.Dispose();
        }

    }
}

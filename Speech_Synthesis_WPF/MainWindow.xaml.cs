using System;                       
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Data.SqlClient;        // Подключение пространства имен для работы с SQL Server (2017)
using System.Speech.Synthesis;      // Подключение пространства имен для работы с синтезом речи
using System.Speech.Recognition;    // Подключение пространства имен для работы с распознованием речи
using System.Diagnostics;           
using System.IO.Ports;              // Подключение пространства имен для работы с микроконтроллерами типа Arduino
                                    

namespace Speech_Synthesis_WPF
{    
    public partial class Speech : Window
    {
        // Предоставляет доступ к функциям установленного модуля синтеза речи.
        SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();

        // Логическая переменная для отправки голосового помощника в сон и для пробуждения.
        Boolean wake_up = true;

        // COM порт для сообщения и передачи логических команд внешнему микроконтроллеру (Arduino NANO 3.0)
        SerialPort ArduinoPort = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);


        // Представляет набор вариантов в соответствии с ограничениями грамматики распознавания речи.
        Choices ChoicesList = new Choices();

        // Главный метод приложения (равноценен методу Main в консольном приложении)
        public Speech()
        {
            InitializeComponent();
            speechSynthesizer.SelectVoiceByHints(VoiceGender.Female);       // Голос синтезатора речи - женский
            // Озвучивание фразы приветствия при старте программы
            speechSynthesizer.Speak("Hi, my name Miranda! I will help with your routine operations!");                    

            System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop).ToString();
            // Предоставляет средства для доступа и управления распознаванием речи в процессе работы программы.
            SpeechRecognitionEngine speechRecognitioEngine = new SpeechRecognitionEngine();
                        
            // Добавление в список массива команд, на которые реагирует и которые выполняет программа.
            // Метод принимает массив с строк string
            // Метод ConnectToSQL(); возвращает необходимый массив; Метод подключается к базе данных SQL сервера и берет данные из таблицы с командами.
            ChoicesList.Add(ConnectToSQL());

            //ChoicesList.Add(new String[] { "Hello", "how are you", "what time is it", "what is today", "open google", "open youtube", "open notepad", "close notepad", "wake up", "sleep", "restart", "update", "ligth on", "light off", "open word", "close word", "open excel", "close excel" });

            // Объект среды выполнения, который ссылается на грамматику распознавания речи,
            // которую приложение будет использовать для распознавания речи.
            Grammar grammar = new Grammar(new GrammarBuilder(ChoicesList));

            try
            {
                speechRecognitioEngine.RequestRecognizerUpdate();                       // Запросы, с помощью которых что распознаватель речи приостанавливается и обновляет состояние.
                speechRecognitioEngine.LoadGrammar(grammar);                            // Загрузка объекта грамматики в распознаватель речи
                speechRecognitioEngine.SpeechRecognized += RecoEng_SpeechRecognized;    // Подписка движка распознования речи на событие, в котором определена логика реакций на озвученные команды
                speechRecognitioEngine.SetInputToDefaultAudioDevice();                  // Настраивает System.Speech.Recognition.SpeechRecognitionEngine для получения входных данных из аудиоустройства по умолчанию.
                speechRecognitioEngine.RecognizeAsync(RecognizeMode.Multiple);          // Асинхронно выполняет одну или несколько операций распознавания речи.
            }
            catch (Exception) { return; }            
        }

        // -------------------------------------------------------------------------------------------------------------------------------
        // Методы и логика приложения

        // Метод-событие в котором определена логика реакций на озвученные команды.
        private void RecoEng_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // То что пользователь говорит
            String r = e.Result.Text;           // Получает данные результатов распознавания, связанного с событием распознавания речи.

            Do(r);                              // Выполнение метода, непосредственно реализующего логику реакций на команды

            textBox1.AppendText(r + "\n");      // Вывод того что сказал пользовательв textBox1
        }

        // Нажатие на кнопку.
        // Программой можно управлять не только с помощью голоса, но и с помощью записи команд в textBox3 и нажатия кнопки
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            String x = textBox3.Text;       // Получение команды
            Do(x);                          // Вышеописанный метод с логикой
            textBox3.Clear();               // Очистка textBox3 после выполнения команды
        }

        // ---------------------------------------------------------------------------------------------------------------------------------

        // Метод завершения запущенного процесса - для команд типа "закрыть _приложение_"
        public static void KillProcess(string s)
        {
            System.Diagnostics.Process[] processes = null;      // Массив системных процессов

            try
            {
                processes = Process.GetProcessesByName(s);      // Выбор процессов по имени
                Process programm = processes[0];                // и помещение в массив

                if (!programm.HasExited) programm.Kill();                                           // закрывает 1 процесс
                //foreach (Process process in processes) if (!process.HasExited) process.Kill();    // закрывает все процессы соответствубщего типа (например все открытые ворды, блокноты и т.п.)
            }
            catch (Exception)
            {
                MessageBox.Show("Нет открытых приложений");                                         // если открытых приложений такого типа нет - оповещение с помощью выброса исключительной ситуации
            }
            finally
            {
                if (processes != null) { foreach (Process p in processes) p.Dispose(); }            // освобождение памяти в массиве системных процессов
            }
        }

        // Метод перезагрузки программы; Выполняется на команду "Restart"
        // Также используется для симуляции обновления программы
        public void Restart()
        {            
            Process.Start("Speech_Synthesis_WPF.exe");
            Environment.Exit(0);
        }

        // Метод, с помощью которого программа "говорит", задействуя функционал синтеза речи
        public void Say(string message)
        {
            textBox2.AppendText(message + "\n");        // Вывод сказанного программой в textBox2
            speechSynthesizer.SpeakAsync(message);      // (асинхронно) озвучивание (дубляж) программой команды пользователя 
                                                        // или иная определенная реакция на соответствующую команду
        }

        // Метод, непосредственно определяющий логику реакции на пользовательские команды, как голосовые так и написанные текстом.
        public void Do(string r)
        {
            // Смена состояния программы (сон / бодрствование)
            // При этом меняется цвет и надпись состояния на форме программы
            switch (r)
            {
                case ("wake up"):
                    wake_up = true;
                    label4.Foreground = Brushes.LimeGreen;
                    label4.Content = "State: Awake";
                    break;

                case ("sleep"):
                    wake_up = false;
                    label4.Foreground = Brushes.Red;
                    label4.Content = "State: Sleep mode";
                    break;
            }

            // Для управления внешним микроконтроллером типа Arduino NANO 3.0 Can. 
            // Тут определены простые команды, заставляющие микроконтроллер включать/выключать светодиоды
            switch (r)
            {
                case ("light on"):
                    ArduinoPort.Open();
                    ArduinoPort.WriteLine("A");
                    ArduinoPort.Close();
                    break;

                case ("light off"):
                    ArduinoPort.Open();
                    ArduinoPort.WriteLine("B");
                    ArduinoPort.Close();
                    break;
            }   // ------------------------------------------------------------

            // если программа "бодрствует"
            if (wake_up == true)
            {
                switch (r)
                {
                    case ("restart"):
                        speechSynthesizer.SpeakAsync("restarting!"); // голосовой вывод программы, то что программа "говорит"
                        Restart();                                   // выполнение метода Restart();
                        break;
                    case ("update"):
                        speechSynthesizer.SpeakAsync("updating");
                        Restart();                                  // имитация обновления с помощью метода перезагрузки

                        break;
                    case ("hi"):
                        Say("hi");                                 // приветствие. реакция на команду "привет"
                        break;
                    case ("hello"):
                        Say("hello");
                        break;
                    case ("how are you"):
                        Say("I'm great, and you?");
                        break;
                    case ("what time is it"):                      // вывод и озвучивание даты\времени при вопросе о текущем времени
                        Say(DateTime.Now.ToString("h:mm tt"));
                        break;
                    case ("what is today"):                        // вывод и озвучивание текущей даты при вопросе о дате
                        Say(DateTime.Now.ToString("M/d/yyyy"));
                        break;

                    case ("open google"):                          // открывает гугл по запросу и озвучивает свои действия
                        Say("Opening google!");
                        Process.Start("http://google.by");
                        break;
                    case ("open youtube"):                         // открывает ютую по запросу и озвучивает свои действия
                        Say("Opening youtube!");
                        Process.Start("http://youtube.com");
                        break;

                    case ("open notepad"):                         // открывает блокнот по запросу и озвучивает свои действия
                        Say("Opening notepade!");
                        Process.Start("notepad.exe");
                        break;
                    case ("close notepad"):                        // закрывает блокнот по запросу и озвучивает свои действия
                        Say("Closing Notepad!");
                        KillProcess("notepad");
                        break;

                    case ("open word"):                            // открывает ворд по запросу и озвучивает свои действия
                        Say("Opening Microsoft Word!");
                        Process.Start("WINWORD.exe");
                        break;
                    case ("close word"):                           // закрывает ворд по запросу и озвучивает свои действия
                        Say("Closing Microsoft Word!");
                        KillProcess("WINWORD");
                        break;

                    case ("open excel"):                           // открывает эксел по запросу и озвучивает свои действия
                        Say("Opening Microsoft Excel!");
                        Process.Start("EXCEL.exe");
                        break;
                    case ("close excel"):                          // закрывает эксел по запросу и озвучивает свои действия
                        Say("Closing Microsoft Excel!");
                        KillProcess("EXCEL");
                        break;

                    default:                                       // реакция на "неизвестную" (неопределенную) команду
                        Say("Unknown command!");
                        break;
                }
            }
        }

        // Метод подключения к SQL серверу и конкретной базе данных, выборка данных, помещение данных в массив строк string,
        // возврат методом готового массива -- для помещения в объект "ChoicesList", который формирует словарь для движка распознования речи.
        public static string[] ConnectToSQL()
        {
            List<string> L = new List<string>();    // список данных типа string        

            // Подключение к серверу
            SqlConnection sqlConnection = new SqlConnection
            {                                                   // контрольная строка, "строка доступа" для подключения к ядру SQL сервера
                ConnectionString = "Integrated Security=SSPI; Initial Catalog=CommandsDb;Data Source=DESKTOP-IUL44S8\\SQLEXPRESS"
            };

            // Запрос к базе данных на выборку данных из таблицы, хранящей необходимые нам команды для движка распознования речи
            SqlCommand sqlCommand = new SqlCommand("select * from Commands", sqlConnection);
            sqlConnection.Open(); // открытие соеденения с сервером

            // Чтение таблицы базы данных
            SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();

            // Помещение считанных данных в созданный выше список
            while (sqlDataReader.Read())
            {
                L.Add(sqlDataReader["Command"].ToString());
            }

            // создание массива строк, емкастью, равной количеству элементов в списке L
            string[] M = new string[L.Count];

            // Перемещение элементов из списка в массив
            for (int i = 0; i < L.Count; i++)
            {
                M[i] = L[i];
            }

            // возврат массива методом
            return M;
        }
    }
}

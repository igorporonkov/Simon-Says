// simon.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Runtime.InteropServices;

class Simon
{
    static string Colorize(string text, string color)
    {
        string col = color switch
        {
            "green" => "\x1b[92m",
            "red" => "\x1b[91m",
            "blue" => "\x1b[94m",
            "yellow" => "\x1b[93m",
            "bold" => "\x1b[1m",
            _ => "\x1b[0m"
        };
        return col + text + "\x1b[0m";
    }

    class ColorInfo
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string Symbol { get; set; }
        public char Key { get; set; }
    }

    static List<ColorInfo> colors = new()
    {
        new ColorInfo { Name = "Зелёный", Color = "green", Symbol = "🟢", Key = '1' },
        new ColorInfo { Name = "Красный", Color = "red", Symbol = "🔴", Key = '2' },
        new ColorInfo { Name = "Синий", Color = "blue", Symbol = "🔵", Key = '3' },
        new ColorInfo { Name = "Жёлтый", Color = "yellow", Symbol = "🟡", Key = '4' }
    };

    static string ConfigFile => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".simon_record.json");

    static int LoadRecord()
    {
        if (!File.Exists(ConfigFile)) return 0;
        string json = File.ReadAllText(ConfigFile);
        var data = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
        return data.GetValueOrDefault("record", 0);
    }

    static void SaveRecord(int record)
    {
        var data = new Dictionary<string, int> { { "record", record } };
        string json = JsonSerializer.Serialize(data);
        File.WriteAllText(ConfigFile, json);
    }

    static void ClearScreen()
    {
        Console.Clear();
    }

    static void PlayBeep(int freq, int dur)
    {
        Console.Beep(freq, dur);
    }

    static void ShowSequence(List<int> seq, int speed, int roundNum, int record)
    {
        ClearScreen();
        Console.WriteLine(Colorize(new string('=', 50), "bold"));
        Console.WriteLine(Colorize($"🎮  САЙМОН ГОВОРИТ  |  Раунд {roundNum}  |  Рекорд: {record}", "bold"));
        Console.WriteLine(Colorize(new string('=', 50), "bold"));
        Console.WriteLine("\nЗапоминай последовательность цветов:\n");

        foreach (int idx in seq)
        {
            var c = colors[idx];
            Console.WriteLine(Colorize($"  {c.Symbol}  {c.Name}", c.Color));
            PlayBeep(440 + idx * 100, 200);
            Thread.Sleep(speed);
            Console.Write("\033[A");
            Thread.Sleep(50);
        }

        Console.WriteLine("\n" + Colorize("Твоя очередь!", "bold"));
        Console.Write("Нажимай ");
        foreach (var c in colors) Console.Write($"{c.Key} ({c.Name}) ");
        Console.WriteLine("\nДля выхода нажми 'q'\n");
    }

    static bool GetUserInput(List<int> seq, int timeout)
    {
        for (int i = 0; i < seq.Count; i++)
        {
            var start = DateTime.Now;
            while (true)
            {
                if (timeout > 0 && (DateTime.Now - start).TotalSeconds > timeout)
                {
                    Console.WriteLine(Colorize("⏰ Время вышло!", "red"));
                    return false;
                }
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).KeyChar;
                    if (key == 'q' || key == 'Q')
                    {
                        Console.WriteLine(Colorize("\nВыход из игры.", "yellow"));
                        Environment.Exit(0);
                    }
                    for (int j = 0; j < colors.Count; j++)
                    {
                        if (key == colors[j].Key)
                        {
                            if (j == seq[i])
                            {
                                Console.WriteLine(Colorize($"  {colors[j].Symbol}  Верно!", colors[j].Color));
                                PlayBeep(880, 100);
                                break;
                            }
                            else
                            {
                                Console.WriteLine(Colorize($"  {colors[j].Symbol}  Ошибка! (ожидался {colors[seq[i]].Name})", "red"));
                                PlayBeep(200, 300);
                                return false;
                            }
                        }
                    }
                    break;
                }
                Thread.Sleep(10);
            }
        }
        return true;
    }

    static void Main(string[] args)
    {
        int speed = 800, rounds = 0, timeout = 0;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-s" && i + 1 < args.Length) speed = int.Parse(args[++i]);
            else if (args[i] == "-r" && i + 1 < args.Length) rounds = int.Parse(args[++i]);
            else if (args[i] == "-t" && i + 1 < args.Length) timeout = int.Parse(args[++i]);
            else if (args[i] == "-h" || args[i] == "--help")
            {
                Console.WriteLine("Usage: simon [options]\n  -s <ms>   Speed (default 800)\n  -r <N>    Rounds (0 = infinite)\n  -t <sec>  Input timeout");
                return;
            }
        }

        int record = LoadRecord();
        Console.WriteLine(Colorize("🎮  Добро пожаловать в игру САЙМОН ГОВОРИТ!", "bold"));
        Console.WriteLine($"Твой текущий рекорд: {Colorize(record.ToString(), "green")}");
        Console.WriteLine("Нажми любую клавишу для начала...");
        Console.ReadKey(true);

        var seq = new List<int>();
        int roundNum = 1;
        var rand = new Random();

        while (true)
        {
            seq.Add(rand.Next(4));
            ShowSequence(seq, speed, roundNum, record);

            bool success = GetUserInput(seq, timeout);
            if (!success)
            {
                Console.WriteLine(Colorize("\n❌ Игра окончена!", "red"));
                Console.Write("Правильная последовательность: ");
                foreach (int x in seq) Console.Write(colors[x].Name + " ");
                Console.WriteLine();
                if (roundNum - 1 > record)
                {
                    record = roundNum - 1;
                    SaveRecord(record);
                    Console.WriteLine(Colorize($"🎉 Новый рекорд: {record}!", "green"));
                }
                else
                {
                    Console.WriteLine($"Твой результат: {roundNum - 1} раундов");
                }
                break;
            }

            if (rounds > 0 && roundNum >= rounds)
            {
                Console.WriteLine(Colorize($"\n🏆 Поздравляем! Ты прошёл все {roundNum} раундов!", "green"));
                if (roundNum > record)
                {
                    record = roundNum;
                    SaveRecord(record);
                    Console.WriteLine(Colorize($"🎉 Новый рекорд: {record}!", "green"));
                }
                break;
            }

            Console.WriteLine(Colorize($"\n✅ Раунд {roundNum} пройден!", "green"));
            roundNum++;
        }
    }
}

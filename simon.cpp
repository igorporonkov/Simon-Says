// simon.cpp
#include <iostream>
#include <vector>
#include <random>
#include <chrono>
#include <thread>
#include <fstream>
#include <string>
#include <termios.h>
#include <unistd.h>
#include <fcntl.h>
#include <cstdlib>
#include <json/json.h> // sudo apt-get install libjsoncpp-dev

using namespace std;

const string RESET = "\033[0m";
const string GREEN = "\033[92m";
const string RED = "\033[91m";
const string BLUE = "\033[94m";
const string YELLOW = "\033[93m";
const string BOLD = "\033[1m";

string colorize(const string& text, const string& color) {
    return color + text + RESET;
}

struct ColorInfo {
    string name;
    string color;
    string symbol;
    char key;
};

vector<ColorInfo> colors = {
    {"Зелёный", GREEN, "🟢", '1'},
    {"Красный", RED, "🔴", '2'},
    {"Синий", BLUE, "🔵", '3'},
    {"Жёлтый", YELLOW, "🟡", '4'}
};

string getHomeDir() {
    const char* home = getenv("HOME");
    if (!home) home = getenv("USERPROFILE");
    return string(home);
}

string getRecordFile() {
    return getHomeDir() + "/.simon_record.json";
}

int loadRecord() {
    ifstream f(getRecordFile());
    if (!f) return 0;
    Json::Value root;
    f >> root;
    return root.get("record", 0).asInt();
}

void saveRecord(int record) {
    Json::Value root;
    root["record"] = record;
    ofstream f(getRecordFile());
    f << root.toStyledString();
}

char getch() {
    struct termios oldt, newt;
    char ch;
    tcgetattr(STDIN_FILENO, &oldt);
    newt = oldt;
    newt.c_lflag &= ~(ICANON | ECHO);
    tcsetattr(STDIN_FILENO, TCSANOW, &newt);
    ch = getchar();
    tcsetattr(STDIN_FILENO, TCSANOW, &oldt);
    return ch;
}

void clearScreen() {
    cout << "\033[2J\033[1;1H";
}

void playBeep(int freq, int dur) {
    // Используем system beep (если установлен)
    string cmd = "beep -f " + to_string(freq) + " -l " + to_string(dur);
    system(cmd.c_str());
}

void showSequence(const vector<int>& seq, int speed, int roundNum, int record) {
    clearScreen();
    cout << colorize(string(50, '='), BOLD) << endl;
    cout << colorize("🎮  САЙМОН ГОВОРИТ  |  Раунд " + to_string(roundNum) +
                     "  |  Рекорд: " + to_string(record), BOLD) << endl;
    cout << colorize(string(50, '='), BOLD) << endl;
    cout << "\nЗапоминай последовательность цветов:\n" << endl;

    for (int idx : seq) {
        auto& c = colors[idx];
        cout << colorize("  " + c.symbol + "  " + c.name, c.color) << endl;
        playBeep(440 + idx * 100, 200);
        this_thread::sleep_for(chrono::milliseconds(speed));
        cout << "\033[A";
        this_thread::sleep_for(chrono::milliseconds(50));
    }

    cout << "\n" << colorize("Твоя очередь!", BOLD) << endl;
    cout << "Нажимай ";
    for (auto& c : colors) {
        cout << c.key << " (" << c.name << ") ";
    }
    cout << endl << "Для выхода нажми 'q'\n" << endl;
}

bool getUserInput(const vector<int>& seq, int timeout) {
    for (size_t i = 0; i < seq.size(); ++i) {
        auto start = chrono::steady_clock::now();
        while (true) {
            if (timeout > 0) {
                auto now = chrono::steady_clock::now();
                if (chrono::duration_cast<chrono::seconds>(now - start).count() > timeout) {
                    cout << colorize("⏰ Время вышло!", RED) << endl;
                    return false;
                }
            }
            char ch = getch();
            if (ch == 'q' || ch == 'Q') {
                cout << colorize("\nВыход из игры.", YELLOW) << endl;
                exit(0);
            }
            for (int j = 0; j < 4; ++j) {
                if (ch == colors[j].key) {
                    if (j == seq[i]) {
                        cout << colorize("  " + colors[j].symbol + "  Верно!", colors[j].color) << endl;
                        playBeep(880, 100);
                        break;
                    } else {
                        cout << colorize("  " + colors[j].symbol + "  Ошибка! (ожидался " +
                                         colors[seq[i]].name + ")", RED) << endl;
                        playBeep(200, 300);
                        return false;
                    }
                }
            }
            break;
        }
    }
    return true;
}

int main(int argc, char* argv[]) {
    int speed = 800;
    int rounds = 0;
    int timeout = 0;

    for (int i = 1; i < argc; ++i) {
        string arg = argv[i];
        if (arg == "-s" && i+1 < argc) speed = stoi(argv[++i]);
        else if (arg == "-r" && i+1 < argc) rounds = stoi(argv[++i]);
        else if (arg == "-t" && i+1 < argc) timeout = stoi(argv[++i]);
        else if (arg == "-h" || arg == "--help") {
            cout << "Usage: simon [options]\n"
                 << "  -s <ms>   Speed (default 800)\n"
                 << "  -r <N>    Rounds (0 = infinite)\n"
                 << "  -t <sec>  Input timeout\n";
            return 0;
        }
    }

    int record = loadRecord();
    cout << colorize("🎮  Добро пожаловать в игру САЙМОН ГОВОРИТ!", BOLD) << endl;
    cout << "Твой текущий рекорд: " << colorize(to_string(record), GREEN) << endl;
    cout << "Нажми любую клавишу для начала..." << endl;
    getch();

    vector<int> sequence;
    int roundNum = 1;
    while (true) {
        sequence.push_back(rand() % 4);
        showSequence(sequence, speed, roundNum, record);

        bool success = getUserInput(sequence, timeout);
        if (!success) {
            cout << colorize("\n❌ Игра окончена!", RED) << endl;
            cout << "Правильная последовательность: ";
            for (int x : sequence) cout << colors[x].name << " ";
            cout << endl;
            if (roundNum - 1 > record) {
                record = roundNum - 1;
                saveRecord(record);
                cout << colorize("🎉 Новый рекорд: " + to_string(record) + "!", GREEN) << endl;
            } else {
                cout << "Твой результат: " << roundNum - 1 << " раундов" << endl;
            }
            break;
        }

        if (rounds > 0 && roundNum >= rounds) {
            cout << colorize("\n🏆 Поздравляем! Ты прошёл все " + to_string(roundNum) + " раундов!", GREEN) << endl;
            if (roundNum > record) {
                record = roundNum;
                saveRecord(record);
                cout << colorize("🎉 Новый рекорд: " + to_string(record) + "!", GREEN) << endl;
            }
            break;
        }

        cout << colorize("\n✅ Раунд " + to_string(roundNum) + " пройден!", GREEN) << endl;
        roundNum++;
    }
    return 0;
}

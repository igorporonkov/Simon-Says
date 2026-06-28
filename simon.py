# simon.py
#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import sys
import os
import time
import random
import json
import argparse
import subprocess
from datetime import datetime
from pathlib import Path

# ANSI colors
COLORS = {
    'green': '\033[92m',
    'red': '\033[91m',
    'blue': '\033[94m',
    'yellow': '\033[93m',
    'reset': '\033[0m',
    'bold': '\033[1m',
}

def colorize(text, color):
    return f"{COLORS.get(color, '')}{text}{COLORS['reset']}"

# Названия цветов и соответствующие коды для вывода
COLOR_MAP = {
    0: {'name': 'Зелёный', 'color': 'green', 'symbol': '🟢', 'key': '1'},
    1: {'name': 'Красный', 'color': 'red', 'symbol': '🔴', 'key': '2'},
    2: {'name': 'Синий', 'color': 'blue', 'symbol': '🔵', 'key': '3'},
    3: {'name': 'Жёлтый', 'color': 'yellow', 'symbol': '🟡', 'key': '4'},
}

RECORD_FILE = Path.home() / '.simon_record.json'

def load_record():
    if RECORD_FILE.exists():
        with open(RECORD_FILE, 'r') as f:
            data = json.load(f)
            return data.get('record', 0)
    return 0

def save_record(record):
    with open(RECORD_FILE, 'w') as f:
        json.dump({'record': record}, f)

def play_beep(frequency, duration):
    # Простой beep через системную команду (работает на Linux/macOS)
    try:
        subprocess.run(['beep', '-f', str(frequency), '-l', str(duration)],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    except:
        pass

def show_sequence(sequence, speed, round_num, record):
    os.system('clear' if os.name == 'posix' else 'cls')
    print(colorize("=" * 50, 'bold'))
    print(colorize(f"🎮  САЙМОН ГОВОРИТ  |  Раунд {round_num}  |  Рекорд: {record}", 'bold'))
    print(colorize("=" * 50, 'bold'))
    print("\nЗапоминай последовательность цветов:\n")

    for idx, color_idx in enumerate(sequence):
        color_info = COLOR_MAP[color_idx]
        # Показываем цвет
        print(colorize(f"  {color_info['symbol']}  {color_info['name']}", color_info['color']))
        play_beep(440 + color_idx * 100, 200)
        time.sleep(speed / 1000.0)
        # Очищаем последнюю строку
        print("\033[A", end='')
        time.sleep(50 / 1000.0)

    print("\n" + colorize("Твоя очередь!", 'bold'))
    print(f"Нажимай {', '.join([f'{v['key']} ({v['name']})' for v in COLOR_MAP.values()])}")
    print(f"Для выхода нажми 'q'\n")

def get_user_input(sequence, timeout=0):
    user_seq = []
    for i, expected in enumerate(sequence):
        start_time = time.time()
        while True:
            if timeout > 0 and (time.time() - start_time) > timeout:
                print(colorize("⏰ Время вышло!", 'red'))
                return None
            # Читаем один символ без нажатия Enter
            try:
                import termios
                import tty
                fd = sys.stdin.fileno()
                old_settings = termios.tcgetattr(fd)
                tty.setraw(sys.stdin.fileno())
                ch = sys.stdin.read(1)
                termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
            except:
                ch = sys.stdin.read(1)

            if ch == 'q' or ch == 'Q':
                print(colorize("\nВыход из игры.", 'yellow'))
                sys.exit(0)
            # Проверяем соответствие
            for idx, info in COLOR_MAP.items():
                if ch == info['key'] or ch.lower() == info['name'][0].lower():
                    if idx == expected:
                        user_seq.append(idx)
                        print(colorize(f"  {info['symbol']}  Верно!", info['color']))
                        play_beep(880, 100)
                        break
                    else:
                        print(colorize(f"  {info['symbol']}  Ошибка! (ожидался {COLOR_MAP[expected]['name']})", 'red'))
                        play_beep(200, 300)
                        return None
            else:
                print(colorize(f"Неизвестная клавиша: {ch}", 'yellow'))
            break
    return user_seq

def main():
    parser = argparse.ArgumentParser(description="Simon Says – игра на память")
    parser.add_argument('-s', '--speed', type=int, default=800, help='Скорость показа (мс)')
    parser.add_argument('-r', '--rounds', type=int, default=0, help='Количество раундов (0 – бесконечно)')
    parser.add_argument('-t', '--timeout', type=int, default=0, help='Таймаут на ввод (сек)')
    args = parser.parse_args()

    record = load_record()
    print(colorize("🎮  Добро пожаловать в игру САЙМОН ГОВОРИТ!", 'bold'))
    print(f"Твой текущий рекорд: {colorize(str(record), 'green')}")
    print("Нажми любую клавишу для начала...")
    input()

    sequence = []
    round_num = 1
    while True:
        # Добавляем новый цвет
        new_color = random.randint(0, 3)
        sequence.append(new_color)

        # Показываем последовательность
        show_sequence(sequence, args.speed, round_num, record)

        # Ввод игрока
        user_seq = get_user_input(sequence, args.timeout)
        if user_seq is None:
            print(colorize("\n❌ Игра окончена!", 'red'))
            print(f"Правильная последовательность: {', '.join([COLOR_MAP[x]['name'] for x in sequence])}")
            if round_num - 1 > record:
                record = round_num - 1
                save_record(record)
                print(colorize(f"🎉 Новый рекорд: {record}!", 'green'))
            else:
                print(f"Твой результат: {round_num - 1} раундов")
            break

        if args.rounds > 0 and round_num >= args.rounds:
            print(colorize(f"\n🏆 Поздравляем! Ты прошёл все {round_num} раундов!", 'green'))
            if round_num > record:
                record = round_num
                save_record(record)
                print(colorize(f"🎉 Новый рекорд: {record}!", 'green'))
            break

        print(colorize(f"\n✅ Раунд {round_num} пройден!", 'green'))
        round_num += 1

if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print(colorize("\n👋 Игра прервана.", 'yellow'))
        sys.exit(0)

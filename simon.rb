#!/usr/bin/env ruby
# simon.rb
# encoding: UTF-8

require 'json'
require 'fileutils'

COLORS = {
  green: "\e[92m",
  red: "\e[91m",
  blue: "\e[94m",
  yellow: "\e[93m",
  bold: "\e[1m",
  reset: "\e[0m"
}

def colorize(text, color)
  "#{COLORS[color]}#{text}#{COLORS[:reset]}"
end

COLOR_MAP = [
  { name: 'Зелёный', color: :green, symbol: '🟢', key: '1' },
  { name: 'Красный', color: :red, symbol: '🔴', key: '2' },
  { name: 'Синий', color: :blue, symbol: '🔵', key: '3' },
  { name: 'Жёлтый', color: :yellow, symbol: '🟡', key: '4' }
]

RECORD_FILE = File.join(Dir.home, '.simon_record.json')

def load_record
  return 0 unless File.exist?(RECORD_FILE)
  JSON.parse(File.read(RECORD_FILE))['record'] || 0
end

def save_record(record)
  File.write(RECORD_FILE, JSON.pretty_generate({ record: record }))
end

def clear_screen
  system('clear') || system('cls')
end

def play_beep(freq, dur)
  print "\a"  # просто звуковой сигнал
end

def show_sequence(seq, speed, round_num, record)
  clear_screen
  puts colorize('=' * 50, :bold)
  puts colorize("🎮  САЙМОН ГОВОРИТ  |  Раунд #{round_num}  |  Рекорд: #{record}", :bold)
  puts colorize('=' * 50, :bold)
  puts "\nЗапоминай последовательность цветов:\n"

  seq.each do |idx|
    c = COLOR_MAP[idx]
    puts colorize("  #{c[:symbol]}  #{c[:name]}", c[:color])
    play_beep(440 + idx * 100, 200)
    sleep(speed / 1000.0)
    print "\033[A"
    sleep(0.05)
  end

  puts "\n" + colorize("Твоя очередь!", :bold)
  print "Нажимай #{COLOR_MAP.map { |c| "#{c[:key]} (#{c[:name]})" }.join(', ')}"
  puts "\nДля выхода нажми 'q'\n"
end

def get_user_input(seq, timeout)
  require 'io/console'
  seq.each_with_index do |expected, i|
    start_time = Time.now
    while true
      if timeout > 0 && (Time.now - start_time) > timeout
        puts colorize("⏰ Время вышло!", :red)
        return false
      end
      ch = STDIN.getch
      if ch == 'q' || ch == 'Q'
        puts colorize("\nВыход из игры.", :yellow)
        exit 0
      end
      found = false
      COLOR_MAP.each_with_index do |c, j|
        if ch == c[:key]
          found = true
          if j == expected
            puts colorize("  #{c[:symbol]}  Верно!", c[:color])
            play_beep(880, 100)
            break
          else
            puts colorize("  #{c[:symbol]}  Ошибка! (ожидался #{COLOR_MAP[expected][:name]})", :red)
            play_beep(200, 300)
            return false
          end
        end
      end
      puts colorize("Неизвестная клавиша: #{ch}", :yellow) unless found
      break if found
    end
  end
  true
end

def main
  speed = 800
  rounds = 0
  timeout = 0

  i = 0
  while i < ARGV.length
    case ARGV[i]
    when '-s'
      speed = ARGV[i+1].to_i
      i += 2
    when '-r'
      rounds = ARGV[i+1].to_i
      i += 2
    when '-t'
      timeout = ARGV[i+1].to_i
      i += 2
    when '-h', '--help'
      puts "Usage: simon [options]\n  -s <ms>   Speed (default 800)\n  -r <N>    Rounds (0 = infinite)\n  -t <sec>  Input timeout"
      return
    else
      i += 1
    end
  end

  record = load_record
  puts colorize("🎮  Добро пожаловать в игру САЙМОН ГОВОРИТ!", :bold)
  puts "Твой текущий рекорд: #{colorize(record.to_s, :green)}"
  puts "Нажми любую клавишу для начала..."
  STDIN.getch

  seq = []
  round_num = 1
  loop do
    seq << rand(4)
    show_sequence(seq, speed, round_num, record)

    success = get_user_input(seq, timeout)
    unless success
      puts colorize("\n❌ Игра окончена!", :red)
      print "Правильная последовательность: "
      puts seq.map { |x| COLOR_MAP[x][:name] }.join(', ')
      if round_num - 1 > record
        record = round_num - 1
        save_record(record)
        puts colorize("🎉 Новый рекорд: #{record}!", :green)
      else
        puts "Твой результат: #{round_num - 1} раундов"
      end
      break
    end

    if rounds > 0 && round_num >= rounds
      puts colorize("\n🏆 Поздравляем! Ты прошёл все #{round_num} раундов!", :green)
      if round_num > record
        record = round_num
        save_record(record)
        puts colorize("🎉 Новый рекорд: #{record}!", :green)
      end
      break
    end

    puts colorize("\n✅ Раунд #{round_num} пройден!", :green)
    round_num += 1
  end
end

main if __FILE__ == $0

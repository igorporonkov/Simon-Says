// simon.js
#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');
const readline = require('readline');

const COLORS = {
    green: '\x1b[92m',
    red: '\x1b[91m',
    blue: '\x1b[94m',
    yellow: '\x1b[93m',
    reset: '\x1b[0m',
    bold: '\x1b[1m'
};

function colorize(text, color) {
    return COLORS[color] + text + COLORS.reset;
}

const COLOR_MAP = [
    { name: 'Зелёный', color: 'green', symbol: '🟢', key: '1' },
    { name: 'Красный', color: 'red', symbol: '🔴', key: '2' },
    { name: 'Синий', color: 'blue', symbol: '🔵', key: '3' },
    { name: 'Жёлтый', color: 'yellow', symbol: '🟡', key: '4' }
];

const RECORD_FILE = path.join(os.homedir(), '.simon_record.json');

function loadRecord() {
    try {
        const data = JSON.parse(fs.readFileSync(RECORD_FILE, 'utf8'));
        return data.record || 0;
    } catch { return 0; }
}

function saveRecord(record) {
    fs.writeFileSync(RECORD_FILE, JSON.stringify({ record }));
}

function clearScreen() {
    console.clear();
}

function playBeep(freq, dur) {
    // В Node.js можно использовать процесс beep или просто вывести символ
    process.stdout.write('\x07');
}

function showSequence(seq, speed, roundNum, record) {
    clearScreen();
    console.log(colorize('='.repeat(50), 'bold'));
    console.log(colorize(`🎮  САЙМОН ГОВОРИТ  |  Раунд ${roundNum}  |  Рекорд: ${record}`, 'bold'));
    console.log(colorize('='.repeat(50), 'bold'));
    console.log('\nЗапоминай последовательность цветов:\n');

    for (const idx of seq) {
        const c = COLOR_MAP[idx];
        console.log(colorize(`  ${c.symbol}  ${c.name}`, c.color));
        playBeep(440 + idx * 100, 200);
        const start = Date.now();
        while (Date.now() - start < speed) { /* busy wait */ }
        console.log('\033[A');
        const start2 = Date.now();
        while (Date.now() - start2 < 50) { /* busy wait */ }
    }

    console.log('\n' + colorize('Твоя очередь!', 'bold'));
    console.log(`Нажимай ${COLOR_MAP.map(c => `${c.key} (${c.name})`).join(', ')}`);
    console.log("Для выхода нажми 'q'\n");
}

function getUserInput(seq, timeout) {
    const rl = readline.createInterface({
        input: process.stdin,
        output: process.stdout,
        terminal: true
    });

    return new Promise((resolve) => {
        let index = 0;
        const timer = timeout > 0 ? setTimeout(() => {
            rl.close();
            console.log(colorize('⏰ Время вышло!', 'red'));
            resolve(false);
        }, timeout * 1000) : null;

        const onKey = (ch) => {
            if (ch === 'q' || ch === 'Q') {
                console.log(colorize('\nВыход из игры.', 'yellow'));
                process.exit(0);
            }
            for (let j = 0; j < COLOR_MAP.length; j++) {
                if (ch === COLOR_MAP[j].key) {
                    if (j === seq[index]) {
                        console.log(colorize(`  ${COLOR_MAP[j].symbol}  Верно!`, COLOR_MAP[j].color));
                        playBeep(880, 100);
                        index++;
                        if (index === seq.length) {
                            if (timer) clearTimeout(timer);
                            rl.close();
                            resolve(true);
                        }
                    } else {
                        console.log(colorize(`  ${COLOR_MAP[j].symbol}  Ошибка! (ожидался ${COLOR_MAP[seq[index]].name})`, 'red'));
                        playBeep(200, 300);
                        if (timer) clearTimeout(timer);
                        rl.close();
                        resolve(false);
                    }
                    return;
                }
            }
            console.log(colorize(`Неизвестная клавиша: ${ch}`, 'yellow'));
        };

        rl.input.on('keypress', (str, key) => {
            if (key && key.name) {
                onKey(key.name);
            } else if (str) {
                onKey(str);
            }
        });

        // Для совместимости со старыми версиями Node
        process.stdin.setRawMode(true);
        process.stdin.resume();
    });
}

async function main() {
    const args = process.argv.slice(2);
    let speed = 800;
    let rounds = 0;
    let timeout = 0;

    for (let i = 0; i < args.length; i++) {
        if (args[i] === '-s' && i+1 < args.length) {
            speed = parseInt(args[++i]);
        } else if (args[i] === '-r' && i+1 < args.length) {
            rounds = parseInt(args[++i]);
        } else if (args[i] === '-t' && i+1 < args.length) {
            timeout = parseInt(args[++i]);
        } else if (args[i] === '-h' || args[i] === '--help') {
            console.log('Usage: node simon.js [options]\n  -s <ms>   Speed (default 800)\n  -r <N>    Rounds (0 = infinite)\n  -t <sec>  Input timeout');
            process.exit(0);
        }
    }

    const record = loadRecord();
    console.log(colorize('🎮  Добро пожаловать в игру САЙМОН ГОВОРИТ!', 'bold'));
    console.log(`Твой текущий рекорд: ${colorize(String(record), 'green')}`);
    console.log('Нажми любую клавишу для начала...');
    await new Promise(resolve => {
        process.stdin.setRawMode(true);
        process.stdin.resume();
        process.stdin.once('data', () => {
            process.stdin.setRawMode(false);
            resolve();
        });
    });

    const seq = [];
    let roundNum = 1;

    while (true) {
        seq.push(Math.floor(Math.random() * 4));
        showSequence(seq, speed, roundNum, record);

        const success = await getUserInput(seq, timeout);
        if (!success) {
            console.log(colorize('\n❌ Игра окончена!', 'red'));
            console.log(`Правильная последовательность: ${seq.map(i => COLOR_MAP[i].name).join(', ')}`);
            if (roundNum - 1 > record) {
                record = roundNum - 1;
                saveRecord(record);
                console.log(colorize(`🎉 Новый рекорд: ${record}!`, 'green'));
            } else {
                console.log(`Твой результат: ${roundNum - 1} раундов`);
            }
            break;
        }

        if (rounds > 0 && roundNum >= rounds) {
            console.log(colorize(`\n🏆 Поздравляем! Ты прошёл все ${roundNum} раундов!`, 'green'));
            if (roundNum > record) {
                record = roundNum;
                saveRecord(record);
                console.log(colorize(`🎉 Новый рекорд: ${record}!`, 'green'));
            }
            break;
        }

        console.log(colorize(`\n✅ Раунд ${roundNum} пройден!`, 'green'));
        roundNum++;
    }
}

main().catch(err => {
    console.error(err);
    process.exit(1);
});

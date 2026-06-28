// simon.go
package main

import (
	"encoding/json"
	"fmt"
	"math/rand"
	"os"
	"os/exec"
	"runtime"
	"strconv"
	"time"
)

const (
	reset  = "\033[0m"
	green  = "\033[92m"
	red    = "\033[91m"
	blue   = "\033[94m"
	yellow = "\033[93m"
	bold   = "\033[1m"
)

func colorize(text, color string) string {
	return color + text + reset
}

type ColorInfo struct {
	Name   string
	Color  string
	Symbol string
	Key    rune
}

var colors = []ColorInfo{
	{"Зелёный", green, "🟢", '1'},
	{"Красный", red, "🔴", '2'},
	{"Синий", blue, "🔵", '3'},
	{"Жёлтый", yellow, "🟡", '4'},
}

func getHomeDir() string {
	home, _ := os.UserHomeDir()
	return home
}

func getRecordFile() string {
	return getHomeDir() + "/.simon_record.json"
}

type RecordData struct {
	Record int `json:"record"`
}

func loadRecord() int {
	data, err := os.ReadFile(getRecordFile())
	if err != nil {
		return 0
	}
	var r RecordData
	json.Unmarshal(data, &r)
	return r.Record
}

func saveRecord(record int) {
	data, _ := json.Marshal(RecordData{Record: record})
	os.WriteFile(getRecordFile(), data, 0644)
}

func clearScreen() {
	cmd := exec.Command("clear")
	if runtime.GOOS == "windows" {
		cmd = exec.Command("cmd", "/c", "cls")
	}
	cmd.Stdout = os.Stdout
	cmd.Run()
}

func playBeep(freq, dur int) {
	// Для простоты используем fmt.Print с ASCII Bell
	fmt.Print("\a")
}

func showSequence(seq []int, speed int, roundNum, record int) {
	clearScreen()
	fmt.Println(colorize("==================================================", bold))
	fmt.Printf(colorize("🎮  САЙМОН ГОВОРИТ  |  Раунд %d  |  Рекорд: %d\n", bold), roundNum, record)
	fmt.Println(colorize("==================================================", bold))
	fmt.Println("\nЗапоминай последовательность цветов:\n")

	for _, idx := range seq {
		c := colors[idx]
		fmt.Println(colorize("  "+c.Symbol+"  "+c.Name, c.Color))
		playBeep(440+idx*100, 200)
		time.Sleep(time.Duration(speed) * time.Millisecond)
		fmt.Print("\033[A")
		time.Sleep(50 * time.Millisecond)
	}

	fmt.Println("\n" + colorize("Твоя очередь!", bold))
	fmt.Print("Нажимай ")
	for _, c := range colors {
		fmt.Printf("%c (%s) ", c.Key, c.Name)
	}
	fmt.Println("\nДля выхода нажми 'q'\n")
}

func getUserInput(seq []int, timeout int) bool {
	var b []byte = make([]byte, 1)
	for i := 0; i < len(seq); i++ {
		start := time.Now()
		for {
			if timeout > 0 && time.Since(start).Seconds() > float64(timeout) {
				fmt.Println(colorize("⏰ Время вышло!", red))
				return false
			}
			// Неблокирующий ввод в Go сложен, используем простой подход
			os.Stdin.Read(b)
			ch := rune(b[0])
			if ch == 'q' || ch == 'Q' {
				fmt.Println(colorize("\nВыход из игры.", yellow))
				os.Exit(0)
			}
			for j := 0; j < 4; j++ {
				if ch == colors[j].Key {
					if j == seq[i] {
						fmt.Println(colorize("  "+colors[j].Symbol+"  Верно!", colors[j].Color))
						playBeep(880, 100)
						break
					} else {
						fmt.Println(colorize("  "+colors[j].Symbol+"  Ошибка! (ожидался "+colors[seq[i]].Name+")", red))
						playBeep(200, 300)
						return false
					}
				}
			}
			break
		}
	}
	return true
}

func main() {
	speed := 800
	rounds := 0
	timeout := 0

	for i := 1; i < len(os.Args); i++ {
		arg := os.Args[i]
		if arg == "-s" && i+1 < len(os.Args) {
			speed, _ = strconv.Atoi(os.Args[i+1])
			i++
		} else if arg == "-r" && i+1 < len(os.Args) {
			rounds, _ = strconv.Atoi(os.Args[i+1])
			i++
		} else if arg == "-t" && i+1 < len(os.Args) {
			timeout, _ = strconv.Atoi(os.Args[i+1])
			i++
		} else if arg == "-h" || arg == "--help" {
			fmt.Println("Usage: simon [options]\n  -s <ms>   Speed (default 800)\n  -r <N>    Rounds (0 = infinite)\n  -t <sec>  Input timeout")
			return
		}
	}

	rand.Seed(time.Now().UnixNano())
	record := loadRecord()
	fmt.Println(colorize("🎮  Добро пожаловать в игру САЙМОН ГОВОРИТ!", bold))
	fmt.Printf("Твой текущий рекорд: %s\n", colorize(strconv.Itoa(record), green))
	fmt.Println("Нажми любую клавишу для начала...")
	var dummy string
	fmt.Scanln(&dummy)

	seq := []int{}
	roundNum := 1
	for {
		seq = append(seq, rand.Intn(4))
		showSequence(seq, speed, roundNum, record)

		success := getUserInput(seq, timeout)
		if !success {
			fmt.Println(colorize("\n❌ Игра окончена!", red))
			fmt.Print("Правильная последовательность: ")
			for _, x := range seq {
				fmt.Print(colors[x].Name + " ")
			}
			fmt.Println()
			if roundNum-1 > record {
				record = roundNum - 1
				saveRecord(record)
				fmt.Println(colorize("🎉 Новый рекорд: "+strconv.Itoa(record)+"!", green))
			} else {
				fmt.Printf("Твой результат: %d раундов\n", roundNum-1)
			}
			break
		}

		if rounds > 0 && roundNum >= rounds {
			fmt.Println(colorize("\n🏆 Поздравляем! Ты прошёл все "+strconv.Itoa(roundNum)+" раундов!", green))
			if roundNum > record {
				record = roundNum
				saveRecord(record)
				fmt.Println(colorize("🎉 Новый рекорд: "+strconv.Itoa(record)+"!", green))
			}
			break
		}

		fmt.Println(colorize("\n✅ Раунд "+strconv.Itoa(roundNum)+" пройден!", green))
		roundNum++
	}
}

// simon.java
import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;
import com.google.gson.*;

public class simon {
    private static final String RESET = "\u001B[0m";
    private static final String GREEN = "\u001B[92m";
    private static final String RED = "\u001B[91m";
    private static final String BLUE = "\u001B[94m";
    private static final String YELLOW = "\u001B[93m";
    private static final String BOLD = "\u001B[1m";

    private static String colorize(String text, String color) {
        return color + text + RESET;
    }

    static class ColorInfo {
        String name;
        String color;
        String symbol;
        char key;
    }

    static List<ColorInfo> colors = Arrays.asList(
        new ColorInfo() {{ name = "Зелёный"; color = GREEN; symbol = "🟢"; key = '1'; }},
        new ColorInfo() {{ name = "Красный"; color = RED; symbol = "🔴"; key = '2'; }},
        new ColorInfo() {{ name = "Синий"; color = BLUE; symbol = "🔵"; key = '3'; }},
        new ColorInfo() {{ name = "Жёлтый"; color = YELLOW; symbol = "🟡"; key = '4'; }}
    );

    private static String configFile = System.getProperty("user.home") + "/.simon_record.json";

    private static int loadRecord() throws IOException {
        Path path = Paths.get(configFile);
        if (!Files.exists(path)) return 0;
        String json = new String(Files.readAllBytes(path));
        Gson gson = new Gson();
        JsonObject obj = gson.fromJson(json, JsonObject.class);
        return obj.get("record").getAsInt();
    }

    private static void saveRecord(int record) throws IOException {
        Gson gson = new GsonBuilder().setPrettyPrinting().create();
        JsonObject obj = new JsonObject();
        obj.addProperty("record", record);
        Files.write(Paths.get(configFile), gson.toJson(obj).getBytes());
    }

    private static void clearScreen() {
        System.out.print("\033[H\033[2J");
        System.out.flush();
    }

    private static void playBeep(int freq, int dur) {
        System.out.print("\007");
    }

    private static void showSequence(List<Integer> seq, int speed, int roundNum, int record) {
        clearScreen();
        System.out.println(colorize("=".repeat(50), BOLD));
        System.out.println(colorize("🎮  САЙМОН ГОВОРИТ  |  Раунд " + roundNum + "  |  Рекорд: " + record, BOLD));
        System.out.println(colorize("=".repeat(50), BOLD));
        System.out.println("\nЗапоминай последовательность цветов:\n");

        for (int idx : seq) {
            ColorInfo c = colors.get(idx);
            System.out.println(colorize("  " + c.symbol + "  " + c.name, c.color));
            playBeep(440 + idx * 100, 200);
            try { Thread.sleep(speed); } catch (InterruptedException e) {}
            System.out.print("\033[A");
            try { Thread.sleep(50); } catch (InterruptedException e) {}
        }

        System.out.println("\n" + colorize("Твоя очередь!", BOLD));
        System.out.print("Нажимай ");
        for (ColorInfo c : colors) System.out.print(c.key + " (" + c.name + ") ");
        System.out.println("\nДля выхода нажми 'q'\n");
    }

    private static boolean getUserInput(List<Integer> seq, int timeout) throws IOException, InterruptedException {
        for (int i = 0; i < seq.size(); i++) {
            long start = System.currentTimeMillis();
            while (true) {
                if (timeout > 0 && (System.currentTimeMillis() - start) > timeout * 1000) {
                    System.out.println(colorize("⏰ Время вышло!", RED));
                    return false;
                }
                if (System.in.available() > 0) {
                    char ch = (char) System.in.read();
                    if (ch == 'q' || ch == 'Q') {
                        System.out.println(colorize("\nВыход из игры.", YELLOW));
                        System.exit(0);
                    }
                    for (int j = 0; j < colors.size(); j++) {
                        if (ch == colors.get(j).key) {
                            if (j == seq.get(i)) {
                                System.out.println(colorize("  " + colors.get(j).symbol + "  Верно!", colors.get(j).color));
                                playBeep(880, 100);
                                break;
                            } else {
                                System.out.println(colorize("  " + colors.get(j).symbol + "  Ошибка! (ожидался " + colors.get(seq.get(i)).name + ")", RED));
                                playBeep(200, 300);
                                return false;
                            }
                        }
                    }
                    break;
                }
                Thread.sleep(10);
            }
        }
        return true;
    }

    public static void main(String[] args) throws IOException, InterruptedException {
        int speed = 800, rounds = 0, timeout = 0;
        for (int i = 0; i < args.length; i++) {
            if (args[i].equals("-s") && i+1 < args.length) speed = Integer.parseInt(args[++i]);
            else if (args[i].equals("-r") && i+1 < args.length) rounds = Integer.parseInt(args[++i]);
            else if (args[i].equals("-t") && i+1 < args.length) timeout = Integer.parseInt(args[++i]);
            else if (args[i].equals("-h") || args[i].equals("--help")) {
                System.out.println("Usage: simon [options]\n  -s <ms>   Speed (default 800)\n  -r <N>    Rounds (0 = infinite)\n  -t <sec>  Input timeout");
                return;
            }
        }

        int record = loadRecord();
        System.out.println(colorize("🎮  Добро пожаловать в игру САЙМОН ГОВОРИТ!", BOLD));
        System.out.println("Твой текущий рекорд: " + colorize(String.valueOf(record), GREEN));
        System.out.println("Нажми любую клавишу для начала...");
        System.in.read();

        List<Integer> seq = new ArrayList<>();
        int roundNum = 1;
        Random rand = new Random();

        while (true) {
            seq.add(rand.nextInt(4));
            showSequence(seq, speed, roundNum, record);

            boolean success = getUserInput(seq, timeout);
            if (!success) {
                System.out.println(colorize("\n❌ Игра окончена!", RED));
                System.out.print("Правильная последовательность: ");
                for (int x : seq) System.out.print(colors.get(x).name + " ");
                System.out.println();
                if (roundNum - 1 > record) {
                    record = roundNum - 1;
                    saveRecord(record);
                    System.out.println(colorize("🎉 Новый рекорд: " + record + "!", GREEN));
                } else {
                    System.out.println("Твой результат: " + (roundNum - 1) + " раундов");
                }
                break;
            }

            if (rounds > 0 && roundNum >= rounds) {
                System.out.println(colorize("\n🏆 Поздравляем! Ты прошёл все " + roundNum + " раундов!", GREEN));
                if (roundNum > record) {
                    record = roundNum;
                    saveRecord(record);
                    System.out.println(colorize("🎉 Новый рекорд: " + record + "!", GREEN));
                }
                break;
            }

            System.out.println(colorize("\n✅ Раунд " + roundNum + " пройден!", GREEN));
            roundNum++;
        }
    }
}

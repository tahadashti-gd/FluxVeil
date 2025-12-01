using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography; // Added for AES

namespace FluxVeil
{
    class Program
    {
        private const string Terminator = "[FLUX_END]";

        static void Main(string[] args)
        {
            if (args.Length == 0) RunInteractiveMode();
            else ProcessCommand(args);
        }

        static void RunInteractiveMode()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  ______ _            _    _      _ _ 
 |  ____| |          | |  | |    (_) |
 | |__  | |_   ___  _| |__| | ___ _| |
 |  __| | | | | \ \/ /  __  |/ _ \ | |
 | |    | | |_| |>  <| |  | |  __/ | |
 |_|    |_|\__,_/_/\_\_|  |_|\___|_|_|
            ");
            Console.WriteLine("FluxVeil - Encrypted Steganography");
            Console.WriteLine("Type 'help' for commands.");
            Console.ResetColor();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("\nFluxVeil > ");
                Console.ResetColor();

                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                string[] args = ParseArguments(input);
                if (args[0].ToLower() == "exit") break;
                if (args[0].ToLower() == "cls") { Console.Clear(); continue; }

                ProcessCommand(args);
            }
        }

        static void ProcessCommand(string[] args)
        {
            string command = args[0].ToLower();
            try
            {
                // --- ENCRYPTED TEXT COMMANDS ---
                if (command == "hide" && args.Length >= 5)
                {
                    // usage: hide input "text" output password
                    string inputPath = args[1];
                    string text = args[2];
                    string outputPath = EnsurePngExtension(args[3]);
                    string password = args[4];

                    if (!File.Exists(inputPath)) { PrintError("Input file not found."); return; }

                    Console.Write("Encrypting payload with AES... ");
                    string encryptedText = AesOperation.EncryptString(text, password);

                    Console.Write("Embedding data... ");
                    HideText(inputPath, encryptedText, outputPath);
                }
                else if (command == "reveal" && args.Length >= 3)
                {
                    // usage: reveal input password
                    string inputPath = args[1];
                    string password = args[2];

                    if (!File.Exists(inputPath)) { PrintError("File not found."); return; }

                    Console.Write("Extracting data... ");
                    string extractedCipher = RevealText(inputPath);

                    if (extractedCipher.StartsWith("Error")) { PrintError(extractedCipher); return; }

                    Console.Write("Decrypting with password... ");
                    try
                    {
                        string plainText = AesOperation.DecryptString(extractedCipher, password);
                        PrintSuccess($"\n[SECRET MESSAGE]: {plainText}");
                    }
                    catch
                    {
                        PrintError("\nFailed! Wrong password or corrupted data.");
                    }
                }
                // --- IMAGE COMMANDS (Stealth Mode) ---
                else if (command == "hide-img" && args.Length >= 4)
                {
                    HideImage(args[1], args[2], EnsurePngExtension(args[3]));
                }
                else if (command == "reveal-img" && args.Length >= 3)
                {
                    RevealImage(args[1], EnsurePngExtension(args[2]));
                }
                else if (command == "help")
                {
                    ShowHelp();
                }
                else
                {
                    PrintError("Invalid command. Check help.");
                }
            }
            catch (Exception ex) { PrintError($"System Error: {ex.Message}"); }
        }

        // --- CORE LOGIC: TEXT ---
        static void HideText(string imagePath, string text, string outputPath)
        {
            text += Terminator;
            using (Bitmap bmp = new Bitmap(imagePath))
            {
                string binaryText = TextToBinary(text);
                if (binaryText.Length > bmp.Width * bmp.Height) throw new Exception("Text too long.");

                int charIndex = 0;
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        if (charIndex < binaryText.Length)
                        {
                            Color pixel = bmp.GetPixel(i, j);
                            int bit = binaryText[charIndex] == '1' ? 1 : 0;
                            int newR = (pixel.R & 254) | bit;
                            bmp.SetPixel(i, j, Color.FromArgb(newR, pixel.G, pixel.B));
                            charIndex++;
                        }
                        else
                        {
                            bmp.Save(outputPath, ImageFormat.Png);
                            PrintSuccess($"Secured image saved to: {outputPath}");
                            return;
                        }
                    }
                }
            }
        }

        static string RevealText(string imagePath)
        {
            using (Bitmap bmp = new Bitmap(imagePath))
            {
                StringBuilder binaryData = new StringBuilder();
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        Color pixel = bmp.GetPixel(i, j);
                        binaryData.Append(pixel.R & 1);
                    }
                }
                string extracted = BinaryToText(binaryData.ToString());
                int terminatorIndex = extracted.IndexOf(Terminator);
                return terminatorIndex != -1 ? extracted.Substring(0, terminatorIndex) : "Error: No FluxVeil signature found.";
            }
        }

        // --- CORE LOGIC: IMAGE (2-BIT STEALTH) ---
        static void HideImage(string hostPath, string secretPath, string outputPath)
        {
            using (Bitmap hostBmp = new Bitmap(hostPath))
            using (Bitmap secretSource = new Bitmap(secretPath))
            using (Bitmap secretBmp = new Bitmap(secretSource, hostBmp.Width, hostBmp.Height))
            {
                for (int i = 0; i < hostBmp.Width; i++)
                {
                    for (int j = 0; j < hostBmp.Height; j++)
                    {
                        Color h = hostBmp.GetPixel(i, j);
                        Color s = secretBmp.GetPixel(i, j);
                        // 2-Bit Injection
                        hostBmp.SetPixel(i, j, Color.FromArgb(
                            (h.R & 0xFC) | ((s.R & 0xC0) >> 6),
                            (h.G & 0xFC) | ((s.G & 0xC0) >> 6),
                            (h.B & 0xFC) | ((s.B & 0xC0) >> 6)));
                    }
                }
                hostBmp.Save(outputPath, ImageFormat.Png);
                PrintSuccess($"Image hidden inside {outputPath} (Stealth Mode)");
            }
        }

        static void RevealImage(string encodedPath, string outputPath)
        {
            using (Bitmap encoded = new Bitmap(encodedPath))
            using (Bitmap output = new Bitmap(encoded.Width, encoded.Height))
            {
                for (int i = 0; i < encoded.Width; i++)
                {
                    for (int j = 0; j < encoded.Height; j++)
                    {
                        Color p = encoded.GetPixel(i, j);
                        // Extract 2 LSB and shift back
                        output.SetPixel(i, j, Color.FromArgb(
                            (p.R & 0x03) << 6, (p.G & 0x03) << 6, (p.B & 0x03) << 6));
                    }
                }
                output.Save(outputPath, ImageFormat.Png);
                PrintSuccess($"Image extracted to {outputPath}");
            }
        }

        // --- HELPERS ---
        static string EnsurePngExtension(string path) => path.ToLower().EndsWith(".png") ? path : Path.ChangeExtension(path, ".png");

        static string[] ParseArguments(string commandLine)
        {
            var result = new List<string>();
            bool inQuotes = false;
            StringBuilder currentArg = new StringBuilder();
            foreach (char c in commandLine)
            {
                if (c == '\"') inQuotes = !inQuotes;
                else if (c == ' ' && !inQuotes) { if (currentArg.Length > 0) { result.Add(currentArg.ToString()); currentArg.Clear(); } }
                else currentArg.Append(c);
            }
            if (currentArg.Length > 0) result.Add(currentArg.ToString());
            return result.ToArray();
        }

        static string TextToBinary(string data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in data.ToCharArray()) sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            return sb.ToString();
        }

        static string BinaryToText(string data)
        {
            List<byte> byteList = new List<byte>();
            for (int i = 0; i < data.Length; i += 8)
            {
                if (i + 8 > data.Length) break;
                try { byteList.Add(Convert.ToByte(data.Substring(i, 8), 2)); } catch { break; }
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }

        static void ShowHelp()
        {
            Console.WriteLine("\nCommands:");
            Console.WriteLine("  hide <img_in> \"<text>\" <img_out> <password>  : Encrypt & Hide Text");
            Console.WriteLine("  reveal <img_in> <password>                   : Decrypt & Read Text");
            Console.WriteLine("  hide-img <host> <secret> <out>               : Hide Image (No pass)");
            Console.WriteLine("  reveal-img <img_in> <img_out>                : Reveal Image");
            Console.WriteLine("  cls / exit");
        }
        static void PrintError(string msg) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(msg); Console.ResetColor(); }
        static void PrintSuccess(string msg) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(msg); Console.ResetColor(); }
    }

    // --- NEW AES ENCRYPTION CLASS ---
    public static class AesOperation
    {
        public static string EncryptString(string plainText, string password)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                // Derive a key from the password (SHA256 ensures 32 bytes for AES-256)
                using (SHA256 sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                }
                aes.IV = iv; // Using zero IV for simplicity in CLI (In prod, use random IV)

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                        array = memoryStream.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(array);
        }

        public static string DecryptString(string cipherText, string password)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                }
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
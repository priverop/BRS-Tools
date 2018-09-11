namespace BRSTools
{
    using System;
    using System.IO;

    public class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length > 3 && args.Length < 2)
            {
                Console.Write("Wrong arguments.");
                showUsage();
            } 
            else
            {
                Packaging p = new Packaging();
                TextManager tm = new TextManager();

                switch (args[0])
                {
                    case "-extractText":
                        tm.LoadFile(args[1]);
                        tm.ExportPO();
                        break;

                    case "-insertText":
                        tm.LoadFile(args[1]);
                        tm.ImportPO(args[2]);
                        tm.Export();
                        break;

                    case "-unpack":
                        p.Unpack(args[1]);
                        break;

                    case "-pack":
                            p.Repack(args[1], args[2]);
                        break;

                    default:
                        Console.WriteLine("Wrong arguments.");
                        showUsage();
                        break;
                }
            }
        }

        private static void showUsage(){
            Console.WriteLine("Usage: BRS-Tools.exe -unpack <fileToUnpack>");
            Console.WriteLine("Usage: BRS-Tools.exe -pack <newFileName> <folderName>");
            Console.WriteLine("Usage: BRS-Tools.exe -extractText <fileToExtract>");
            Console.WriteLine("Usage: BRS-Tools.exe -insertText <originalFile> <po>");
        }

        private static void showCredits(){
            Console.WriteLine("=========================");
            Console.WriteLine("== BRS UNPACKER by Nex ==");
            Console.WriteLine("=========================");
        }
    }
}

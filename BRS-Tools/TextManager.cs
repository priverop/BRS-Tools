namespace BRSTools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Yarhl.IO;
    using Yarhl.Media.Text;
    using Yarhl.FileFormat;

    public class TextManager
    {
        // Original File
        public string FileName { get; set; }
        public int MagID { get; set; }
        public int FileSize { get; set; }
        public int PointerNumber { get; set; }
        public int Unknown { get; set; }
        public List<int> Pointers { get; }
        public List<string> Text { get; }
        public const Byte TEXTSEPARATOR = 00;
        public const int FOOTERSIZE = 12; // TESTEAR

        public Dictionary<string, string> Buttons { get; }

        // Imported TXT
        private List<int> newPointers;
        private List<string> newText;

        // Buttons
        public const string CROSS = "b(01)";
        public const string SQUARE = "b(03)";

        public TextManager(){
            this.Pointers = new List<int>();
            this.Text = new List<string>();
            this.Buttons = new Dictionary<string, string>();
            this.Buttons.Add(CROSS, "<CROSSBUTTON>");
            this.Buttons.Add(SQUARE, "<SQUAREBUTTON>");
        }

        public void loadFile(string fileToExtractName)
        {
            using (DataStream fileToExtractStream = new DataStream(fileToExtractName, FileOpenMode.Read)){
                DataReader fileToExtractReader = new DataReader(fileToExtractStream);

                this.FileName = fileToExtractName;
                this.MagID = fileToExtractReader.ReadInt32();
                this.FileSize = fileToExtractReader.ReadInt32();
                this.PointerNumber = fileToExtractReader.ReadInt32();
                this.Unknown = fileToExtractReader.ReadInt32();

                long currentPosition = fileToExtractStream.Position;
                int firstPointer = fileToExtractReader.ReadInt32();
                long pointerTableSize = firstPointer - currentPosition;
                fileToExtractStream.Position = currentPosition;

                while(fileToExtractStream.Position != firstPointer)
                {
                    this.Pointers.Add(fileToExtractReader.ReadInt32());
                }

                int lastPointer = this.Pointers[this.Pointers.Count - 1];

                while(fileToExtractStream.Position != (this.FileSize - FOOTERSIZE))
                {
                    this.Text.Add(fileToExtractReader.ReadString());
                }
            }
        }

        public void exportPO(){

            Po poExport = new Po
            {
                Header = new PoHeader("Black Rock Shooter The Game", "TraduSquare.es", "es")
                {
                    LanguageTeam = "TraduSquare",
                }
            };

            for (int i = 0; i < this.Text.Count; i++)
            {
                string sentence = this.Text[i];
                if (string.IsNullOrEmpty(sentence))
                    sentence = "<!empty>";
                poExport.Add(new PoEntry(this.CleanButtons(sentence)) { Context = i.ToString() });
            }

            poExport.ConvertTo<BinaryFormat>().Stream.WriteTo(this.FileName + ".po");
        }


        private string CleanButtons(string sentence){

            return sentence.Replace(@"\", "")
                           .Replace(CROSS, this.Buttons[CROSS])
                           .Replace(SQUARE, this.Buttons[SQUARE]);
        }
    }
}
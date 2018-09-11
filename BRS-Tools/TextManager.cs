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
        public int NewFileSize { get; set; }

        // Buttons
        public const string CROSS = "b(01)";
        public const string SQUARE = "b(03)";

        public TextManager()
        {
            this.Pointers = new List<int>();
            this.Text = new List<string>();
            this.Buttons = new Dictionary<string, string>();
            this.Buttons.Add(CROSS, "<CROSSBUTTON>");
            this.Buttons.Add(SQUARE, "<SQUAREBUTTON>");
        }

        public void LoadFile(string fileToExtractName)
        {
            using (DataStream fileToExtractStream = new DataStream(fileToExtractName, FileOpenMode.Read))
            {
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

                while (fileToExtractStream.Position != firstPointer)
                {
                    int pointer = fileToExtractReader.ReadInt32();
                    this.Pointers.Add(pointer);
                    fileToExtractStream.PushCurrentPosition();

                    fileToExtractStream.RunInPosition(
                        () => this.Text.Add(fileToExtractReader.ReadString()),
                        pointer);

                    fileToExtractStream.PopPosition();
                }
            }
        }

        public void ExportPO()
        {

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
                poExport.Add(new PoEntry(this.RemoveButtons(sentence)) { Context = i.ToString() });
            }

            poExport.ConvertTo<BinaryFormat>().Stream.WriteTo(this.FileName + ".po");
        }

        public void ImportPO(string poFileName)
        {

            DataStream inputPO = new DataStream(poFileName, FileOpenMode.Read);
            BinaryFormat binaryFile = new BinaryFormat(inputPO);
            Po newPO = binaryFile.ConvertTo<Po>();
            inputPO.Dispose();

            this.newPointers = new List<int>();
            this.newText = new List<string>();

            NewFileSize = this.GetHeaderSize();

            int pointer = Pointers[0];
            this.newPointers.Add(pointer);

            NewFileSize += sizeof(int);

            foreach (var entry in newPO.Entries)
            {
                string sentence = string.IsNullOrEmpty(entry.Translated) ?
                    entry.Original : entry.Translated;
                if (sentence == "<!empty>")
                    sentence = string.Empty;
                sentence = AddButtons(sentence);
                this.newText.Add(sentence);

                NewFileSize += sentence.Length + 1;

                // Last entry doesn't recalc pointer
                if (entry.Context != (newPO.Entries.Count-1).ToString()){
                    pointer += sentence.Length + 1; // After every string there is an extra 00 byte

                    this.newPointers.Add(pointer);
                    NewFileSize += sizeof(int);
                }
            }

        }

        public void Export()
        {
            using(DataStream exportedFileStream = new DataStream(this.FileName + "_new", FileOpenMode.Write)){
                DataWriter exportedFileWriter = new DataWriter(exportedFileStream);

                exportedFileWriter.Write(this.MagID);
                long fileSizePosition = exportedFileStream.Position;
                exportedFileWriter.Write(this.NewFileSize);
                exportedFileWriter.Write(this.newPointers.Count);
                exportedFileWriter.Write(this.Unknown);

                foreach(var pointer in this.newPointers){
                    exportedFileWriter.Write(pointer);
                }

                foreach (var sentence in this.newText)
                {
                    exportedFileWriter.Write(sentence);
                }
                long currentPosition = exportedFileStream.Position;
                exportedFileWriter.WritePadding(00, 0x10, false);
                long endPosition = exportedFileStream.Position;

                NewFileSize += (int)(endPosition - currentPosition);
                exportedFileStream.Position = fileSizePosition;
                exportedFileWriter.Write(this.NewFileSize);

            }
        }

        private string RemoveButtons(string sentence)
        {

            return sentence.Replace(@"\", "")
                           .Replace(CROSS, this.Buttons[CROSS])
                           .Replace(SQUARE, this.Buttons[SQUARE]);
        }

        private string AddButtons(string sentence)
        {
            return sentence.Replace(this.Buttons[CROSS], CROSS)
                           .Replace(this.Buttons[SQUARE], SQUARE)
                           .Replace("b", @"\b");

        }

        private int GetHeaderSize(){
            return sizeof(int) * 4; //MagID + FileSize + NumPointers + Unknown
        }
    }
}
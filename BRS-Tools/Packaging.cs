namespace BRSTools
{
    using System;
    using System.IO;
    using Yarhl.IO;

    public class Packaging
    {
        private const string HEADER_FILENAME = "0 - Header";
        private const string BLOCK_FILENAME = " - Block";
        private string UNPACK_FOLDER = "UNPACK";

        public void Unpack(string fileToExtractName)
        {

            /*
             * Almaceno el Magid.
             * Almaceno el numero de bloques.
             * Bucle de 1 hasta numero de bloques:
             *      Guardo 4bytes puntero
             *      Guardo 4bytes puntero+1
             *      Saco la diferencia = tamaño
             *      Avanzo hasta el offset
             *      Guardo hasta el tamaño en fichero nuevo
             */
            using (DataStream fileToUnpackStream = new DataStream(fileToExtractName, FileOpenMode.Read))
            {

                DataReader fileToUnpackReader = new DataReader(fileToUnpackStream);

                // Create folder to save unpacked files
                Directory.CreateDirectory(this.getUnpackDirName());

                DataStream headerStream = new DataStream(this.getUnpackDirNameSlash() + HEADER_FILENAME, FileOpenMode.Write);
                DataWriter writerHeader = new DataWriter(headerStream);

                uint magid = fileToUnpackReader.ReadUInt32();
                ulong numberOfBlocks = fileToUnpackReader.ReadUInt64();

                writerHeader.Write(magid);
                writerHeader.Write(numberOfBlocks);

                // Save the current position
                long currentPosition = fileToUnpackStream.Position;

                // Read first block pointer (where the pointer table ends)
                uint firstBlockPointer = fileToUnpackReader.ReadUInt32();

                // Return to previous position
                fileToUnpackStream.Position = currentPosition;

                long pointerTableSize = firstBlockPointer - currentPosition;

                // Save pointer table
                writerHeader.Write(fileToUnpackReader.ReadBytes((int)pointerTableSize));
                headerStream.Dispose();

                // Return to previous position
                fileToUnpackStream.Position = currentPosition;

                // The loop will finish when we reach the first non-block pointer byte
                long endBlocksPosition = currentPosition + (((int)numberOfBlocks) * sizeof(int));
                fileToUnpackStream.Position = endBlocksPosition;
                uint endBlocks = fileToUnpackReader.ReadUInt32();
                fileToUnpackStream.Position = currentPosition;

                uint blockPointer;
                uint nextBlockPointer = 0x00;
                uint fileSize = (uint)fileToUnpackStream.Length;
                uint i = 1;

                while (nextBlockPointer != fileSize)
                {
                    // First iteration
                    if (nextBlockPointer == 0x00)
                        blockPointer = fileToUnpackReader.ReadUInt32();
                    else
                        blockPointer = nextBlockPointer;

                    nextBlockPointer = fileToUnpackReader.ReadUInt32();

                    // Last iteration
                    if (nextBlockPointer == endBlocks)

                        // Read block until the end of the file
                        nextBlockPointer = fileSize;

                    uint blockSize = nextBlockPointer - blockPointer;

                    // Save block
                    DataStream blockStream = new DataStream(this.getUnpackDirNameSlash() +  i + BLOCK_FILENAME, FileOpenMode.Write);
                    DataWriter blockWriter = new DataWriter(blockStream);
                    fileToUnpackStream.RunInPosition(
                        () => blockWriter.Write(fileToUnpackReader.ReadBytes((int)blockSize)),
                        blockPointer);
                    i++;

                }
            }
        }


        public void Repack(string newFileName){

            if(File.Exists(this.getUnpackDirNameSlash() + newFileName)){
                Console.WriteLine("File " + newFileName + " already exists. It will be overwritten.");
                File.Delete(this.getUnpackDirNameSlash() + newFileName);
            }

            string[] files = Directory.GetFiles(this.getUnpackDirName());

            DataWriter packedFileWriter = new DataWriter(new DataStream(this.getUnpackDirNameSlash() +
                                                           newFileName, FileOpenMode.Write));

            foreach (string fileName in files){

                if (!char.IsPunctuation(removeDir(fileName)[0])){

                    DataReader fileToPackReader = new DataReader(new DataStream(fileName, FileOpenMode.Read));

                    packedFileWriter.Write(fileToPackReader.ReadBytes((int)fileToPackReader.Stream.Length));
                    fileToPackReader.Stream.Dispose();
                }


            }

        }

        public string getUnpackDirName(){
            return this.UNPACK_FOLDER;
        }

        public string getUnpackDirNameSlash()
        {
            return this.UNPACK_FOLDER + "/";
        }

        private string removeDir(string fileName){
            return fileName.Remove(0, getUnpackDirName().Length+1); // +1 folder separator
        }

    }
}

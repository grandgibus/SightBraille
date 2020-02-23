using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static SightBraille.SightBrailleApp;

namespace SightBraille
{
    public class Document
    {

        public SightBrailleApp app;

        public bool IsSaved = false;
        public string FilePath = null;

        public List<BrailleCharacter>[] Characters = new List<BrailleCharacter>[CHARACTER_ROWS];

        public Document(SightBrailleApp app)
        {
            this.app = app;
        }

        public void DocumentChanged()
        {
            this.IsSaved = false;
            app.ChangeEditorWindowTitleStatus(FilePath != null ? FilePath + " (modifié)" : "Nouveau fichier (modifié)");
        }

        public void DocumentUnchanged()
        {
            this.IsSaved = true;
            app.ChangeEditorWindowTitleStatus(FilePath != null ? FilePath : "Nouveau fichier");
        }

        public void UpdateDocument(List<BrailleCharacter>[] data)
        {
            this.Characters = data;
        }

        public void SaveDocument(List<BrailleCharacter>[] data)
        {
            this.Characters = data;
            if(FilePath == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Fichier braille (*.braille)|*.braille|Tous les fichiers (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == true)
                {
                    this.FilePath = saveFileDialog.FileName;
                }
                else
                {
                    return;
                }
            }

            try
            {
                Stream stream = File.Open(FilePath, FileMode.Create);

                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, Characters);

                DocumentUnchanged();
            }catch(Exception e)
            {
                Console.WriteLine("Error happened " + e.Message);
            }
        }

        private bool CheckConfirmation()
        {
            if (!this.IsSaved)
            {
                MessageBoxResult result = MessageBox.Show("Les modifications apportées au document actuel n'ont pas été enregistrées. Etes-vous sur de vouloir continuer?", "Confirmation", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }

            return true;
        }

        public List<BrailleCharacter>[] OpenDocument()
        {

            if (!CheckConfirmation())
            {
                return null;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier braille (*.braille)|*.braille|Tous les fichiers (*.*)|*.*";

            if(openFileDialog.ShowDialog() == true)
            {
                string newFilePath = openFileDialog.FileName;

                try
                {
                    Stream stream = File.Open(newFilePath, FileMode.Open);
                    BinaryFormatter binaryFormatter = new BinaryFormatter();

                    List<BrailleCharacter>[] data = (List<BrailleCharacter>[])binaryFormatter.Deserialize(stream);

                    if(data != null)
                    {
                        this.FilePath = newFilePath;
                        DocumentUnchanged();
                        Characters = data;
                        return data;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error happened " + e.Message);
                }
                
            }

            return null;
        }

        public void NewDocument()
        {
            this.FilePath = null;
            DocumentUnchanged();
            Characters = new List<BrailleCharacter>[CHARACTER_ROWS];
            for(int i = 0; i < CHARACTER_ROWS; i++)
            {
                Characters[i] = new List<BrailleCharacter>();
            }
        }

        public string GetInstructions()
        {
            List<String> instructions = new List<string>();
            instructions.Add("START_PRINT");
            Document document = this;

            for (int i = 0; i < SightBrailleApp.CHARACTER_ROWS; i++)
            {
                List<BrailleSymbol> symbols = CalculateSymbols(document, i);

                for (int j = 0; j < 3; j++)
                {
                    List<String> dotLine = new List<String>();
                    dotLine.Add("BEGIN_LINE");
                    int horizontalPosition = 0;

                    foreach (BrailleSymbol symbol in symbols)
                    {

                        for (int k = 0; k < 2; k++)
                        {
                            if (symbol.Points[j, k])
                            {
                                dotLine.Add((horizontalPosition + k).ToString());
                            }
                        }

                        horizontalPosition += 2;
                    }

                    dotLine.Add("STOP_LINE");
                    instructions.AddRange(dotLine);
                }
            }

            instructions.Add("END_OF_PRINT");

            return string.Join("\n", instructions.ToArray());
        }

        private List<BrailleSymbol> CalculateSymbols(Document document, int row)
        {
            List<BrailleCharacter> characters = document.Characters[row];
            List<BrailleSymbol> symbols = new List<BrailleSymbol>();
            int i = 0;
            int length = characters.Count;
            BrailleCharacter before = BlankCharacter;
            BrailleCharacter beforebefore = BlankCharacter;
            BrailleCharacter after;

            //int symbolCountBefore = Symbols[row].Count;
            foreach (BrailleCharacter character in characters)
            {
                if (i < length - 1)
                {
                    after = characters[i + 1];
                }
                else
                {
                    after = BlankCharacter;
                }

                foreach (BrailleSymbolSlot slot in character.GetSymbols(beforebefore, before, after))
                {
                    symbols.Add(slot.GetSymbol());

                }

                //Next
                beforebefore = before;
                before = character;
                i++;
            }

            return symbols;

        }
    }
}

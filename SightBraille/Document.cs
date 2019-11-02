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
    }
}

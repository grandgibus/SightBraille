using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SightBraille.BrailleEditor;
using static SightBraille.SightBrailleApp;

namespace SightBraille
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class EditorWindow : Window
    {

        public static RoutedCommand DictionnaryCommand = new RoutedCommand();

        private BrailleEditor _editor;

        public EditorWindow()
        {
            InitializeComponent();
            this._editor = new BrailleEditor(this, this.MainEditorContainer);
            CommandBinding dictionnaryCommandBinding = new CommandBinding(DictionnaryCommand, ExecuteDictionnary, CanExecuteDictionnary);
            this.CommandBindings.Add(dictionnaryCommandBinding);
            this.DictionnaryButton.Command = DictionnaryCommand;
            this.SymbolMenu.Command = DictionnaryCommand;

            this.SerialPortsComboBox.ItemsSource = (Application.Current as SightBrailleApp).ConnectionManager.SerialPorts;
        }

        private void CanExecuteDictionnary(object obj, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExecuteDictionnary(object obj, ExecutedRoutedEventArgs e)
        {
            Window symbols = new BrailleSymbolListWindow();
            symbols.Show();
        }

        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            (Application.Current as SightBrailleApp).Document.NewDocument();
            this._editor.LoadNewData((Application.Current as SightBrailleApp).Document.Characters);
        }

        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Open();
        }

        private void Open()
        {
            List<BrailleCharacter>[] data = (Application.Current as SightBrailleApp).Document.OpenDocument();
            if(data != null)
            {
                this._editor.LoadNewData(data);
            }
        }

        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            (Application.Current as SightBrailleApp).Document.SaveDocument(this._editor.Characters);
        }

        private void PrintCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void PrintCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            printDocument();
        }

        private void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Fichier braille (*.braille)|*.braille|Tous les fichiers (*.*)|*.*";
        }

        private void printDocument()
        {

            PrintDialog printDialog = new PrintDialog();

            bool? result = printDialog.ShowDialog();
            if(result == true)
            {

                double docWidth = printDialog.PrintableAreaWidth;
                double docHeight = printDialog.PrintableAreaHeight;

                FlowDocument document = new FlowDocument();
                document.Background = Brushes.White;
                Section main = new Section();
                main.FontSize = docWidth / A4_WIDTH * 10.5;
                main.FontFamily = new FontFamily("Courier New");
                main.FontStretch = FontStretches.UltraExpanded;
                document.PagePadding = new Thickness(docWidth / A4_WIDTH * MARGIN_WIDTH, docWidth / A4_WIDTH * MARGIN_HEIGHT, docWidth / A4_WIDTH * MARGIN_WIDTH, docWidth / A4_WIDTH * MARGIN_HEIGHT);
                List<String> text = new List<String>();

                foreach (List<BrailleSymbolSlotPosition> line in this._editor.Symbols)
                {
                    string txtLine = "";
                    foreach (BrailleSymbolSlotPosition s in line)
                    {
                        txtLine += s.Symbol.DisplayLettter;
                    }
                    text.Add(txtLine);
                }

                foreach (string line in text)
                {
                    Paragraph p = new Paragraph(new Run(line));
                    p.Margin = new Thickness(0);
                    p.LineHeight = docWidth / A4_WIDTH * 10.25;
                    p.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                    main.Blocks.Add(p);
                }
                document.Blocks.Add(main);
                document.PageHeight = docHeight;
                document.PageWidth = docWidth;

                document.ColumnGap = 0;
                document.ColumnWidth = printDialog.PrintableAreaWidth;
                IDocumentPaginatorSource idpSource = document;
                printDialog.PrintDocument(idpSource.DocumentPaginator, "Braille");

/*                FlowDocumentPageViewer viewer = new FlowDocumentPageViewer();
                viewer.Document = document;
                Window preview = new Window();
                preview.Background = Brushes.LightGray;
                preview.Content = viewer;
                preview.Show();*/
            }


        }
    }
}

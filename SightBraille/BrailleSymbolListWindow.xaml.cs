using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using static SightBraille.SightBrailleApp;

namespace SightBraille
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class BrailleSymbolListWindow : Window
    {

        public SymbolDots firstSymbol = new SymbolDots();
        public SymbolDots secondSymbol = new SymbolDots();

        public BrailleSymbolListWindow()
        {
            InitializeComponent();

            List<char> allChars = new List<char>();
            allChars.AddRange(SightBrailleApp.LowercaseChars);
            allChars.AddRange(SightBrailleApp.UppercaseChars);
            allChars.AddRange(SightBrailleApp.CurrencyChars);
            allChars.AddRange(SightBrailleApp.NumberChars);
            allChars.AddRange(SightBrailleApp.SymbolChars);

            foreach (char c in allChars)
            {
                ListViewItem item = new ListViewItem();
                item.Content = c;
                this.CharacterList.Items.Add(item);
            }
            this.CharacterList.SelectedIndex = 0;
            this.SymbolsPanel.Children.Add(firstSymbol.DotCanvas);
            this.SymbolsPanel.Children.Add(secondSymbol.DotCanvas);
            this.secondSymbol.DotCanvas.Margin = new Thickness(20, 0, 0, 0);

            this.CharacterList.MouseDoubleClick += DoubleClick;
        }

        private void DoubleClick(object obj, MouseEventArgs args)
        {
            if(CharacterList.SelectedIndex != -1)
            {
                ((SightBrailleApp.Current as SightBrailleApp).MainWindow as EditorWindow).AddCharacter((char)((ListViewItem)CharacterList.SelectedItem).Content);
                this.Close();
            }
        }

        private void CharacterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            char character = ((char)((ListViewItem)CharacterList.SelectedItem).Content);
            this.Character.Text = character.ToString();

            BrailleCharacter braille = GetBrailleCharacter(character);
            List<BrailleSymbolSlot> slots = braille.GetSymbols(BlankCharacter, BlankCharacter, BlankCharacter);

            if (slots.Count == 1)
            {
                this.firstSymbol.UpdateDots(slots[0].GetSymbol().Points);
                this.secondSymbol.DotCanvas.Visibility = Visibility.Collapsed;
            }
            if (slots.Count == 2)
            {
                this.firstSymbol.UpdateDots(slots[0].GetSymbol().Points);
                this.secondSymbol.DotCanvas.Visibility = Visibility.Visible;
                this.secondSymbol.UpdateDots(slots[1].GetSymbol().Points);
            }
        }
    }

    public class SymbolDots
    {
        public Canvas DotCanvas;
        public Ellipse[,] Dots = new Ellipse[3,2];

        public SymbolDots()
        {
            DotCanvas = new Canvas();
            for(int i = 0; i < 3; i++)
            {
                for(int j = 0; j < 2; j++)
                {
                    Dots[i, j] = AddDotAtPos(j, i);
                }
            }
            DotCanvas.Width = 60;
            DotCanvas.Height = 90;
            DotCanvas.HorizontalAlignment = HorizontalAlignment.Center;
            DotCanvas.VerticalAlignment = VerticalAlignment.Center;
        }

        public void UpdateDots(bool[,] state)
        {
            Brush black = Brushes.Black;
            Brush empty = Brushes.LightGray;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Dots[i, j].Fill = state[i, j] ? black : empty;
                }
            }
        }

        public Ellipse AddDotAtPos(int x, int y)
        {
            Ellipse dot = new Ellipse();
            dot.Width = 20;
            dot.Height = 20;
            dot.Fill = Brushes.Black;
            Canvas.SetLeft(dot, x * 35);
            Canvas.SetTop(dot, y * 35);
            DotCanvas.Children.Add(dot);
            return dot;
        }
    }
}

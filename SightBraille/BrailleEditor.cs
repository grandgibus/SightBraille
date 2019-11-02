using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Input;
using System.IO;
using static SightBraille.SightBrailleApp;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using static SightBraille.SightBrailleApp.BrailleCharacter;

namespace SightBraille
{
    class BrailleEditor
    {

        private EditorWindow window;
        private Viewbox _parent;

        public StackPanel EditorContainer;

        public Style LetterStyle = new Style(typeof(TextBlock));
        public Style DotStyle = new Style(typeof(Ellipse));

        public BrailleEditor(EditorWindow window, Viewbox panel)
        {

            this.LetterStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new FontFamily("Courier New")));
            this.LetterStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 9 * FACTOR));
            this.LetterStyle.Setters.Add(new Setter(TextOptions.TextRenderingModeProperty, TextRenderingMode.Aliased));

            this.DotStyle.Setters.Add(new Setter(Ellipse.FillProperty, Brushes.Transparent));
            this.DotStyle.Setters.Add(new Setter(Ellipse.WidthProperty, 3 * FACTOR));
            this.DotStyle.Setters.Add(new Setter(Ellipse.HeightProperty, 3 * FACTOR));

            this.EditorContainer = new StackPanel()
            {
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect()
                {
                    BlurRadius = 15,
                    ShadowDepth = 5,
                    Opacity = 0.3
                },
                UseLayoutRounding = true,
                Margin = new Thickness(0, 15, 0, 15)
            };
            this._parent = panel;
            this._parent.Child = (this.EditorContainer);
            this.window = window;

            InitCanavas();
        }


        private void DataChanged()
        {
            (Application.Current as SightBrailleApp).Document.DocumentChanged();
        }

        public void KeyPressed(object sender, KeyEventArgs args)
        {
            Key k = args.Key;

            if (k == Key.Back)
            {
                handeled(args);
                AttemptEraseBack();
            }
            else if (k == Key.Up)
            {
                handeled(args);
                AttemptCursorPositionOffset(-1, 0);
            }
            else if (k == Key.Down)
            {
                handeled(args);
                AttemptCursorPositionOffset(1, 0);
            }
            else if (k == Key.Left)
            {
                handeled(args);
                AttemptCursorPositionOffsetChangeLine(-1);
            }
            else if (k == Key.Right)
            {
                handeled(args);
                AttemptCursorPositionOffsetChangeLine(1);
            }else if(k == Key.Enter)
            {
                handeled(args);
                AttemptSplitLine();
            }

        }

        private void handeled(KeyEventArgs args)
        {
            args.Handled = true;
        }

        public void TextInput(object sender, TextCompositionEventArgs args)
        {
            string txt = args.Text;

            if(txt != null && txt.Length > 0)
            {
                foreach(char c in txt)
                {
                    AttemptWriteChar(c);
                }
                args.Handled = true;
            }
        }

        public void LeftMouseButtonPressed(object sender, MouseButtonEventArgs args)
        {
            Point clickPos = args.GetPosition(Canvas);
            int row = (int)(clickPos.Y / BRAILLE_HEIGHT_DISP);
            int column = (int)(Math.Round(clickPos.X / BRAILLE_WIDTH_DISP));
            AttemptCursorPositionChange(row, column);
        }

        private int inRange(int max, int a)
        {
            return Math.Max(0, Math.Min(max, a));
        }

        public void SetSymbolAtPosition(int row, int column, BrailleSymbolSlotPosition symbol)
        {
            if(symbol != null)
            {
                BrailleSymbolSlot slot = symbol.Symbol;
                setLetter(row, column, slot.DisplayLettter);
                setFillerCase(row, column, slot.Filler);
            }
            else
            {
                setLetter(row, column, ' ');
                setFillerCase(row, column, false);
            }
        }

        private void setLetter(int row, int column, char c)
        {
            Letters[row, column].Text = c.ToString();
        }

        private void setFillerCase(int row, int column, bool filler)
        {
            Dots[row, column].Fill = filler ? Brushes.LightGray : Brushes.Transparent;
        }

        public List<BrailleCharacter>[] Characters = new List<BrailleCharacter>[CHARACTER_ROWS];
        public List<BrailleSymbolSlotPosition>[] Symbols = new List<BrailleSymbolSlotPosition>[CHARACTER_ROWS];

        public Canvas Canvas;
        public TextBlock[,] Letters = new TextBlock[CHARACTER_ROWS, CHARACTER_COLUMNS];
        public Ellipse[,] Dots = new Ellipse[CHARACTER_ROWS, CHARACTER_COLUMNS];

        public EditorCursor Cursor;

        public void LoadNewData(List<BrailleCharacter>[] data)
        {
            Characters = data;
            for(int i = 0; i < CHARACTER_ROWS; i++)
            {
                RecalculateSymbolSlots(i);
            }
            AttemptCursorPositionChange(0, 0);
        }

        public void AttemptCursorPositionOffset(int offsetRow, int offsetColumn)
        {
            AttemptCursorPositionChange(Cursor.Row + offsetRow, Cursor.Column + offsetColumn);
        }

        public void AttemptCursorPositionOffsetChangeLine(int offsetColumn)
        {
            if(offsetColumn + Cursor.Column > Symbols[Cursor.Row].Count && Cursor.Row + 1 < CHARACTER_ROWS)
            {
                AttemptCursorPositionChange(Cursor.Row + 1, 0);
            }else if(offsetColumn + Cursor.Column < 0 && Cursor.Row > 0)
            {
                AttemptCursorPositionChange(Cursor.Row - 1, CHARACTER_COLUMNS);
            }
            else
            {
                AttemptCursorPositionOffset(0, offsetColumn);
            }

        }

        public void AttemptCursorPositionChange(int row, int column)
        {
            int outputRow = inRange(CHARACTER_ROWS - 1, row);
            int outputColumn = inRange(CHARACTER_COLUMNS, column);
            //One position added to column to account for the cursor's mecanics

            int length = Symbols[outputRow].Count;
            if(outputColumn >= length)
            {
                outputColumn = length;
            }

            int offset = outputRow != Cursor.Row ? 0 : outputColumn - Cursor.Column;
            int p = 0;
            while(IsSymbolOffsetFiller(p - 1, outputRow, outputColumn))
            {
                if(offset >= 0)
                {
                    p++;
                }
                else
                {
                    p--;
                }
            }

            Cursor.SetPosition(outputRow, outputColumn + p);
        }

        private bool IsSymbolOffsetFiller(int o, int row, int column)
        {
            BrailleSymbolSlotPosition s = getSymbolSlotPosition(o, row, column);
            return s == null ? false : s.Symbol.Filler;
        }

        public void AttemptSplitLine()
        {
            //If last line empty
            bool isLastEmpty = true;
            foreach (BrailleCharacter c in Characters[CHARACTER_ROWS - 1])
            {
                if(c.Type != BrailleCharacterType.WHITESPACE)
                {
                    isLastEmpty = false;
                    break;
                }
            }

            if (isLastEmpty && Cursor.Row < CHARACTER_ROWS - 1)
            {

                List<BrailleCharacter> remaining = Characters[Cursor.Row];
                List<BrailleCharacter> cut = new List<BrailleCharacter>();

                //If cursor is at eol
                BrailleSymbolSlotPosition s = GetSymbolSlotPositionCurrent();
                if (s != null)
                {
                    int characterIndex = s.i;
                    remaining = Characters[Cursor.Row].GetRange(0, s.i);
                    cut = Characters[Cursor.Row].GetRange(s.i, Characters[Cursor.Row].Count - s.i);

                }

                for(int i = CHARACTER_ROWS - 1; i > Cursor.Row; i--)
                {
                    Characters[i] = Characters[i - 1];
                }

                Characters[Cursor.Row] = remaining;
                Characters[Cursor.Row + 1] = cut;

                AttemptCursorPositionChange(Cursor.Row + 1, 0);
                for(int i = 0; i < CHARACTER_ROWS; i++)
                {
                    RecalculateSymbolSlots(i);
                }

                DataChanged();
            }
        }

        public void AttemptWriteChar(char c)
        {
            int row = Cursor.Row;
            int column = Cursor.Column;

            BrailleCharacter newChar = GetBrailleCharacter(c);
            if (newChar != null)
            {
                int symbolCount = Symbols[row].Count;
                //Primary check if room available
                if (symbolCount < CHARACTER_COLUMNS)
                {
                    //Find current character index or select last one
                    List<BrailleCharacter> characters = Characters[row];
                    if (characters.Count == 0)
                    {
                        characters.Add(newChar);
                        RecalculateSymbolSlots(row);
                        AttemptCursorPositionOffset(0, newChar.GetSymbols(BlankCharacter, BlankCharacter, BlankCharacter).Count);
                        DataChanged();
                        return;
                    }
                    else
                    {
                        BrailleSymbolSlotPosition previous = GetSymbolSlotPositionBefore();

                        //Calculate new list of symbol slot position and check if it fits
                        List<BrailleCharacter> newCharacters = characters.ToList();
                        int index = previous != null ? previous.i + 1 : 0;
                        newCharacters.Insert(index, newChar);

                        int newSymbolsCount = GetNewSymbolSlots(newCharacters).Count;
                        if (newSymbolsCount <= CHARACTER_COLUMNS)
                        {
                            Characters[row] = newCharacters;
                            RecalculateSymbolSlots(row);
                            AttemptCursorPositionChange(row, GetLastSymbolFromCharacterColumn(index, row));
                            DataChanged();
                            return;
                        }
                    }
                }
            }
        }

        private int GetLastSymbolFromCharacterColumn(int charPos, int row)
        {
            int j = 0;
            int newColumn = 0;
            foreach (BrailleSymbolSlotPosition s in Symbols[row])
            {
                if (s.i == charPos)
                {
                    newColumn = j;
                }
                j++;
            }
            return newColumn + 1;
        }

        private List<BrailleSymbolSlotPosition> GetNewSymbolSlots(List<BrailleCharacter> characters)
        {
            List<BrailleSymbolSlotPosition> symbols = new List<BrailleSymbolSlotPosition>();
            int i = 0;
            int length = characters.Count;
            BrailleCharacter before = BlankCharacter;
            BrailleCharacter beforebefore = BlankCharacter;
            BrailleCharacter after;

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
                    BrailleSymbolSlotPosition s = new BrailleSymbolSlotPosition(slot, 0, i);
                    symbols.Add(s);

                }

                //Next
                beforebefore = before;
                before = character;
                i++;
            }
            return symbols;
        }

        private void RecalculateSymbolSlots(int row)
        {
            List<BrailleCharacter> characters = Characters[row];
            List<BrailleSymbolSlotPosition> symbols = new List<BrailleSymbolSlotPosition>();
            int i = 0;
            int length = characters.Count;
            BrailleCharacter before = BlankCharacter;
            BrailleCharacter beforebefore = BlankCharacter;
            BrailleCharacter after;

            int symbolCountBefore = Symbols[row].Count;
            int k = 0;
            foreach(BrailleCharacter character in characters)
            {
                if(i < length - 1)
                {
                    after = characters[i + 1];
                }
                else
                {
                    after = BlankCharacter;
                }

                foreach(BrailleSymbolSlot slot in character.GetSymbols(beforebefore, before, after))
                {
                    BrailleSymbolSlotPosition s = new BrailleSymbolSlotPosition(slot, row, i);
                    symbols.Add(s);

                    //Directly update graphics for performance
                    SetSymbolAtPosition(row, k, s);
                    k++;

                }

                //Next
                beforebefore = before;
                before = character;
                i++;
            }
            Symbols[row] = symbols;

            for(int j = k; j < symbolCountBefore; j++)
            {
                SetSymbolAtPosition(row, j, null);
            }
        }

        public void AttemptEraseBack()
        {
            BrailleSymbolSlotPosition symbol = GetSymbolSlotPositionBefore();
            if(symbol != null && symbol.Symbol.Filler)
            {
                AttemptCursorPositionOffset(0, -1);
            }else if(symbol == null && Cursor.Row > 0)
            {
                if (Symbols[Cursor.Row - 1].Count == 0)
                {
                    for (int i = Cursor.Row - 1; i < CHARACTER_ROWS - 1; i++)
                    {
                        Characters[i] = Characters[i + 1];
                    }
                    Characters[CHARACTER_ROWS - 1] = new List<BrailleCharacter>();
                    AttemptCursorPositionChange(Cursor.Row - 1, CHARACTER_COLUMNS);

                    for (int i = Cursor.Row; i < CHARACTER_ROWS; i++)
                    {
                        RecalculateSymbolSlots(i);
                    }
                }
                else if (Symbols[Cursor.Row].Count == 0)
                {
                    for (int i = Cursor.Row; i < CHARACTER_ROWS - 1; i++)
                    {
                        Characters[i] = Characters[i + 1];
                    }
                    AttemptCursorPositionChange(Cursor.Row - 1, CHARACTER_COLUMNS);
                    for (int i = Cursor.Row; i < CHARACTER_ROWS; i++)
                    {
                        RecalculateSymbolSlots(i);
                    }
                }

                DataChanged();

            }
            else if(symbol != null)
            {
                int row = symbol.row;
                Characters[row].RemoveAt(symbol.i);
                RecalculateSymbolSlots(row);
                
                if(symbol.i == 0)
                {
                    AttemptCursorPositionChange(row, 0);
                }
                else if(Characters[row].Count >= symbol.i)
                {
                    AttemptCursorPositionChange(row, GetLastSymbolFromCharacterColumn(symbol.i - 1, row));
                }else if(Characters[row].Count > 0)
                {
                    AttemptCursorPositionChange(row, Characters[row].Count);
                }

                DataChanged();

            }
            
        }

        private BrailleSymbolSlotPosition GetSymbolSlotPositionBefore()
        {
            return GetSymbolSlotPosition(-1);
        }

        private BrailleSymbolSlotPosition GetSymbolSlotPositionBeforeBefore()
        {
            return GetSymbolSlotPosition(-2);
        }

        private BrailleSymbolSlotPosition GetSymbolSlotPositionCurrent()
        {
            return GetSymbolSlotPosition(0);
        }

        private BrailleSymbolSlotPosition GetSymbolSlotPosition(int o)
        {
            return getSymbolSlotPosition(o, Cursor.Row, Cursor.Column);
        }

        private BrailleSymbolSlotPosition getSymbolSlotPosition(int o, int row, int column)
        {
            if (Symbols[row].Count <= column + o || column + o < 0)
            {
                return null;
            }
            else
            {
                return Symbols[row][column + o];
            }
        }

        public class EditorCursor
        {
            public int Row;
            public int Column;

            private Canvas editorCanvas;
            private Rectangle cursor;

            private Storyboard cursorStoryBoard = new Storyboard();

            public EditorCursor(EditorWindow window, Canvas canvas)
            {
                this.cursor = new Rectangle()
                {
                    Width = 0.5 * FACTOR,
                    Height = BRAILLE_HEIGHT_DISP * 0.7,
                    Fill = Brushes.Black
                };
                this.editorCanvas = canvas;
                this.editorCanvas.Children.Add(this.cursor);

                this.cursorStoryBoard = (Storyboard)window.FindResource("AnimateFlicker");
                foreach (DoubleAnimation a in this.cursorStoryBoard.Children)
                {
                    Storyboard.SetTarget(a, this.cursor);
                }
                this.cursor.Loaded += new RoutedEventHandler((object sender, RoutedEventArgs args) => { this.cursorStoryBoard.Begin(); });
            }

            public void SetPosition(int row, int column)
            {
                this.Row = row;
                this.Column = column;
                Canvas.SetLeft(cursor, column * BRAILLE_WIDTH_DISP);
                Canvas.SetTop(cursor, row * BRAILLE_HEIGHT_DISP + 2 * FACTOR);
                this.cursorStoryBoard.Begin();
            }

        }
        public void InitCanavas()
        {
            this.Canvas = new Canvas()
            {
                Width = CHARACTER_COLUMNS * BRAILLE_WIDTH_DISP,
                Height = CHARACTER_ROWS * BRAILLE_HEIGHT_DISP,
                Margin = new Thickness(MARGIN_WIDTH_DISP, MARGIN_HEIGHT_DISP, MARGIN_WIDTH_DISP, MARGIN_HEIGHT_DISP),
                Background = Brushes.Transparent,
                UseLayoutRounding = true,
                Cursor = Cursors.IBeam,
                Focusable = true,
                IsEnabled = true
            };

            window.PreviewKeyDown += new KeyEventHandler(KeyPressed);
            window.TextInput += new TextCompositionEventHandler(TextInput);
            Canvas.MouseLeftButtonDown += new MouseButtonEventHandler(LeftMouseButtonPressed);

            this.EditorContainer.Children.Add(Canvas);

            for (int i = 0; i < CHARACTER_ROWS; i++)
            {
                for(int j = 0; j < CHARACTER_COLUMNS; j++)
                {
                    double xPos = j * BRAILLE_WIDTH_DISP + BRAILLE_WIDTH_DISP / 4;
                    double yPos = i * BRAILLE_HEIGHT_DISP + BRAILLE_HEIGHT_DISP / 2;
                    TextBlock letter = new TextBlock();
                    letter.UseLayoutRounding = true;
                    letter.Text = " ";
                    Ellipse dot = new Ellipse();
                    dot.Style = DotStyle;
                    letter.Style = LetterStyle;
                    Canvas.SetLeft(letter, xPos);
                    Canvas.SetTop(letter, yPos - 3 * FACTOR);
                    Canvas.SetLeft(dot, xPos);
                    Canvas.SetTop(dot, yPos);
                    Letters[i,j] = letter;
                    Dots[i,j] = dot;
                    Canvas.Children.Add(dot);
                    Canvas.Children.Add(letter);
                }
            }

            for(int i = 0; i < CHARACTER_ROWS; i++)
            {
                Symbols[i] = new List<BrailleSymbolSlotPosition>();
                Characters[i] = new List<BrailleCharacter>();
            }

            Cursor = new EditorCursor(window, Canvas);
        }

        public class BrailleSymbolSlotPosition
        {
            public BrailleSymbolSlot Symbol;
            public int row;
            public int i;

            public BrailleSymbolSlotPosition(BrailleSymbolSlot symbol, int row, int i)
            {
                this.Symbol = symbol;
                this.row = row;
                this.i = i;
            }
        }

        public void SaveEdits()
        {
            Document doc = (SightBrailleApp.Current as SightBrailleApp).Document;
            doc.Characters = (List<BrailleCharacter>[])Characters.Clone();
            doc.IsSaved = false;

        }
    }
}

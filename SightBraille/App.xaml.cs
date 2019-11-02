using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SightBraille
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    /// 

    public partial class SightBrailleApp : Application
    {
        public SerialPortConnectionManager ConnectionManager = new SerialPortConnectionManager();

        public Document Document;

        public SightBrailleApp()
        {
            ConnectionManager.InitPortManager();
            this.MainWindow = new EditorWindow();
            this.MainWindow.Show();
            this.Document = new Document(this);
            this.Document.NewDocument();
        }

        public const double A4_WIDTH = 210;
        public const double A4_LENGTH = 297;

        public const double MARGIN_WIDTH = 15;
        public const double MARGIN_HEIGHT = 10;

        public const double USEABLE_A4_WIDTH = A4_WIDTH - 2 * MARGIN_WIDTH;
        public const double USEABLE_A4_LENGTH = A4_LENGTH - 2 * MARGIN_HEIGHT;

        public const double BRAILLE_WIDTH = 6.25;
        public const double BRAILLE_HEIGHT = 10;

        public const int CHARACTER_ROWS = (int)(USEABLE_A4_LENGTH / BRAILLE_HEIGHT);
        public const int CHARACTER_COLUMNS = (int)(USEABLE_A4_WIDTH / BRAILLE_WIDTH);

        public const double FACTOR = 5;

        public const double BRAILLE_WIDTH_DISP = BRAILLE_WIDTH * FACTOR;
        public const double BRAILLE_HEIGHT_DISP = BRAILLE_HEIGHT * FACTOR;

        public const double MARGIN_WIDTH_DISP = MARGIN_WIDTH * FACTOR;
        public const double MARGIN_HEIGHT_DISP = MARGIN_HEIGHT * FACTOR;

        public void ChangeEditorWindowTitleStatus(string text)
        {
            this.MainWindow.Title = "SightBraille - " + text;
        }

        public class BrailleSymbol : IEnumerable
        {
            public bool[,] Points = new bool[3, 2];
            public BrailleSymbol(string character)
            {
                Points = GetBrailleFromString(character);
            }

            public bool[,] GetBrailleFromString(string text)
            {
                bool[,] braille = new bool[3, 2];
                if (text.Length != 6)
                {
                    return braille;
                }
                else
                {
                    int i = 0;
                    int j = 0;
                    foreach (char c in text)
                    {
                        braille[i, j] = c == '#';
                        j++;

                        if (j > 1)
                        {
                            j = 0;
                            i++;
                        }
                    }

                    return braille;
                }
            }

            public IEnumerator GetEnumerator()
            {
                return Points.GetEnumerator();
            }
        }

        public class BrailleSymbolSlot
        {

            private BrailleSymbol Symbol;
            public char DisplayLettter;
            public bool Filler;

            public BrailleSymbolSlot(char c, BrailleSymbol symbol, bool filler)
            {
                this.DisplayLettter = c;
                this.Symbol = symbol;
                this.Filler = filler;
            }

            public BrailleSymbol GetSymbol()
            {
                return Symbol;
            }
        }

        public static Dictionary<char, BrailleSymbol> BrailleDictionary = new Dictionary<char, BrailleSymbol>
        {
            {'a', new BrailleSymbol("#-----") },
            {'b', new BrailleSymbol("#-#---") },
            {'c', new BrailleSymbol("##----") },
            {'d', new BrailleSymbol("##-#--") },
            {'e', new BrailleSymbol("#--#--") },
            {'f', new BrailleSymbol("###---") },
            {'g', new BrailleSymbol("####--") },
            {'h', new BrailleSymbol("#-##--") },
            {'i', new BrailleSymbol("-##---") },
            {'j', new BrailleSymbol("-###--") },
            {'k', new BrailleSymbol("#---#-") },
            {'l', new BrailleSymbol("#-#-#-") },
            {'m', new BrailleSymbol("##--#-") },
            {'n', new BrailleSymbol("##-##-") },
            {'o', new BrailleSymbol("#--##-") },
            {'p', new BrailleSymbol("###-#-") },
            {'q', new BrailleSymbol("#####-") },
            {'r', new BrailleSymbol("#-###-") },
            {'s', new BrailleSymbol("-##-#-") },
            {'t', new BrailleSymbol("-####-") },
            {'u', new BrailleSymbol("#---##") },
            {'v', new BrailleSymbol("#-#-##") },
            {'w', new BrailleSymbol("-###-#") },
            {'x', new BrailleSymbol("##--##") },
            {'y', new BrailleSymbol("##-###") },
            {'z', new BrailleSymbol("#--###") },

            {'à', new BrailleSymbol("#-####") },
            {'â', new BrailleSymbol("#----#") },
            {'ç', new BrailleSymbol("###-##") },
            {'è', new BrailleSymbol("######") },
            {'ê', new BrailleSymbol("#-#--#") },
            {'ë', new BrailleSymbol("###--#") },
            {'î', new BrailleSymbol("##---#") },
            {'ï', new BrailleSymbol("####-#") },
            {'ô', new BrailleSymbol("##-#-#") },
            {'œ', new BrailleSymbol("-##--#") },
            {'ù', new BrailleSymbol("-#####") },
            {'û', new BrailleSymbol("#--#-#") },
            {'ü', new BrailleSymbol("#-##-#") },
            //Number
            {'1', new BrailleSymbol("#----#") },
            {'2', new BrailleSymbol("#-#--#") },
            {'3', new BrailleSymbol("##---#") },
            {'4', new BrailleSymbol("##-#-#") },
            {'5', new BrailleSymbol("#--#-#") },
            {'6', new BrailleSymbol("###--#") },
            {'7', new BrailleSymbol("####-#") },
            {'8', new BrailleSymbol("#-##-#") },
            {'9', new BrailleSymbol("-##--#") },
            {'0', new BrailleSymbol("-#-###") },
            {'+', new BrailleSymbol("--###-") },
            {'-', new BrailleSymbol("----##") },
            {'×', new BrailleSymbol("---##-") },
            {'÷', new BrailleSymbol("--##--") },
            {'=', new BrailleSymbol("--####") },

            {',', new BrailleSymbol("--#---") },
            {';', new BrailleSymbol("--#-#-") },
            {':', new BrailleSymbol("--##--") },
            {'.', new BrailleSymbol("--##-#") },
            {'?', new BrailleSymbol("--#--#") },
            {'!', new BrailleSymbol("--###-") },
            {'"', new BrailleSymbol("--####") },
            {'(', new BrailleSymbol("--#-##") },
            {')', new BrailleSymbol("---###") },
            {'\'', new BrailleSymbol("----#-") },
            {'*', new BrailleSymbol("---##-") },
            {'/', new BrailleSymbol("-#--#-") },
            {'@', new BrailleSymbol("-#-##-") },

            //Symbol
            {'§', new BrailleSymbol("###-#-") },
            {'&', new BrailleSymbol("######") },
            {'©', new BrailleSymbol("##----") },
            {'®', new BrailleSymbol("#-###-") },
            {'™', new BrailleSymbol("-####-") },
            {'%', new BrailleSymbol("-#--##") },
            //Currency
            {'¥', new BrailleSymbol("##-###") },
            {'€', new BrailleSymbol("#--#--") },
            {'$', new BrailleSymbol("-##-#-") },
            {'£', new BrailleSymbol("#-#-#-") },
        };

        public static Dictionary<String, BrailleSymbol> BrailleSpecialDictionary = new Dictionary<string, BrailleSymbol>
        {
            {NUMBER, new BrailleSymbol("-----#") },
            {WHITESPACE, new BrailleSymbol("------") },
            {UPPERCASE, new BrailleSymbol("-#---#") },
            {SYMBOL, new BrailleSymbol("---#--") },
            {CURRENCY, new BrailleSymbol("-#-#--") },
        };

        public static List<char> WhitespaceChars = new List<char> {' '};
        public static List<char> LowercaseChars = new List<char> {'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','à','â','ç','è','ê','ë','î','ï','ô','œ','ù','û','ü',',',';',':','.','?','!','"','(',')','\'','-','*','/','@'};
        public static List<char> UppercaseChars = new List<char> {'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z','À', 'Â', 'Ç', 'È', 'Ê', 'Ë', 'Î', 'Ï', 'Ô', 'Œ', 'Ù', 'Û', 'Ü'};
        public static List<char> NumberChars = new List<char> {'1','2','3','4','5','6','7','8','9','0','+','-','×','÷','='};
        public static List<char> SymbolChars = new List<char> {'§','&','©','®','™','%'};
        public static List<char> CurrencyChars = new List<char> {'¥','€','$','£'};

        private const string WHITESPACE = "whitespace";
        private const string NUMBER = "number";
        private const string UPPERCASE = "uppercase";
        private const string SYMBOL = "symbol";
        private const string CURRENCY = "currency";

        public static BrailleCharacter BlankCharacter = new BrailleCharacter(' ', BrailleCharacterType.WHITESPACE);

        public static BrailleCharacter GetBrailleCharacter(char c)
        {
            if(c == ' ')
            {
                return BlankCharacter;
            }else if (LowercaseChars.Contains(c))
            {
                return new BrailleCharacter(c, BrailleCharacterType.LOWERCASE);
            }else if (UppercaseChars.Contains(c))
            {
                return new BrailleCharacter(c, BrailleCharacterType.UPPERCASE);
            }else if (NumberChars.Contains(c))
            {
                return new BrailleCharacter(c, BrailleCharacterType.NUMBER);
            }else if (SymbolChars.Contains(c))
            {
                return new BrailleCharacter(c, BrailleCharacterType.SYMBOL);
            }else if (CurrencyChars.Contains(c))
            {
                return new BrailleCharacter(c, BrailleCharacterType.CURRENCY);
            }
            else
            {
                return null;
            }
        }

        [Serializable]
        public class BrailleCharacter
        {
            public char Letter;
            public BrailleCharacterType Type;
            [NonSerialized]
            private List<BrailleSymbolSlot> symbolSlots;

            public BrailleCharacter(char c, BrailleCharacterType type)
            {
                this.Letter = c;
                this.Type = type;
            }

            public List<BrailleSymbolSlot> GetSymbols(BrailleCharacter beforebefore, BrailleCharacter before, BrailleCharacter after)
            {
                this.symbolSlots = new List<BrailleSymbolSlot>();

                if(Type == BrailleCharacterType.WHITESPACE)
                {
                    AddSymbolSlotSpecial(WHITESPACE, false);
                }else if(Type == BrailleCharacterType.LOWERCASE)
                {
                    if(before.Type == BrailleCharacterType.NUMBER || (beforebefore.Type == BrailleCharacterType.UPPERCASE && before.Type == BrailleCharacterType.UPPERCASE))
                    {
                        AddSymbolSlotSpecial(WHITESPACE);
                    }
                    AddSymbolSlot(Letter);
                }else if(Type == BrailleCharacterType.UPPERCASE)
                {
                    if(before.Type != BrailleCharacterType.UPPERCASE)
                    {
                        if (!IsIndependantCharacter(before))
                        {
                            AddSymbolSlotSpecial(WHITESPACE);
                        }

                        //If the previous char is not uppercase, then add uppercase mark. If the character after it is also uppercase, then add a second mark.
                        AddSymbolSlotSpecial(UPPERCASE);
                        if(after.Type == BrailleCharacterType.UPPERCASE)
                        {
                            AddSymbolSlotSpecial(UPPERCASE);
                        }
                    }
                    AddSymbolSlot(Letter);
                }else if(Type == BrailleCharacterType.NUMBER)
                {
                    //Add number mark if character before is not number
                    if(before.Type != BrailleCharacterType.NUMBER)
                    {
                        AddSymbolSlotSpecial(NUMBER);
                    }
                    AddSymbolSlot(Letter);
                }
                else
                {
                    //Handles both money and special symbols
                    if (!IsIndependantCharacter(before))
                    {
                        AddSymbolSlotSpecial(WHITESPACE);
                    }
                    if(Type == BrailleCharacterType.CURRENCY)
                    {
                        AddSymbolSlotSpecial(CURRENCY);
                        AddSymbolSlot(Letter);
                    }else if(Type == BrailleCharacterType.SYMBOL)
                    {
                        AddSymbolSlotSpecial(SYMBOL);
                        AddSymbolSlot(Letter);
                    }
                }

                return symbolSlots;

            }

            private bool IsIndependantCharacter(BrailleCharacter c)
            {
                return c.Type == BrailleCharacterType.WHITESPACE || c.Type == BrailleCharacterType.LOWERCASE || c.Type == BrailleCharacterType.SYMBOL || c.Type == BrailleCharacterType.CURRENCY;
            }

            private void AddSymbolSlotSpecial(string s, bool filler = true)
            {
                this.symbolSlots.Add(new BrailleSymbolSlot(' ', BrailleSpecialDictionary[s], filler));
            }

            private void AddSymbolSlot(char c)
            {
                this.symbolSlots.Add(new BrailleSymbolSlot(c, BrailleDictionary[c.ToString().ToLower()[0]], false));
            }
        }

        public enum BrailleCharacterType
        {
            LOWERCASE,
            UPPERCASE,
            NUMBER,
            WHITESPACE,
            SYMBOL,
            CURRENCY
        }


    }
}
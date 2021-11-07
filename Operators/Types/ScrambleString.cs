using System;
using System.Diagnostics;
using System.Text;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_a2567844_3314_48de_bda7_7904b5546535;

namespace T3.Operators.Types.Id_7b21f10b_3548_4a23_95df_360addaeb03d
{
    public class ScrambleString : Instance<ScrambleString>
    {
        [Output(Guid = "ABA9EB42-5AF0-4165-A2BD-FDFCD4340484", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<string> Result = new Slot<string>();

        [Output(Guid = "8116d50e-0220-4bb7-b09d-881f722804cd")]
        public readonly Slot<System.Text.StringBuilder> Builder = new Slot<System.Text.StringBuilder>();

        public ScrambleString()
        {
            Result.UpdateAction = Update;
            Builder.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var maxLength = MaxLength.GetValue(context);
            var stringBuilder = InputBuffer.GetValue(context);
            var lastIndex = _index;

            if (maxLength <= 0)
            {
                Result.Value= String.Empty;
                return;
            }

            if (!InputBuffer.IsConnected || stringBuilder == null)
            {
                stringBuilder = _fallbackBuffer;
            }

            if (ClearTrigger.GetValue(context))
            {
                stringBuilder.Clear();
                _index = 0;
            }

            //var mode = (Modes)WriteMode.GetValue(context);

            try
            {
                if (Insert.GetValue(context))
                {
                    if (TriggerRandomPos.GetValue(context))
                        _index = (int)_random.NextLong(0, stringBuilder.Length);

                    var separator = Separator.GetValue(context); ;
                    if (!string.IsNullOrEmpty(separator))
                        separator = separator.Replace("\\n", "\n");

                    var str = InsertString.GetValue(context);
                    var insertString = str + separator;
                    var insertLength = insertString.Length;
                    var currentLength = stringBuilder.Length;
                    var lineWrap = (WrapLinesModes)WrapLines.GetValue(context);
                    var mode = (Modes)WriteMode.GetValue(context);
                    
                    if (_index > maxLength)
                        _index = 0;
                    
                    var pos = _index;
                    if (pos + insertLength > maxLength)
                    {
                        insertLength = maxLength - pos;
                    }

                    if (mode != Modes.Insert && pos < currentLength - insertLength)
                    {
                        stringBuilder.Remove(pos, insertLength);
                    }

                    if (pos > currentLength)
                    {
                        var fillString = FillCharacter.GetValue(context);
                        var fillChar = string.IsNullOrEmpty(fillString) ?  '_' : fillString[0];
                        stringBuilder.Append(new string(fillChar, pos - currentLength));
                    }
                    
                    stringBuilder.Insert(pos, insertString);
                    
                    InsertLineWraps(lineWrap, stringBuilder, pos, insertLength, WrapLineColumn.GetValue(context).Clamp(1,1000));
                    switch (mode)
                    {
                        case Modes.Insert:
                        {
                            _index += insertLength;
                            break;
                        }
                        case Modes.Overwrite:
                        {
                            _index += insertLength;
                            _index %= maxLength;
                            break;
                        }
                        case Modes.OverwriteAtFixedOffset:
                            _index += FillOffset.GetValue(context);
                            if (_index > maxLength)
                            {
                                _index = _index % maxLength;
                            }
                            else if (_index < 0)
                            {
                                _index += maxLength - insertLength;
                            }
                            
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    
                    
                    //InsertLineWraps(lineWrap, stringBuilder);
                    
                    if (stringBuilder.Length > maxLength)
                        stringBuilder.Length = maxLength;
                    
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to manipulate string at index {_index} " + e.Message);
            }

            //

            Builder.Value = stringBuilder;
            Result.Value = stringBuilder.ToString();
        }

        private void InsertLineWraps(WrapLinesModes lineWrap, StringBuilder stringBuilder, int insertPos, int insertLength, int wrapColumn)
        {
            if (lineWrap == WrapLinesModes.WrapAtCharacters)
            {
                //var columnPosAfterInsert = 0;
                var lookBackIndex = insertPos;
                while (lookBackIndex > 0 && stringBuilder[lookBackIndex] != '\n')
                {
                    lookBackIndex--;
                }

                var lineLength = insertPos - lookBackIndex + insertLength;
                if (lineLength > wrapColumn && insertPos > 0 && insertPos < stringBuilder.Length)
                {
                    stringBuilder[insertPos - 1] = '\n';
                    return;
                }
                
                var lookForwardIndex = insertPos;
                while (lookForwardIndex < stringBuilder.Length && stringBuilder[lookForwardIndex] != '\n')
                {
                    lookForwardIndex++;
                }

                if (lookForwardIndex - lookBackIndex > wrapColumn)
                {
                    stringBuilder[insertPos + insertLength - 1] = '\n';
                }
            }
        }

        private enum Modes
        {
            Insert,
            Overwrite,
            OverwriteAtFixedOffset,
        }

        private enum WrapLinesModes
        {
            DontWrap,
            WrapAtWords,
            WrapAtCharacters,
        }

        private StringBuilder _fallbackBuffer = new StringBuilder();

        private int _index = 0;
        private Random _random = new Random();

        [Input(Guid = "CE436E27-05A5-431D-9AA2-920DBFF639A7", MappedType = typeof(Modes))]
        public readonly InputSlot<int> WriteMode = new InputSlot<int>();

        [Input(Guid = "095202BF-118F-4C4C-802E-7916BC290A60")]
        public readonly InputSlot<bool> Insert = new InputSlot<bool>();

        [Input(Guid = "960179BD-286F-4629-BBCB-CD31AA9C9AE2")]
        public readonly InputSlot<string> InsertString = new InputSlot<string>();

        [Input(Guid = "38CE7F47-C117-47A2-AEEA-609716C60555")]
        public readonly InputSlot<int> MaxLength = new InputSlot<int>();

        [Input(Guid = "8EEE8067-1A4E-4372-93D0-2DBC368AA45A")]
        public readonly InputSlot<string> Separator = new InputSlot<string>();

        // [Input(Guid = "B5027A5D-BA50-4BDC-8488-3F71B188FCBC")]
        // public readonly InputSlot<bool> Fill = new InputSlot<bool>();
        //
        [Input(Guid = "1559C0E9-BA56-447F-8241-03D8D59AC205")]
        public readonly InputSlot<int> FillOffset = new InputSlot<int>();

        [Input(Guid = "F977FAAF-1840-4A75-9BC5-43176F2E88E9")]
        public readonly InputSlot<bool> TriggerRandomPos = new InputSlot<bool>();

        [Input(Guid = "875CBFA9-FFA8-4204-810C-C04F5F421441", MappedType = typeof(WrapLinesModes))]
        public readonly InputSlot<int> WrapLines = new InputSlot<int>();

        [Input(Guid = "BD941C4B-18A8-4687-85A8-3FE53B4F6213")]
        public readonly InputSlot<int> WrapLineColumn = new InputSlot<int>();


        [Input(Guid = "77A5604A-034A-4352-BD46-BE3CB57F90B7")]
        public readonly InputSlot<bool> ClearTrigger = new InputSlot<bool>();

        [Input(Guid = "CCFAC8A9-0954-4869-A47C-B66C714F6545")]
        public readonly InputSlot<StringBuilder> InputBuffer = new InputSlot<StringBuilder>();
        
        [Input(Guid = "5E8FB3EE-48A6-46D5-8CF9-368F5A1EA507")]
        public readonly InputSlot<string> FillCharacter = new InputSlot<string>();
        
    }
}
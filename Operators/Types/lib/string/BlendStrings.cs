using System;
using System.Text;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;


namespace T3.Operators.Types.Id_98bd1491_6e69_4ae0_9fc1_0be8e6a72d32
{
    public class BlendStrings : Instance<BlendStrings>
    {
        [Output(Guid = "1bb629bb-dd30-48df-b6b4-3245af10dc09")]
        public readonly Slot<string> Result = new();

        public BlendStrings()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var strA = InputTextA.GetValue(context);
            var strB = InputTextB.GetValue(context);
            var blendFactor = Blend.GetValue(context).Clamp(0,1);

            var maxLength = Math.Max(strA.Length, strB.Length).Clamp(1,100);
            var scrambleFactor = Scramble.GetValue(context);
            var scrambleSeed = ScrambleSeed.GetValue(context);

            const string chars = " .-/\\?#<^*(&ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijlmnopqrstuvwxyz0123456789";
            
            _stringBuilder.Clear();

            for (int index = 0; index < maxLength; index++)
            {
                var charA = GetChar(strA, index);
                var charB = GetChar(strB, index);
                
                if (charA == '\n' || charB == '\n')
                {
                    _stringBuilder.Append(charA);
                    continue;
                }

                var charAInt = chars.IndexOf(charA).Clamp(0,chars.Length-1);
                var charBInt = chars.IndexOf(charB).Clamp(0,chars.Length-1);
                
                var hashA = MathUtils.Hash01((uint)((index * 123 + scrambleSeed/100))); 
                var scrambleOffset = hashA < 0.1f + scrambleFactor ?  (MathUtils.Hash01((uint)(index * 123 + scrambleSeed )) - 0.5f) * chars.Length 
                                                     :0;

                var blendedValue = (int)(charAInt + (charBInt - charAInt) * blendFactor + scrambleOffset).Clamp(0, chars.Length-1);
                var s = chars[blendedValue];
                _stringBuilder.Append(s);
            }

            Result.Value = _stringBuilder.ToString();
        }

        private static char GetChar(string str, int index)
        {
            if (index < 0 || index >= str.Length)
                return ' ';
            
            return str[index];
        }
        
        private StringBuilder _stringBuilder = new();
        
        

        [Input(Guid = "3197934e-d0ed-4a81-9dc1-2cc63d97ac6f")]
        public readonly InputSlot<string> InputTextA = new();

        [Input(Guid = "CCC21ECC-2877-4FE7-8D78-F7E2A708D762")]
        public readonly InputSlot<string> InputTextB= new();
        
        [Input(Guid = "2EFD4A0C-958C-49F6-86CB-F8D9FD6FB308")]
        public readonly InputSlot<float> Blend= new();
        
        [Input(Guid = "DC4E5B79-53E5-463A-92AD-D9BB1F2B0495")]
        public readonly InputSlot<float> Scramble= new();

        [Input(Guid = "D95E112F-B89A-4AA1-954B-10521C0A3815")]
        public readonly InputSlot<int> ScrambleSeed= new();


    }
}
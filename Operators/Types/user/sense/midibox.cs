using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ed972ff7_1608_49b3_968c_09093cf727f3
{
    public class midibox : Instance<midibox>
    {

        [Input(Guid = "91355fdf-818b-49c7-b86f-6db52f481a8d")]
        public readonly InputSlot<bool> trig_ch1 = new InputSlot<bool>();

        [Input(Guid = "379a6ceb-573a-4caa-a5ec-09824e4cfc29")]
        public readonly InputSlot<float> noteCH1 = new InputSlot<float>();

        [Input(Guid = "bcb6a51f-905d-44a8-8b24-c7d2f2a3ce34")]
        public readonly InputSlot<bool> trig_ch2 = new InputSlot<bool>();

        [Input(Guid = "feeb83d8-7fbe-4885-a2a8-7b3c1ead53f7")]
        public readonly InputSlot<float> noteCH2 = new InputSlot<float>();

        [Input(Guid = "169c2a7e-63fd-4f76-a409-aef2d6979d39")]
        public readonly InputSlot<bool> trig_ch3 = new InputSlot<bool>();

        [Input(Guid = "d5c0ba04-f4a4-4980-af10-5b0243ffc917")]
        public readonly InputSlot<float> noteCH3 = new InputSlot<float>();

        [Input(Guid = "de472f08-7166-49cd-ae56-9544d1960871")]
        public readonly InputSlot<bool> trig_ch4 = new InputSlot<bool>();

        [Input(Guid = "6c6edffe-f4f8-4281-b2fc-714b0842e916")]
        public readonly InputSlot<float> noteCH4 = new InputSlot<float>();

        [Input(Guid = "534311de-69cb-477a-bcb3-e796196bdd4b")]
        public readonly InputSlot<bool> trig_ch5 = new InputSlot<bool>();

        [Input(Guid = "1c22e29f-eee6-411b-8ab0-1a846b4a80e3")]
        public readonly InputSlot<float> noteCH5 = new InputSlot<float>();

        [Input(Guid = "a14bd52c-333e-43df-a8ac-a53c39dc594c")]
        public readonly InputSlot<bool> trig_ch6 = new InputSlot<bool>();

        [Input(Guid = "d51aa96f-104d-46c4-8980-466c280477bf")]
        public readonly InputSlot<float> noteCH6 = new InputSlot<float>();

        [Input(Guid = "7bc10657-fec1-44b6-ae30-58b4417f8341")]
        public readonly InputSlot<bool> trig_ch7 = new InputSlot<bool>();

        [Input(Guid = "5b74ad38-8a50-4dc4-bbf3-0b9ed731252f")]
        public readonly InputSlot<float> noteCH7 = new InputSlot<float>();

        [Input(Guid = "86bb06ae-d7f2-4774-b386-890fa444f6d8")]
        public readonly InputSlot<bool> trig_ch8 = new InputSlot<bool>();

        [Input(Guid = "55ed586d-4fcc-470d-a3fd-df1c61bf8a54")]
        public readonly InputSlot<float> noteCH8 = new InputSlot<float>();

        [Input(Guid = "a509f618-b393-47e2-a758-6ee661d34111")]
        public readonly InputSlot<bool> trig_ch9 = new InputSlot<bool>();

        [Input(Guid = "4c50120d-b2c3-4c8c-b2b9-3dc23ea3548d")]
        public readonly InputSlot<float> noteCH9 = new InputSlot<float>();

        [Input(Guid = "431ea712-ff3c-4404-a73e-2cbc43564b8b")]
        public readonly InputSlot<bool> trig_ch10 = new InputSlot<bool>();

        [Input(Guid = "f06fac7d-66de-40e8-a58c-d6114e687d71")]
        public readonly InputSlot<float> noteCH10 = new InputSlot<float>();

        [Input(Guid = "1d2f1c73-ec66-4c7e-a376-0888135b0215")]
        public readonly InputSlot<bool> trig_ch11 = new InputSlot<bool>();

        [Input(Guid = "f7694f31-9d96-44dd-9c3d-08573d020439")]
        public readonly InputSlot<float> noteCH11 = new InputSlot<float>();

        [Input(Guid = "e54ee9ee-9d5c-4278-bcda-cd4b8b1d4755")]
        public readonly InputSlot<bool> trig_ch12 = new InputSlot<bool>();

        [Input(Guid = "bdade590-41f2-4f55-a377-ed75daa81177")]
        public readonly InputSlot<float> noteCH12 = new InputSlot<float>();

        [Input(Guid = "be0319c6-84df-40d5-bd79-29b7dc7669d1")]
        public readonly InputSlot<bool> trig_ch13 = new InputSlot<bool>();

        [Input(Guid = "9ce8776a-c338-4f5e-bf8b-7758316e357e")]
        public readonly InputSlot<float> noteCH13 = new InputSlot<float>();

        [Input(Guid = "d244a394-55fb-4191-aafd-c880dfcaa15b")]
        public readonly InputSlot<bool> trig_ch14 = new InputSlot<bool>();

        [Input(Guid = "f45e8658-cda8-4bbf-ab28-b0b2d72a8e00")]
        public readonly InputSlot<float> noteCH14 = new InputSlot<float>();

        [Input(Guid = "23da6ed0-7e91-4e79-8b67-24f318af7f95")]
        public readonly InputSlot<bool> trig_ch15 = new InputSlot<bool>();

        [Input(Guid = "6718323c-fd51-4de3-bfef-9694328eeb0a")]
        public readonly InputSlot<float> noteCH15 = new InputSlot<float>();

        [Input(Guid = "bc589b2b-9e8c-4cab-9f15-ca8fe601a5b2")]
        public readonly InputSlot<bool> trig_ch16 = new InputSlot<bool>();

        [Input(Guid = "7ff4e532-8c7c-4a1f-b80f-96c0ed552433")]
        public readonly InputSlot<float> noteCH16 = new InputSlot<float>();

        [Output(Guid = "feb81390-7069-41ce-89aa-2631bf64b3ea")]
        public readonly Slot<T3.Core.DataTypes.Command> midout = new Slot<T3.Core.DataTypes.Command>();

    }
}


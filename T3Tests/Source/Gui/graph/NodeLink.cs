namespace t3.graph
{
    class NodeLink
    {
        public int OutputNodeIndex;
        public int OutputSlotIndex;
        public int InputNodeIndex;
        public int InputSlotIndex;
        public NodeLink() { }
        public NodeLink(int input_idx, int input_slot, int output_idx, int output_slot) { OutputNodeIndex = input_idx; OutputSlotIndex = input_slot; InputNodeIndex = output_idx; InputSlotIndex = output_slot; }
    };
}
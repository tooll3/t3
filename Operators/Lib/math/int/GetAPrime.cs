namespace Lib.math.@int;

[Guid("f99cb938-205d-4605-8c17-e0be67f25d98")]
internal sealed class GetAPrime : Instance<GetAPrime>
{
    [Output(Guid = "219AF347-49C8-4CED-A689-6E6ED0E49710")]
    public readonly Slot<int> Result = new();

    public GetAPrime()
    {
        Result.UpdateAction += Update;
    }

        
    private void Update(EvaluationContext context)
    {
        var index = Index.GetValue(context);
        if (index == _lastIndex)
            return;
            
        _lastIndex = index;
        Result.Value = ComputePrime(index);
    }

    private static int ComputePrime(int index)
    {
        var count = 0;
        var n = 2;
        while (true)
        {
            if (count > 10000)
                return -1;
                
            var isPrime = true;
            for (var i = 2; i <= n / 2; i++)
            {
                if (n % i != 0)
                    continue;
                    
                isPrime = false;
                break;
            }

            if (isPrime)
            {
                count++;
                if (count >= index)
                {
                    return n;
                }
            }

            n++;
        }
    }

        
    private int _lastIndex = -1;

    [Input(Guid = "f94d2c1c-35e4-4862-bbaa-1ed84fd276f6")]
    public readonly InputSlot<int> Index = new();
}
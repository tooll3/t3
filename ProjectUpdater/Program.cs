namespace ProjectUpdater;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        Console.WriteLine("Commencing spline reticulation...");

        string currentLibRoot = Path.Combine("C:", "Repos", "FOSS", "t3", "Operators", "Types");
        string newLibRoot = Path.Combine("C:", "Repos", "FOSS", "t3", "Operators");

        Conversion.StartConversion(currentLibRoot, newLibRoot);
    }
}
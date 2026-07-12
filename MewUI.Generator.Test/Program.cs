namespace SampleApp
{
    public static class Program
    {
        public static void Main()
        {
            var vm = new MyViewModel { Host = "localhost", Port = 3000 };

            // 访问自动生成的 ObservableValue<T> 属性
            //var hostObs = vm.HostObservable;
            //var portObs = vm.PortObservable;

            //Console.WriteLine($"Host observable value: {hostObs.Value}");
            //Console.WriteLine($"Port observable value: {portObs.Value}");

            //// 修改 ObservableValue 会写回原属性
            //hostObs.Value = "example.com";
            //portObs.Value = 9090;

            //Console.WriteLine($"vm.Host after update: {vm.Host}");
            //Console.WriteLine($"vm.Port after update: {vm.Port}");
        }
    }
}

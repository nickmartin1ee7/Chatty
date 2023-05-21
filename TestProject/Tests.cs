using ChatHubClient;

namespace TestProject
{
    public class Tests
    {
        private ChatHubService _chatHubA;
        private ChatHubService _chatHubB;

        [SetUp]
        public void Setup()
        {
            _chatHubA = new ChatHubService("https://e640-2600-1700-77c0-5b30-c800-997e-31b8-a778.ngrok-free.app/chathub");
            _chatHubB = new ChatHubService("https://e640-2600-1700-77c0-5b30-c800-997e-31b8-a778.ngrok-free.app/chathub");
        }

        [Test]
        public async Task OnUserConnected()
        {
            bool activated = false;

            _chatHubA.OnUserConnected += (o, e) =>
            {
                Console.WriteLine(e);
                activated = true;
            };

            await _chatHubA.StartAsync("UnitTest");

            Assert.IsTrue(_chatHubA.IsStarted);

            await Task.Delay(200);

            Assert.IsTrue(activated);
        }

        [Test]
        public async Task OnMessageReceived()
        {
            bool received = false;

            _chatHubA.OnMessageReceived += (o, e) =>
            {
                Console.WriteLine(e);
                received = true;
            };

            await _chatHubA.StartAsync("UnitTest");

            Assert.IsTrue(_chatHubA.IsStarted);

            await Task.Delay(200);

            await _chatHubA.SendMessageAsync("Testing!");

            await Task.Delay(200);

            Assert.IsTrue(received);
        }

        [Test]
        public async Task OnUserDisconnected()
        {
            bool received = false;

            _chatHubA.OnUserConnected += (o, e) =>
            {
                Console.WriteLine($"{nameof(_chatHubA)}.{nameof(OnUserConnected)}: {e}");
            };

            _chatHubB.OnUserConnected += (o, e) =>
            {
                Console.WriteLine($"{nameof(_chatHubB)}.{nameof(OnUserConnected)}: {e}");
            };

            _chatHubA.OnUserDisconnected += (o, e) =>
            {
                Console.WriteLine($"{nameof(_chatHubA)}.{nameof(OnUserDisconnected)}: {e}");
                received = true;
            };

            await _chatHubA.StartAsync("UnitTestA");
            await _chatHubB.StartAsync("UnitTestB");

            Assert.IsTrue(_chatHubA.IsStarted);
            Assert.IsTrue(_chatHubB.IsStarted);

            await Task.Delay(200);

            await _chatHubB.StopAsync();

            await Task.Delay(200);

            Assert.IsTrue(received);

            await _chatHubA.StopAsync();
        }
    }
}
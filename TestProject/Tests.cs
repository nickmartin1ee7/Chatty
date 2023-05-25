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
            var httpClient = new HttpClient();
            _chatHubA = new ChatHubService(httpClient, "http://localhost:5080");
            _chatHubB = new ChatHubService(httpClient, "http://localhost:5080");
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

            await _chatHubA.StartAsync("UnitTest1");

            Assert.IsTrue(_chatHubA.IsStarted);

            await Task.Delay(200);

            Assert.IsTrue(activated);
        }

        [Test]
        public async Task OnAllMessageReceived()
        {
            bool received = false;

            _chatHubA.OnMessageReceived += (o, e) =>
            {
                Console.WriteLine(e);
                received = true;
            };

            await _chatHubA.StartAsync("UnitTest2");

            Assert.IsTrue(_chatHubA.IsStarted);

            await Task.Delay(200);

            await _chatHubA.SendMessageAsync("Testing!");

            await Task.Delay(200);

            Assert.IsTrue(received);
        }

        [Test]
        public async Task OnDmMessageReceived()
        {
            bool received = false;

            _chatHubA.OnMessageReceived += (o, e) =>
            {
                Console.WriteLine(e);
                received = true;
            };

            _chatHubA.OnUsernameRegistered += async (o, e) =>
            {
                if (e == "UnitTest3")
                    await _chatHubA.SendMessageAsync("Testing!", "UnitTest3");
            };

            await _chatHubA.StartAsync("UnitTest3");

            Assert.IsTrue(_chatHubA.IsStarted);

            await Task.Delay(5000);

            Assert.IsTrue(received);
        }

        [Test]
        public async Task OnReconnectOlderMessagesReceived()
        {
            bool received = false;

            await _chatHubA.StartAsync("UnitTest4");
            await _chatHubA.SendMessageAsync("Testing!");
            await _chatHubA.StopAsync();

            _chatHubA.OnMessageReceived += (o, e) =>
            {
                Console.WriteLine(e);
                received = true;
            };

            await _chatHubA.StartAsync("UnitTest4");

            Assert.IsTrue(_chatHubA.IsStarted);

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
                received = e == "UnitTest6";
            };

            await _chatHubA.StartAsync("UnitTest5");
            await _chatHubB.StartAsync("UnitTest6");

            Assert.IsTrue(_chatHubA.IsStarted);
            Assert.IsTrue(_chatHubB.IsStarted);

            await Task.Delay(200);

            await _chatHubB.StopAsync();

            await Task.Delay(600);

            Assert.IsTrue(received);

            await _chatHubA.StopAsync();
        }
    }
}
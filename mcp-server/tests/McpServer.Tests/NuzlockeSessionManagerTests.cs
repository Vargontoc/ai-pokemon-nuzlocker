using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using es.vargontoc.nuzlocke.ai.Services;
using es.vargontoc.nuzlocke.ai.Models;

namespace es.vargontoc.nuzlocke.ai.Tests
{
    public class NuzlockeSessionManagerTests : IDisposable
    {
        private readonly string _rootDir;
        private readonly string _registryPath;
        private readonly NuzlockeSessionManager _manager;

        public NuzlockeSessionManagerTests()
        {
            _rootDir = Path.Combine(Path.GetTempPath(), "nuzlocke_tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_rootDir);
            _registryPath = Path.Combine(_rootDir, "registry.json");

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["NuzlockeRegistryPath"] = _registryPath
                })
                .Build();

            _manager = new NuzlockeSessionManager(config);
        }

        [Fact]
        public async Task CreateListLoadAndDeleteSession_Workflow()
        {
            // Create a session directory
            var sessionDir = Path.Combine(_rootDir, "session1");
            Directory.CreateDirectory(sessionDir);

            var info = await _manager.CreateSessionAsync("TestSession", sessionDir);
            Assert.False(string.IsNullOrEmpty(info.Id));
            Assert.Equal(sessionDir, info.Path);

            var list = await _manager.ListSessionsAsync();
            Assert.Contains(list, s => s.Id == info.Id);

            var loaded = await _manager.GetSessionAsync(info.Id);
            Assert.NotNull(loaded);

            var filePath = Path.Combine(sessionDir, ".nuzlocke");
            Assert.True(File.Exists(filePath));

            var data = await _manager.LoadSessionDataAsync(info.Id);
            Assert.Equal(info.Id, data.SessionId);

            // Modify and save
            data.AgentMemory.KeyDecisions.Add("decision1");
            await _manager.SaveSessionDataAsync(info.Id, data);

            var reloaded = await _manager.LoadSessionDataAsync(info.Id);
            Assert.Contains("decision1", reloaded.AgentMemory.KeyDecisions);

            // Delete session
            var del = await _manager.DeleteSessionAsync(info.Id);
            Assert.True(del);

            var afterList = await _manager.ListSessionsAsync();
            Assert.DoesNotContain(afterList, s => s.Id == info.Id);
            Assert.False(File.Exists(filePath));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_rootDir)) Directory.Delete(_rootDir, true);
            }
            catch { }
        }
    }
}

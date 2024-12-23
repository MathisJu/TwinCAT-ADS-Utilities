using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdsUtilitiesTests;

public class AdsRoutingClientTest
{
    private readonly AdsRoutingClient _client;

    public AdsRoutingClientTest()
    {
        _client = new AdsRoutingClient();
        _client.Connect().Wait();

    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task GetLocalIpFromLocalHostname_ShouldReturnIp_WhenNameIsValid()
    {
        // Arrange
        string localHost = "localhost";
        string localHostname = Environment.MachineName;

        // Act
        string? ipLocalHost = await _client.GetIpFromHostname(localHost);
        string? ipLocalHostname = await _client.GetIpFromHostname(localHostname);

        // Assert
        Assert.NotNull(ipLocalHost);
        Assert.Equal("127.0.0.1", ipLocalHost);

        Assert.NotNull(ipLocalHostname);
        Assert.True(IPAddress.TryParse(ipLocalHostname, out _));
    }

    [Fact]
    public async Task GetLocalIpFromLocalHostname_ShouldReturnNull_WhenNameIsInvalid()
    {
        // Arrange
        string invalidHostname = "invalidHostname";

        // Act
        string? ipInvalidName = await _client.GetIpFromHostname(invalidHostname);

        // Assert
        Assert.Null(ipInvalidName);
    }
}
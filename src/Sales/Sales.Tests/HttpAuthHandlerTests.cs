using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using Sales.API.Handlers;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Sales.Tests;

public class HttpAuthHandlerTests
{
    private Mock<IHttpContextAccessor> CreateMockHttpContextAccessor(string authorizationHeader = null)
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        
        if (authorizationHeader != null)
        {
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var headerDictionary = new HeaderDictionary();
            
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                headerDictionary["Authorization"] = authorizationHeader;
            }
            
            mockRequest.Setup(r => r.Headers).Returns(headerDictionary);
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        }
        else
        {
            mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null);
        }
        
        return mockHttpContextAccessor;
    }

    private Mock<HttpMessageHandler> CreateMockInnerHandler()
    {
        var mockInnerHandler = new Mock<HttpMessageHandler>();
        mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Success")
            });
        
        return mockInnerHandler;
    }

    #region SendAsync Tests

    [Fact]
    public async Task SendAsync_ShouldAddAuthorizationHeader_WhenTokenExists()
    {
        // Arrange
        var token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var mockHttpContextAccessor = CreateMockHttpContextAccessor(token);
        var mockInnerHandler = CreateMockInnerHandler();
        
        var httpAuthHandler = new HttpAuthHandler(mockHttpContextAccessor.Object)
        {
            InnerHandler = mockInnerHandler.Object
        };
        
        var invoker = new HttpMessageInvoker(httpAuthHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com/api/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Contains("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", request.Headers.Authorization.Parameter);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ShouldNotAddAuthorizationHeader_WhenTokenIsEmpty()
    {
        // Arrange
        var mockHttpContextAccessor = CreateMockHttpContextAccessor(string.Empty);
        var mockInnerHandler = CreateMockInnerHandler();
        
        var httpAuthHandler = new HttpAuthHandler(mockHttpContextAccessor.Object)
        {
            InnerHandler = mockInnerHandler.Object
        };
        
        var invoker = new HttpMessageInvoker(httpAuthHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com/api/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(request.Headers.Authorization);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ShouldNotAddAuthorizationHeader_WhenTokenIsNull()
    {
        // Arrange
        var mockHttpContextAccessor = CreateMockHttpContextAccessor(null);
        var mockInnerHandler = CreateMockInnerHandler();
        
        var httpAuthHandler = new HttpAuthHandler(mockHttpContextAccessor.Object)
        {
            InnerHandler = mockInnerHandler.Object
        };
        
        var invoker = new HttpMessageInvoker(httpAuthHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com/api/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(request.Headers.Authorization);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ShouldNotAddAuthorizationHeader_WhenHttpContextIsNull()
    {
        // Arrange
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null);
        var mockInnerHandler = CreateMockInnerHandler();
        
        var httpAuthHandler = new HttpAuthHandler(mockHttpContextAccessor.Object)
        {
            InnerHandler = mockInnerHandler.Object
        };
        
        var invoker = new HttpMessageInvoker(httpAuthHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com/api/test");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(request.Headers.Authorization);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ShouldCallBaseHandler_Always()
    {
        // Arrange
        var token = "Bearer test-token";
        var mockHttpContextAccessor = CreateMockHttpContextAccessor(token);
        var mockInnerHandler = CreateMockInnerHandler();
        
        var httpAuthHandler = new HttpAuthHandler(mockHttpContextAccessor.Object)
        {
            InnerHandler = mockInnerHandler.Object
        };
        
        var invoker = new HttpMessageInvoker(httpAuthHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test.com/api/test");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        mockInnerHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public async Task SendAsync_ShouldPreserveOriginalRequest_WhenTokenExists()
    {
        // Arrange
        var token = "Bearer test-token";
        var mockHttpContextAccessor = CreateMockHttpContextAccessor(token);
        var mockInnerHandler = CreateMockInnerHandler();
        
        var httpAuthHandler = new HttpAuthHandler(mockHttpContextAccessor.Object)
        {
            InnerHandler = mockInnerHandler.Object
        };
        
        var invoker = new HttpMessageInvoker(httpAuthHandler);
        var originalUri = new Uri("http://test.com/api/test");
        var request = new HttpRequestMessage(HttpMethod.Post, originalUri);
        request.Headers.Add("Custom-Header", "CustomValue");
        request.Content = new StringContent("test content");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(originalUri, request.RequestUri);
        Assert.True(request.Headers.Contains("Custom-Header"));
        Assert.Equal("CustomValue", request.Headers.GetValues("Custom-Header").First());
        Assert.NotNull(request.Content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}

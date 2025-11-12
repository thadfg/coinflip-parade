using Moq;
using PersistenceService.Infrastructure.Kafka;
using System;
using System.Threading.Tasks;

public class MockKafkaLogHelper
{
    public Mock<IKafkaLogHelper> Mock { get; }

    public IKafkaLogHelper Object => Mock.Object;

    public MockKafkaLogHelper()
    {
        Mock = new Mock<IKafkaLogHelper>();

        // Default behavior: do nothing
        Mock.Setup(x => x.LogToKafkaAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    public void VerifyLogCalled(string level, string message, Times times)
    {
        Mock.Verify(x => x.LogToKafkaAsync(
            level,
            message,
            It.IsAny<Exception>(),
            It.IsAny<string>()), times);
    }
}

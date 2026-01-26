using FluentAssertions;
using Imagile.Framework.Storage.Interfaces;
using Imagile.Framework.Storage.Tests.TestFixtures;
using Xunit;

namespace Imagile.Framework.Storage.Tests.Interfaces;

[Trait("Category", "Unit")]
public class IQueueMessageTests
{
    [Fact]
    public void DefaultQueueName_ReturnsExpectedValue()
    {
        // Act
        var queueName = TestQueueMessage.DefaultQueueName;

        // Assert
        queueName.Should().Be("test-queue");
    }

    [Fact]
    public void DefaultQueueName_AccessibleViaGenericConstraint()
    {
        // Act
        var queueName = GetQueueName<TestQueueMessage>();

        // Assert
        queueName.Should().Be("test-queue");
    }

    [Fact]
    public void ImplementingType_CanBeInstantiated()
    {
        // Act
        var message = new TestQueueMessage
        {
            TestId = 123,
            TestData = "test"
        };

        // Assert
        message.TestId.Should().Be(123);
        message.TestData.Should().Be("test");
    }

    [Fact]
    public void MultipleImplementations_HaveDistinctQueueNames()
    {
        // Act
        var testQueueName = TestQueueMessage.DefaultQueueName;
        var archiveQueueName = ArchiveQueueMessage.DefaultQueueName;
        var anotherQueueName = AnotherQueueMessage.DefaultQueueName;

        // Assert
        testQueueName.Should().NotBe(archiveQueueName);
        testQueueName.Should().NotBe(anotherQueueName);
        archiveQueueName.Should().NotBe(anotherQueueName);
    }

    private static string GetQueueName<T>() where T : IQueueMessage
    {
        return T.DefaultQueueName;
    }
}
